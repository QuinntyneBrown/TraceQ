# TraceQ Startup & Sample CSV Import — Issues Report

**Date:** 2026-03-25
**Tested by:** Claude Code
**Sample CSV:** `docs/specs/sample-requirements.csv` (41 requirements)

---

## Summary

| Category | Count |
|----------|-------|
| Critical (blocks startup) | 2 |
| High (functional gap) | 2 |
| Medium (correctness/config) | 3 |
| Low (quality/DX) | 3 |

---

## Critical Issues

### 1. ONNX Model Files Missing — Backend Crashes on Startup

**Severity:** Critical
**Component:** Backend startup (`OnnxEmbeddingService`)
**Error:**
```
System.IO.FileNotFoundException: ONNX model file not found at './models/all-MiniLM-L6-v2.onnx'.
Run scripts/download-model.ps1 to download it.
```

**Root Cause:** The `models/` directory at the repo root contains only `README.md`. The ONNX model (~86 MB), `vocab.txt`, and `tokenizer.json` must be downloaded before the API will start.

**Additional Problem:** `appsettings.json` references `./models/all-MiniLM-L6-v2.onnx` (relative path), which resolves relative to the working directory. When running via `dotnet run` from `src/TraceQ.Api/`, it looks for `src/TraceQ.Api/models/`, NOT the repo-root `models/` directory. The download script places files in the repo-root `models/` directory, so even after downloading, the path mismatch persists.

**Fix Options:**
- (a) Change `appsettings.json` paths to `../../models/all-MiniLM-L6-v2.onnx` (relative to Api project)
- (b) Add a post-build copy step to copy model files into the output directory
- (c) Use absolute or repo-root-relative paths

---

### 2. `download-model.ps1` Fails on PowerShell 5.1

**Severity:** Critical
**Component:** `scripts/download-model.ps1` (line 21)
**Error:**
```
Join-Path : A positional parameter cannot be found that accepts argument 'models'.
```

**Root Cause:** `Join-Path $PSScriptRoot ".." "models"` uses 3-argument `Join-Path`, which requires PowerShell 6+. Windows ships with PowerShell 5.1 by default.

**Fix:** Replace with nested calls:
```powershell
$modelsDir = Join-Path (Join-Path $PSScriptRoot "..") "models"
```

---

## High Issues

### 3. Stale Build Artifacts Cause False Compilation Errors

**Severity:** High
**Component:** Build system
**Error:**
```
error CS0234: The type or namespace name 'Utilities' does not exist in the namespace 'TraceQ.Core'
```

**Root Cause:** The `TraceQ.Core.Utilities.TraceLinkParser` class exists and compiles correctly, but stale `obj/` artifacts from a prior build caused the incremental build to fail. A `dotnet clean && dotnet build` resolved it.

**Impact:** First-time developers cloning the repo and running `dotnet run` will encounter this if any partial build state exists.

**Fix:** The `run.bat` script could include a `dotnet build` step before `dotnet run`, or documentation should note to run `dotnet clean` if build errors appear.

---

### 4. No Qdrant Dependency Check or Startup Instructions

**Severity:** High
**Component:** Backend startup / Developer experience
**Observation:** The backend requires a Qdrant vector database running on `localhost:6334`. If Qdrant is not running, the app starts in "degraded mode" but there are no instructions in the startup script or README about how to start Qdrant. The `run.bat` does not check for or start Qdrant.

**Impact:** Semantic search and similarity clustering silently return empty results if Qdrant is unavailable. In this test, Qdrant happened to be running, but new developers would not know this dependency exists.

**Fix:** Add Qdrant startup (e.g., `docker run -d -p 6333:6333 -p 6334:6334 qdrant/qdrant`) to `run.bat` or add a prerequisite check.

---

## Medium Issues

### 5. `.gitignore` Does Not Cover Copied Model Files in `src/TraceQ.Api/models/`

**Severity:** Medium
**Component:** Git configuration
**Observation:** `.gitignore` has `models/*.onnx` which covers the repo-root `models/` directory. However, due to Issue #1, model files need to be copied to `src/TraceQ.Api/models/` for the app to find them. This path is NOT gitignored, meaning the 86 MB ONNX model could be accidentally committed.

