# TraceQ.Core

Domain layer for TraceQ. Contains entity models, DTOs, service/repository interfaces, and utilities. This project has no external NuGet dependencies — it defines contracts that `TraceQ.Infrastructure` implements.

## Models

| Class | Purpose |
|-------|---------|
| `Requirement` | Primary domain entity — PLM requirement with Number, Name, Description, metadata fields, and traceability links |
| `ImportBatch` | Tracks a single CSV import session (filename, timestamp, row counts) |
| `ImportRecord` | Per-requirement outcome within an import batch (inserted/updated/skipped/error) |
| `DashboardLayout` | Named dashboard widget layout stored as JSON |
| `AuditLogEntry` | Audit trail entry (event type, details, timestamp) |

## DTOs

Data transfer objects for API request/response payloads:

- `RequirementDto`, `SearchRequestDto`, `SearchResultDto`, `SearchFiltersDto`
- `ImportResultDto`, `ImportBatchDto`, `ImportBatchDetailDto`, `ImportRecordDto`
- `FacetsDto`, `FacetValueDto`, `DistributionDto`
- `TraceabilityCoverageDto`, `SimilarityClusterDto`, `ClusterMemberDto`
- `DashboardLayoutDto`, `PaginatedResultDto<T>`

## Interfaces

| Interface | Responsibility |
|-----------|----------------|
| `ICsvParser` | Parse a CSV stream into `RequirementParseResult` list |
| `IImportService` | Import CSV, upsert requirements, track batches |
| `IRequirementRepository` | CRUD, search, facets, distribution, traceability queries |
| `ISearchService` | Semantic search, find-similar, similarity clustering |
| `IEmbeddingService` | Generate single and batch text embeddings |
| `IVectorStore` | Initialize, upsert, search, and delete vector points |
| `IDashboardLayoutRepository` | CRUD for dashboard layouts |
| `IAuditService` | Log and query audit events |

## Utilities

- **`TraceLinkParser`** — parses and normalizes comma/semicolon-separated traceability link strings

## Namespace conventions

```
TraceQ.Core.Models       — entity classes
TraceQ.Core.DTOs         — data transfer objects
TraceQ.Core.Interfaces   — service and repository contracts
TraceQ.Core.Utilities    — shared helpers
```
