# TraceQ

Air-gapped requirements intelligence platform for Space & Defense. Import requirements from Windchill PLM CSV exports and perform semantic search, analysis, and reporting — all running locally with zero external cloud calls.

## Why TraceQ?

Defense contractors and government agencies need to analyze software and system requirements in environments where data cannot leave the network. TraceQ runs entirely on localhost using a bundled ONNX embedding model, a local vector database, and SQLite — no internet required.

## Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│   Angular 17+   │────▶│  ASP.NET Core 8  │────▶│  Qdrant (vectors)   │
│  Material UI    │     │  Kestrel :5000    │     │  SQLite (metadata)  │
└─────────────────┘     └──────────────────┘     └─────────────────────┘
                               │
                        ┌──────┴───────┐
                        │ ONNX Runtime │
                        │ MiniLM-L6-v2 │
                        └──────────────┘
```

**Clean Architecture** — three layers with clear separation of concerns:

| Layer | Project | Responsibility |
|-------|---------|----------------|
| API | `TraceQ.Api` | Controllers, middleware, Kestrel config |
| Core | `TraceQ.Core` | Domain models, DTOs, interfaces |
| Infrastructure | `TraceQ.Infrastructure` | CSV parsing, embeddings, Qdrant, SQLite |

## Features

- **CSV Import** — Parse Windchill PLM exports (RFC 4180 compliant) with batch tracking and audit logging
- **Local Embeddings** — Generate 384-dimensional vectors using all-MiniLM-L6-v2 via ONNX Runtime (CPU-only)
- **Vector Search** — Semantic and keyword search over requirements using Qdrant
- **Dashboard** — Interactive reporting with distribution charts, traceability coverage, and similarity clusters
- **Air-Gap Compliant** — Localhost-only binding, no outbound network calls, full audit trail via Serilog

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for the Angular frontend)
- [Qdrant](https://qdrant.tech/) running locally on port 6334

## Getting Started

### 1. Download the ONNX model

See [`models/README.md`](models/README.md) for instructions on downloading the all-MiniLM-L6-v2 model and vocabulary file into the `models/` directory.

### 2. Start Qdrant

```bash
./qdrant  # or run via Docker: docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

### 3. Build and run

```bash
dotnet build
dotnet run --project src/TraceQ.Api
```

The API starts on `http://localhost:5000`. CORS is configured for the Angular dev server at `http://localhost:4200`.

If local application-control policy blocks binaries under the repo path, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-api-unblocked.ps1
```

### 4. Run tests

```bash
dotnet test
```

If tests fail to load assemblies because binaries under the repo path are blocked, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-unblocked.ps1
```

## Tech Stack

| Component | Technology |
|-----------|------------|
| Backend | C# 12 / .NET 8 / ASP.NET Core |
| Frontend | Angular 17+ / Angular Material / Chart.js |
| Vector Store | Qdrant (local, gRPC) |
| Metadata DB | SQLite (EF Core 8) |
| Embeddings | all-MiniLM-L6-v2 (ONNX, 384-dim) |
| CSV Parsing | CsvHelper |
| Logging | Serilog (console + rolling file) |
| Testing | xUnit / Moq / FluentAssertions |

## Project Structure

```
TraceQ/
├── src/
│   ├── TraceQ.Api/            # ASP.NET Core host, controllers
│   ├── TraceQ.Core/           # Domain models, DTOs, interfaces
│   └── TraceQ.Infrastructure/ # Implementations (CSV, ONNX, Qdrant, SQLite)
├── tests/
│   ├── TraceQ.Api.Tests/
│   ├── TraceQ.Core.Tests/
│   └── TraceQ.Infrastructure.Tests/
├── docs/specs/                # L1 & L2 requirements
└── models/                    # ONNX model files (not checked in)
```

## Documentation

- [L1 — High-Level Requirements](docs/specs/L1.md)
- [L2 — Detailed Requirements & Acceptance Criteria](docs/specs/L2.md)
- [ONNX Model Setup](models/README.md)

## Contributing

See [CONTRIBUTORS.md](CONTRIBUTORS.md) for contribution guidelines and a list of contributors.

## License

This project is licensed under the [MIT License](LICENSE).