**Fix:** Either fix the path resolution (Issue #1) or add `src/TraceQ.Api/models/` to `.gitignore`.

---

### 6. Connection String Key Mismatch

**Severity:** Medium
**Component:** `Program.cs` line 59-62 / `appsettings.json`
**Observation:** `Program.cs` first looks for `GetConnectionString("DefaultConnection")`, then falls back to `GetConnectionString("Sqlite")`. The `appsettings.json` only defines `"Sqlite"`. This works due to the fallback chain, but is confusing — the primary key name doesn't match the config.

**Fix:** Either rename the config key to `"DefaultConnection"` or change Program.cs to look for `"Sqlite"` first.

---

### 7. CSV Last Row Is Empty

**Severity:** Medium
**Component:** `docs/specs/sample-requirements.csv`
**Observation:** The CSV file has a trailing empty line (line 43 is empty). The import service handled this gracefully (no error), but it's worth noting for data quality. The import reported 41 inserted rows from a 42-line file (header + 41 data rows + 1 empty), which is correct.

---

## Low Issues

### 8. Similarity Clusters Returns Empty Array

**Severity:** Low
**Component:** `GET /api/reports/similarity-clusters`
**Observation:** The endpoint returns `[]` (empty array) even though 41 requirements are embedded and semantic search works. The default threshold of 0.85 may be too high for the sample data set, or the clustering implementation may need review.

**Impact:** The similarity detection feature appears non-functional with the sample data.

---

### 9. `run.bat` Uses Hardcoded Absolute Paths

**Severity:** Low
**Component:** `eng/scripts/run.bat`
**Observation:** The script uses `C:\projects\TraceQ\...` which only works on this specific machine. Should use `%~dp0` (script directory) with relative paths to be portable.

---

### 10. Preview .NET SDK Warning

**Severity:** Low
**Component:** Build toolchain
**Observation:** The installed .NET SDK (`9.0.300-preview.0.25177.5`) is a preview version building a .NET 8 project. This emits `NETSDK1057` warnings during every build. A stable .NET 8 or 9 SDK should be used instead.

---

## Import Results Summary

| Metric | Value |
|--------|-------|
| File | `sample-requirements.csv` |
| Total rows parsed | 41 |
| Inserted | 41 |
| Updated | 0 |
| Errors | 0 |
| Skipped | 0 |
| Duplicate re-import skipped | 41 (correct) |
| Embedding status | All 41 marked `isEmbedded: true` |
| Traceability coverage | 60.98% (25 traced / 16 untraced) |

## API Endpoints Tested

| Endpoint | Method | Status | Notes |
|----------|--------|--------|-------|
| `/health` | GET | 200 OK | Returns "Healthy" |
| `/api/import/csv` | POST | 200 OK | 41 records imported successfully |
| `/api/import/csv` (duplicate) | POST | 200 OK | 41 skipped (correct) |
| `/api/import/csv` (no file) | POST | 400 | Proper validation error |
| `/api/import/csv` (non-CSV) | POST | 400 | "Only .csv files are accepted." |
| `/api/import/history` | GET | 200 OK | Shows 2 import batches |
| `/api/requirements` | GET | 200 OK | Paginated, 41 total |
| `/api/requirements?page=2` | GET | 200 OK | Remaining 21 items |
| `/api/search` | POST | 200 OK | Semantic search works, returns ranked results |
| `/api/reports/distribution/state` | GET | 200 OK | 31 Approved, 8 Under Review, 2 Draft |
| `/api/reports/distribution/invalid` | GET | 400 | Proper validation error |
| `/api/reports/traceability` | GET | 200 OK | 60.98% coverage |
| `/api/reports/similarity-clusters` | GET | 200 OK | Returns [] (see Issue #8) |
| `/api/dashboard/layouts` | GET | 200 OK | Returns [] (no layouts saved yet) |
| `/api/audit` | GET | 200 OK | Shows import + search audit entries |
| Frontend (http://localhost:4200) | GET | 200 OK | Angular app loads |
