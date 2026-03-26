# Backend Recovery Runbook

This document explains how to get the TraceQ backend into a fully working state, which means:

- the ONNX embedding model is present and loaded
- Qdrant is reachable on `localhost`
- the API reports healthy dependency status
- imported requirements are embedded into Qdrant
- semantic search returns results instead of an empty list

## Current failure mode

When either dependency is missing, the backend starts in degraded mode:

- if the ONNX model files are missing, TraceQ falls back to a dummy embedding service
- if Qdrant is down, the vector store accepts no writes and returns no search results

In that state:

- `GET /health` returns `Degraded`
- `POST /api/search` returns `[]`
- imported requirements remain `isEmbedded: false`

This is expected behavior. Semantic search only works after both the model and Qdrant are available and the API has been restarted.

## Required end state

The backend is working as expected when all of the following are true:

1. `models/all-MiniLM-L6-v2.onnx` exists.
2. `models/vocab.txt` exists.
3. Qdrant is reachable at:
   - HTTP: `http://localhost:6333`
   - gRPC: `localhost:6334`
4. `GET http://localhost:5000/health` returns `Healthy`.
5. Requirements have been embedded into Qdrant.
6. `POST /api/search` returns actual results for relevant queries.

## Configuration used by the backend

The API reads these settings from [appsettings.json](C:/projects/TraceQ/src/TraceQ.Api/appsettings.json):

```json
{
  "Qdrant": {
    "Host": "localhost",
    "HttpPort": 6333,
    "GrpcPort": 6334,
    "CollectionName": "requirements"
  },
  "EmbeddingModel": {
    "ModelPath": "./models/all-MiniLM-L6-v2.onnx",
    "VocabPath": "./models/vocab.txt",
    "MaxSequenceLength": 256,
    "EmbeddingDimension": 384
  }
}
```

Unless those settings are changed, all recovery work should target those paths and ports.

## Step 1: Install the model files

TraceQ expects these files in the repo `models/` directory:

- `models/all-MiniLM-L6-v2.onnx`
- `models/vocab.txt`

The existing project instructions are in [README.md](C:/projects/TraceQ/models/README.md).

If you are on an internet-connected machine, use the bundled script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\download-model.ps1
```

That script downloads:

- `all-MiniLM-L6-v2.onnx`
- `vocab.txt`
- `tokenizer.json`

Only the first two are required by the backend, but copying the whole `models/` folder is fine.

For an air-gapped environment:

1. Run the download script on an approved connected machine.
2. Copy the `models/` directory to the TraceQ project root on the target machine.
3. Verify the files exist:

```powershell
Get-ChildItem .\models
```

Expected output should include:

- `all-MiniLM-L6-v2.onnx`
- `vocab.txt`

## Step 2: Start Qdrant on the expected ports

TraceQ requires Qdrant on:

- HTTP `6333`
- gRPC `6334`

The repo README already documents the expected startup model:

```bash
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

If you use a local binary instead of Docker, it still must expose:

- `http://localhost:6333/healthz`
- gRPC on `localhost:6334`

Verify Qdrant is up:

```powershell
curl.exe http://localhost:6333/healthz
```

Expected result:

```text
healthz check passed
```

If that endpoint does not respond, the backend will remain degraded and semantic search will not work.

## Step 3: Restart the API after both dependencies are ready

This part is important.

TraceQ decides dependency availability at startup:

- the embedding service is selected during DI registration
- Qdrant connectivity is checked during vector store initialization

That means:

- adding the model files after the API is already running is not enough
- starting Qdrant after the API is already running is not enough

You must restart the API after both dependencies are in place.

### Recommended command in this environment

This machine uses local Application Control rules that may block binaries under the repo path. Use the bundled workaround script:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-api-unblocked.ps1
```

### Normal command

If App Control is not a problem on the target machine, this is the standard startup:

```powershell
dotnet run --project .\src\TraceQ.Api
```

## Step 4: Verify backend health

After restart, verify the backend:

```powershell
curl.exe http://localhost:5000/health
```

Expected result:

```text
Healthy
```

If the response is still `Degraded`, one of these is still wrong:

- model file path
- vocab file path
- Qdrant HTTP health endpoint
- Qdrant gRPC availability

## Step 5: Ensure requirements are embedded

Semantic search only works after requirements have vectors in Qdrant.

TraceQ handles this automatically in two ways:

- inline after import
- background polling every 30 seconds

The background service only processes rows when both dependencies are available.

### If you already imported data while the backend was degraded

That is recoverable. The rows stay marked as `isEmbedded: false`, so after the backend is restarted in a healthy state, the background service should pick them up automatically.

Recommended sequence:

1. Start Qdrant.
2. Place model files.
3. Restart the API.
4. Wait at least 30 to 60 seconds.
5. Test search.

### Fastest way to force fresh embeddings

If you want deterministic validation, re-import the sample CSV after the backend is healthy:

```powershell
curl.exe -X POST -F "file=@tests/TestData/windchill_export_sample.csv" http://localhost:5000/api/import/csv
```

That path should:

- store the requirements in SQLite
- generate embeddings
- upsert vectors into Qdrant

## Step 6: Validate semantic search

Use the semantic search endpoint directly:

```powershell
curl.exe -X POST ^
  -H "Content-Type: application/json" ^
  -d "{\"query\":\"thermal control\",\"top\":5}" ^
  http://localhost:5000/api/search
```

With the sample CSV imported and the backend healthy, queries like these should return results:

- `thermal control`
- `reaction wheel`
- `power budget`
- `antenna pointing`
- `radiation hardness`
- `flight software fault tolerance`

These are based on requirement names and descriptions in [windchill_export_sample.csv](C:/projects/TraceQ/tests/TestData/windchill_export_sample.csv).

## Operational checklist

Use this exact order when recovering the backend:

1. Verify `models/all-MiniLM-L6-v2.onnx` exists.
2. Verify `models/vocab.txt` exists.
3. Start Qdrant on ports `6333` and `6334`.
4. Confirm `http://localhost:6333/healthz` is reachable.
5. Restart the API.
6. Confirm `http://localhost:5000/health` returns `Healthy`.
7. Re-import sample data or wait for the background embedder.
8. Test `POST /api/search`.

## Troubleshooting

### `GET /health` says `Degraded`

Cause:

- missing model files, missing vocab file, or Qdrant unavailable

Action:

- verify the file paths in `models/`
- verify Qdrant health on `6333`
- restart the API after fixing either dependency

### `POST /api/search` returns `[]` even though `GET /health` is `Healthy`

Cause:

- imported requirements may not have been embedded yet

Action:

- wait for the background service polling interval
- or re-import the CSV once the backend is healthy

### Qdrant starts but search is still empty after re-import

Cause:

- API was still running in degraded mode when Qdrant came up

Action:

- restart the API after Qdrant is already available

### The API will not start from the repo path on this machine

Cause:

- local Application Control policy blocks assemblies under the workspace path

Action:

- run [run-api-unblocked.ps1](C:/projects/TraceQ/scripts/run-api-unblocked.ps1)

### Tests fail to load assemblies under the repo path

Cause:

- same Application Control behavior as above

Action:

- run [test-unblocked.ps1](C:/projects/TraceQ/scripts/test-unblocked.ps1)

## Expected steady-state behavior

Once the recovery steps are complete:

- backend health is `Healthy`
- imports embed requirements successfully
- Qdrant contains vectors for imported requirements
- semantic search returns ranked results
- “Search always shows no results” stops being true unless the query is genuinely unrelated
