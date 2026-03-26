# TraceQ.Infrastructure

Implementation layer for TraceQ. Provides CSV parsing, SQLite persistence, ONNX embedding generation, Qdrant vector storage, and service logic. Implements all interfaces defined in `TraceQ.Core`.

## Key components

### CSV parsing (`Csv/`)

| Class | Purpose |
|-------|---------|
| `CsvParser` | Parses Windchill PLM CSV exports using CsvHelper. Case-insensitive headers, RFC 4180 support, UTF-8/BOM detection. |
| `WindchillRequirementMap` | Column-to-property mapping: `Number` -> `RequirementNumber`, `Name`, `Description`, `Type`, `State`, `Priority`, `Owner`, `Created On`, `Modified On`, `Module`, `Parent Number`, `Traced To`. |

### Data access (`Data/`)

| Class | Purpose |
|-------|---------|
| `TraceQDbContext` | EF Core context with `Requirements`, `ImportBatches`, `ImportRecords`, `DashboardLayouts`, `AuditLogEntries` tables. SQLite provider. |
| `RequirementRepository` | Full CRUD, keyword search, faceted queries, distribution aggregation, traceability coverage, embedding status tracking. |
| `DashboardLayoutRepository` | CRUD for named dashboard layouts stored as JSON. |

### Embeddings (`Embeddings/`)

| Class | Purpose |
|-------|---------|
| `OnnxEmbeddingService` | Loads all-MiniLM-L6-v2 ONNX model, generates 384-dim L2-normalized embeddings. Singleton, thread-safe. |
| `WordPieceTokenizer` | BERT-compatible tokenizer with Unicode NFC normalization, accent stripping, subword splitting, `[CLS]`/`[SEP]`/`[PAD]` tokens. Max 256 tokens. |
| `EmbeddingBackgroundService` | Hosted service that polls every 30s for unembedded requirements and processes them in batches of 50. |
| `EmbeddingModelOptions` | Configuration POCO bound to `EmbeddingModel` section (model path, vocab path, max sequence length, dimension). |

### Vector store (`VectorStore/`)

| Class | Purpose |
|-------|---------|
| `QdrantVectorStore` | Qdrant client wrapper. Creates collection on init, upserts in batches of 100, supports filtered cosine similarity search. |
| `QdrantOptions` | Configuration POCO bound to `Qdrant` section (host, ports, collection name). |

### Services (`Services/`)

| Class | Purpose |
|-------|---------|
| `ImportService` | End-to-end import pipeline: parse CSV, upsert to SQLite, track batch, trigger embedding generation. |
| `SearchService` | Semantic search (embed query -> Qdrant search -> hydrate from SQLite), find-similar, similarity clustering via union-find. |
| `AuditService` | Logs import/search/deletion events to SQLite with JSON-serialized detail payloads. |

### Health checks (`Health/`)

| Class | Purpose |
|-------|---------|
| `AirGapHealthCheck` | Verifies ONNX model file, vocab file, and Qdrant reachability on localhost. |

## DI registration

```csharp
// In Program.cs or host setup:
services.AddTraceQInfrastructure(connectionString);   // DbContext, repos, CSV parser, services
services.AddEmbeddingServices(configuration);          // ONNX model, tokenizer, background worker
services.AddQdrantVectorStore(configuration);          // Qdrant client, vector store
```

## NuGet dependencies

- CsvHelper 33.1.0
- Microsoft.EntityFrameworkCore.Sqlite 8.*
- Microsoft.ML.OnnxRuntime 1.24.4
- Qdrant.Client 1.17.0
- Microsoft.Extensions.Hosting.Abstractions 8.*

## Namespace conventions

```
TraceQ.Infrastructure.Csv          — CSV parsing and column mapping
TraceQ.Infrastructure.Data         — EF Core context and repositories
TraceQ.Infrastructure.Embeddings   — ONNX model, tokenizer, background service
TraceQ.Infrastructure.VectorStore  — Qdrant client and options
TraceQ.Infrastructure.Services     — Import, search, audit service implementations
TraceQ.Infrastructure.Health       — Air-gap health checks
```
