# TraceQ

Air-gapped requirements intelligence platform for Space & Defense. Import requirements from Windchill PLM CSV exports and perform semantic search, analysis, and reporting — all running locally with zero external cloud calls.

## Why TraceQ?

Defense contractors and government agencies need to analyze software and system requirements in environments where data cannot leave the network. TraceQ runs entirely on localhost using a bundled ONNX embedding model, a local vector database, and SQLite — no internet required.

## Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│   Angular 21+   │────>│  ASP.NET Core 8  │────>│  Qdrant (vectors)   │
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

Install these before starting:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (npm 10.1.0+)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for running Qdrant)
- [Git](https://git-scm.com/)

## Getting Started

Follow these steps in order to clone, set up, and run TraceQ on a new machine.

### Step 1 — Clone the repository

```bash
git clone https://github.com/QuinntyneBrown/TraceQ.git
cd TraceQ
```

### Step 2 — Download the ONNX embedding model

The semantic search engine needs a local embedding model. Run the download script on a machine with internet access:

```powershell
powershell -ExecutionPolicy Bypass -File .\eng\scripts\download-model.ps1
```

This downloads three files into the `models/` directory:

| File | Size | Purpose |
|------|------|---------|
| `all-MiniLM-L6-v2.onnx` | ~86 MB | ONNX sentence-transformer model |
| `vocab.txt` | ~256 KB | WordPiece tokenizer vocabulary |
| `tokenizer.json` | ~466 KB | Tokenizer configuration |

For air-gapped environments, run the script on a connected machine and transfer the `models/` folder via approved media. See [`models/README.md`](models/README.md) for alternative download methods.

### Step 3 — Start Qdrant (vector database)

TraceQ stores embeddings in Qdrant. Start it with Docker:

```bash
docker run -d --name traceq-qdrant --restart unless-stopped -p 6333:6333 -p 6334:6334 -v traceq-qdrant-data:/qdrant/storage qdrant/qdrant
```

Verify it's running:

```bash
curl http://localhost:6333/collections
```

You should see a JSON response (e.g. `{"result":{"collections":[]}}`). Qdrant must be running before starting the backend.

### Step 4 — Install frontend dependencies

```bash
cd src/TraceQ.Web
npm install
cd ../..
```

### Step 5 — Build and run the backend

```bash
cd src/TraceQ.Api
dotnet run
```

The API starts on **http://localhost:5000**. On first launch it will:

1. Create the SQLite database at `src/TraceQ.Api/data/traceq.db`
2. Connect to Qdrant and create the `requirements` collection
3. Start the background embedding service

Check the health endpoint to confirm everything is connected:

```bash
curl http://localhost:5000/health
```

A response of `Healthy` means all systems are go. Any other result means a required dependency is missing or unavailable — go back to Step 3.

> **Execution policy issues on Windows?** If local policy blocks binaries under the repo path:
> ```powershell
> powershell -ExecutionPolicy Bypass -File .\eng\scripts\run-api-unblocked.ps1
> ```

### Step 6 — Run the frontend

In a separate terminal:

```bash
cd src/TraceQ.Web
npm start
```

The Angular dev server starts on **http://localhost:4200**.

### Step 7 — Import sample data

Open **http://localhost:4200** in your browser and navigate to the **Import** page (upload icon in the sidebar). Upload the included sample CSV:

```
docs/specs/sample-requirements.csv
```

This imports 30 aerospace requirements. The background service will automatically generate embeddings — check the API console logs for `Completed embedding 30 requirements`.

### Step 8 — Search

Navigate to the **Search** page and try queries like:

- `thermal protection system`
- `propulsion delta-v`
- `radiation hardness`

Results are ranked by semantic similarity. Use the facet filters (Type, State, Priority, Module, Owner) to narrow results.

## Quick Start (all-in-one)

If you've completed the setup steps above at least once, you can start everything with a single command:

```bash
eng\scripts\run.bat
```

This opens two terminal windows — one for the backend and one for the frontend.

> **Note:** Qdrant must already be running. Start it with `docker start qdrant` if you've previously created the container.
> For the default setup in this repo, the persistent container name is `traceq-qdrant`, so use `docker start traceq-qdrant`.

## Running Tests

### Backend (.NET)

```bash
dotnet test
```

If binaries are blocked by execution policy:

```powershell
powershell -ExecutionPolicy Bypass -File .\eng\scripts\test-unblocked.ps1
```

### Frontend (Angular)

```bash
cd src/TraceQ.Web
npm test
```

## Configuration

All backend configuration is in `src/TraceQ.Api/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `Urls` | `http://localhost:5000` | API bind address |
| `ConnectionStrings:Sqlite` | `Data Source=./data/traceq.db` | SQLite database path |
| `Qdrant:Host` | `localhost` | Qdrant server host |
| `Qdrant:GrpcPort` | `6334` | Qdrant gRPC port |
| `Qdrant:HttpPort` | `6333` | Qdrant REST port |
| `Qdrant:CollectionName` | `requirements` | Vector collection name |
| `EmbeddingModel:ModelPath` | `./models/all-MiniLM-L6-v2.onnx` | ONNX model file path |
| `EmbeddingModel:VocabPath` | `./models/vocab.txt` | Vocabulary file path |

## Tech Stack

| Component | Technology |
|-----------|------------|
| Backend | C# 12 / .NET 8 / ASP.NET Core |
| Frontend | Angular 21 / Angular Material / Chart.js |
| Vector Store | Qdrant (local, gRPC) |
| Metadata DB | SQLite (EF Core 8) |
| Embeddings | all-MiniLM-L6-v2 (ONNX, 384-dim) |
| CSV Parsing | CsvHelper |
| Logging | Serilog (console + rolling file) |
| Testing | xUnit / Moq / FluentAssertions (backend), Vitest (frontend) |

## Project Structure

```
TraceQ/
├── src/
│   ├── TraceQ.Api/              # ASP.NET Core host, controllers
│   ├── TraceQ.Core/             # Domain models, DTOs, interfaces
│   ├── TraceQ.Infrastructure/   # Implementations (CSV, ONNX, Qdrant, SQLite)
│   └── TraceQ.Web/              # Angular frontend (multi-project workspace)
├── tests/
│   ├── TraceQ.Api.Tests/
│   ├── TraceQ.Core.Tests/
│   └── TraceQ.Infrastructure.Tests/
├── models/                      # ONNX model files (not checked in)
├── docs/specs/                  # L1 & L2 requirements, sample CSV
└── eng/scripts/                 # Build, run, and setup scripts
```

## Documentation

- [L1 — High-Level Requirements](docs/specs/L1.md)
- [L2 — Detailed Requirements & Acceptance Criteria](docs/specs/L2.md)
- [ONNX Model Setup](models/README.md)

## Contributing

See [CONTRIBUTORS.md](CONTRIBUTORS.md) for contribution guidelines and a list of contributors.

## License

This project is licensed under the [MIT License](LICENSE).
