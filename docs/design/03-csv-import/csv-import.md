# CSV Import Pipeline -- Detail Design

## 1. Overview

The CSV Import Pipeline allows systems engineers to upload Windchill PLM CSV exports into TraceQ. The pipeline performs the following steps:

1. **Upload and Validate** -- The user uploads a CSV file (max 50 MB) via drag-and-drop or file picker. The frontend validates file extension (.csv) and size before sending.
2. **Parse** -- The backend parses the CSV using CsvHelper with a Windchill-specific column mapping (`WindchillRequirementMap`). Each row is parsed independently so that per-row errors do not abort the entire import. Encoding is detected from the byte-order mark (BOM).
3. **Upsert Requirements** -- For each successfully parsed row the system checks the SQLite `Requirements` table by requirement number:
   - **Insert** -- new requirement number, row is inserted.
   - **Update** -- existing requirement whose content has changed, row is updated and `IsEmbedded` is set to `false`.
   - **Skip** -- existing requirement with no changes, row is skipped.
4. **Generate Embeddings** -- New and changed requirements are embedded in-line via the ONNX embedding model. The resulting vectors are upserted into the Qdrant `requirements` collection in chunks of 100.
5. **Produce Report** -- An `ImportBatch` record with per-row `ImportRecord` entries is persisted to SQLite. The frontend displays inserted / updated / skipped / error counts and allows drill-down to individual record status.

A background service (`EmbeddingBackgroundService`) polls every 30 seconds for any requirements with `IsEmbedded = false` as a safety net to catch anything missed by the inline embedding step.

## 2. Components

### 2.1 Frontend

#### ImportComponent

| Property | Value |
|---|---|
| Route | `/import` |
| Responsibility | Drag-and-drop upload area, client-side file validation (.csv extension, less than 50 MB), progress indicator during upload, import result summary showing inserted / updated / skipped / error counts, import history table with pagination (default 10 per page), batch detail view showing per-record status. |

#### ImportService (Angular API Client)

| Method | Signature |
|---|---|
| `uploadCsv` | `uploadCsv(file: File): Observable<ImportResult>` |
| `getHistory` | `getHistory(params?: PaginationParams): Observable<PaginatedResult<ImportBatch>>` |
| `getBatch` | `getBatch(batchId: string): Observable<ImportBatchDetail>` |

### 2.2 Backend

#### ImportController

| Endpoint | Method | Description |
|---|---|---|
| `/api/import/csv` | POST | Multipart form upload with 50 MB limit. Returns `ImportResultDto`. |
| `/api/import/history` | GET | Paginated list of past import batches. |
| `/api/import/{batchId}` | GET | Detail view of a single batch with per-record status. |

Delegates to `IImportService`. After a successful import the controller calls `IAuditService.LogImportAsync` to record the operation.

#### IImportService / ImportService

- `ImportAsync(Stream stream, string fileName)` -- orchestrates the full pipeline:
  1. Calls `ICsvParser.ParseAsync` to obtain a list of `RequirementParseResult`.
  2. Creates an `ImportBatch` record.
  3. For each parse result: checks existing requirement by number via `IRequirementRepository.GetByNumberAsync`. Inserts, updates (sets `IsEmbedded = false`), or skips accordingly. Creates a corresponding `ImportRecord`.
  4. Saves the batch and records to SQLite.
  5. Calls `IEmbeddingService.GenerateBatchEmbeddingsAsync` inline for new/changed requirements.
  6. Calls `IVectorStore.UpsertBatchAsync` to push vectors to Qdrant.
  7. Calls `IRequirementRepository.MarkAsEmbeddedAsync` for successfully embedded requirements.
  8. Returns `ImportResultDto`.

#### ICsvParser / CsvParser

- `ParseAsync(Stream stream)` -- uses CsvHelper with `WindchillRequirementMap` class map.
- Column mapping: Number -> RequirementNumber, Name, Description, Type, State, Priority, Owner, Created On, Modified On, Module, Parent Number, Traced To.
- All fields are optional except Number.
- Per-row error handling: each row produces a `RequirementParseResult` containing the parsed requirement (on success) or an error message (on failure).
- Detects encoding from BOM; falls back to UTF-8.

#### IEmbeddingService / OnnxEmbeddingService

- `GenerateBatchEmbeddingsAsync(IReadOnlyList<Requirement> requirements)` -- generates vector embeddings for a batch of requirements using the ONNX runtime model.

#### IVectorStore / QdrantVectorStore

- `UpsertBatchAsync(IReadOnlyList<VectorPoint> points)` -- upserts vectors into the Qdrant `requirements` collection, chunked at 100 points per request to respect Qdrant payload limits.

#### IRequirementRepository / RequirementRepository

| Method | Description |
|---|---|
| `UpsertAsync(Requirement requirement)` | Insert or update a requirement in SQLite. |
| `GetByNumberAsync(string number)` | Look up an existing requirement by its number. |
| `MarkAsEmbeddedAsync(IEnumerable<string> ids)` | Set `IsEmbedded = true` for the given requirement IDs. |
| `GetUnembeddedAsync()` | Return all requirements where `IsEmbedded = false`. |

#### EmbeddingBackgroundService

A `BackgroundService` that polls every 30 seconds for requirements with `IsEmbedded = false`. For any found, it generates embeddings via `OnnxEmbeddingService`, upserts to Qdrant, and marks them as embedded. Acts as a safety net for the inline embedding step.

#### IAuditService / AuditService

- `LogImportAsync(string fileName, int inserted, int updated, int errors)` -- writes an audit log entry to SQLite recording the import operation.

### 2.3 Domain Models

#### ImportBatch

| Field | Type | Description |
|---|---|---|
| Id | string (GUID) | Primary key. |
| FileName | string | Original uploaded file name. |
| ImportedAt | DateTime | Timestamp of the import. |
| InsertedCount | int | Number of rows inserted. |
| UpdatedCount | int | Number of rows updated. |
| SkippedCount | int | Number of unchanged rows skipped. |
| ErrorCount | int | Number of rows that failed parsing or processing. |

#### ImportRecord

| Field | Type | Description |
|---|---|---|
| Id | string (GUID) | Primary key. |
| ImportBatchId | string | Foreign key to `ImportBatch`. |
| RequirementNumber | string | The requirement number from the CSV row. |
| Status | ImportStatus | Enum: `Inserted`, `Updated`, `Skipped`, `Error`. |
| ErrorMessage | string? | Error detail when Status is `Error`. |

#### Requirement

Existing domain model extended with:

| Field | Type | Description |
|---|---|---|
| IsEmbedded | bool | `false` when the requirement is new or changed and has not yet been embedded. Set to `true` after successful embedding and vector upsert. |

### 2.4 Data Stores

| Store | Usage |
|---|---|
| **SQLite** | `ImportBatches` table, `ImportRecords` table (FK to ImportBatches), `Requirements` table. |
| **Qdrant** | `requirements` collection for vector embeddings used by semantic search. |

## 3. Class Diagram

![Class Diagram](class-diagram.puml.png)

## 4. Sequence Diagram

![Sequence Diagram](sequence-diagram.puml.png)

## 5. C4 Diagram

![C4 Diagram](c4-csv-import.drawio.png)
