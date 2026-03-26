# TraceQ.Api

ASP.NET Core 8 Web API host for TraceQ. Exposes REST endpoints for CSV import, semantic search, reporting, dashboard layouts, and audit logging. Configured for air-gapped operation with localhost-only binding.

## Running

```bash
dotnet run
```

Starts on `http://localhost:5000`. Requires Qdrant running locally and the ONNX model files in `./models/`.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/import/csv` | Upload and import a Windchill CSV (multipart, 50 MB max) |
| GET | `/api/import/history` | Paginated import batch history |
| GET | `/api/import/{batchId}` | Detail for a specific import batch |
| POST | `/api/search` | Semantic search over requirements |
| GET | `/api/requirements` | Paginated list with optional keyword search |
| GET | `/api/requirements/facets` | Distinct values/counts for filter dropdowns |
| GET | `/api/requirements/{id}` | Single requirement detail |
| GET | `/api/requirements/{id}/similar` | Find semantically similar requirements |
| DELETE | `/api/requirements/{id}` | Delete requirement from SQLite and Qdrant |
| GET | `/api/reports/distribution/{field}` | Count distribution by type/state/priority/module/owner |
| GET | `/api/reports/traceability` | Traceability coverage metrics |
| GET | `/api/reports/similarity-clusters` | Clusters of highly similar requirements |
| GET | `/api/dashboard/layouts` | List saved dashboard layouts |
| POST | `/api/dashboard/layouts` | Save a dashboard layout |
| DELETE | `/api/dashboard/layouts/{id}` | Delete a saved layout |
| GET | `/api/audit` | Paginated audit log with optional event type filter |
| GET | `/health` | Health check (air-gap compliance, model files, Qdrant) |

## Configuration

All settings in `appsettings.json`:

| Key | Default | Description |
|-----|---------|-------------|
| `Urls` | `http://localhost:5000` | Kestrel bind address (localhost only) |
| `ConnectionStrings:Sqlite` | `Data Source=./data/traceq.db` | SQLite path |
| `Qdrant:Host` | `localhost` | Qdrant server |
| `Qdrant:GrpcPort` | `6334` | Qdrant gRPC port |
| `Qdrant:HttpPort` | `6333` | Qdrant REST port |
| `Qdrant:CollectionName` | `requirements` | Vector collection name |
| `EmbeddingModel:ModelPath` | `./models/all-MiniLM-L6-v2.onnx` | ONNX model file |
| `EmbeddingModel:VocabPath` | `./models/vocab.txt` | Tokenizer vocabulary |

## Dependencies

- **TraceQ.Core** — domain models, DTOs, interfaces
- **TraceQ.Infrastructure** — service implementations, data access, embeddings
- **Serilog.AspNetCore** — structured logging to console and rolling file (`./logs/`)

## Security

- Kestrel binds only to `127.0.0.1:5000` (not network-accessible)
- CORS allows only `http://localhost:4200`
- No outbound network calls — all ML inference and storage is local
- Audit trail logs imports, searches, and deletions
