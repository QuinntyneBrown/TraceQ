# TraceQ Software Design Documents

## System Overview

TraceQ is an air-gapped requirements intelligence platform for Space & Defense. It enables semantic search and analysis of Windchill PLM CSV exports locally with zero external cloud calls.

**Tech Stack:** .NET 8 / Angular 17+ / SQLite / Qdrant / ONNX Runtime (all-MiniLM-L6-v2)

## Design Documents

| # | Behaviour | Document |
|---|-----------|----------|
| 1 | [System Overview](01-system-overview/system-overview.md) | Architecture, C4 context/container/component diagrams |
| 2 | [Semantic Search](02-semantic-search/semantic-search.md) | Query embedding, vector search, filtering, ranking |
| 3 | [CSV Import Pipeline](03-csv-import/csv-import.md) | File upload, parsing, upsert, embedding generation |
| 4 | [Vector Embedding Generation](04-vector-embedding/vector-embedding.md) | ONNX inference, tokenization, background service |
| 5 | [Similarity Clustering](05-similarity-clustering/similarity-clustering.md) | Union-Find clustering, pairwise scoring |
| 6 | [Traceability Analysis](06-traceability-analysis/traceability-analysis.md) | Coverage calculation, untraced detection |
| 7 | [Requirements Management](07-requirements-management/requirements-management.md) | CRUD, facets, pagination, dual-database sync |
| 8 | [Dashboard & Reporting](08-dashboard-reporting/dashboard-reporting.md) | Distribution charts, layout persistence, widgets |
| 9 | [Audit Trail](09-audit-trail/audit-trail.md) | Event logging, query, retention |

## Diagram Conventions

- **Class Diagrams:** PlantUML (`.puml`) rendered to PNG
- **Sequence Diagrams:** PlantUML (`.puml`) rendered to PNG
- **C4 Diagrams:** Draw.io (`.drawio`) — Context, Container, and Component levels
- All diagrams are sized to fit US Letter pages (8.5" x 11")
