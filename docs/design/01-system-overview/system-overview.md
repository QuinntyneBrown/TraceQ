# System Overview — Detail Design

## 1. Overview

TraceQ is a locally-deployed requirements intelligence platform designed for air-gapped Space & Defense environments. It ingests Windchill PLM CSV exports, stores requirements in SQLite, generates 384-dimensional vector embeddings via a local ONNX model (all-MiniLM-L6-v2), indexes them in Qdrant for semantic search, and presents an Angular Material UI for search, import, reporting, and audit.

### Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| Air-gapped, localhost-only | Compliance with L2-7.1 for classified environments |
| Dual database (SQLite + Qdrant) | Relational metadata + high-performance vector similarity |
| ONNX Runtime (CPU) | No GPU dependency; deterministic, portable inference |
| Angular monorepo with libraries | Separation of API, components, features for testability |
| Background embedding service | Decouples import latency from embedding generation |

### Deployment Topology

All components run on a single machine:

- **Kestrel** on `localhost:5000` (API)
- **Angular dev server** on `localhost:4200` (or static files served by Kestrel)
- **Qdrant** on `localhost:6334` (gRPC)
- **SQLite** file at `./data/traceq.db`
- **ONNX model** at `./models/all-MiniLM-L6-v2.onnx`

## 2. C4 Diagrams

### 2.1 Context Diagram

![C4 Context Diagram](c4-context.drawio.png)

*Source: [c4-context.drawio](c4-context.drawio)*

### 2.2 Container Diagram

![C4 Container Diagram](c4-container.drawio.png)

*Source: [c4-container.drawio](c4-container.drawio)*

### 2.3 Component Diagram — Backend

![C4 Component Diagram — Backend](c4-component-backend.drawio.png)

*Source: [c4-component-backend.drawio](c4-component-backend.drawio)*

### 2.4 Component Diagram — Frontend

![C4 Component Diagram — Frontend](c4-component-frontend.drawio.png)

*Source: [c4-component-frontend.drawio](c4-component-frontend.drawio)*

## 3. Layered Architecture

```
┌──────────────────────────────────────────────────────────┐
│  Presentation Layer  (Angular 17+ / Material / Chart.js) │
│  - 4 feature pages + 16 shared components + API library  │
└────────────────────────┬─────────────────────────────────┘
                         │ HTTP REST / JSON / CORS
┌────────────────────────▼─────────────────────────────────┐
│  API Layer  (ASP.NET Core / Kestrel / localhost:5000)     │
│  - 6 Controllers, 18+ endpoints                          │
└────────────────────────┬─────────────────────────────────┘
                         │ Dependency Injection
┌────────────────────────▼─────────────────────────────────┐
│  Core Layer  (TraceQ.Core)                                │
│  - Domain Models (5), DTOs (16), Service Interfaces (8)   │
└────────────────────────┬─────────────────────────────────┘
                         │ Implementation
┌────────────────────────▼─────────────────────────────────┐
│  Infrastructure Layer  (TraceQ.Infrastructure)            │
│  - EF Core Repos, ONNX Embeddings, Qdrant, CsvParser     │
│  - EmbeddingBackgroundService (30s poll)                   │
└──────────────────────────────────────────────────────────┘
```

## 4. Component Catalogue

### 4.1 Domain Models

| Model | Purpose | Key Fields |
|-------|---------|------------|
| `Requirement` | PLM requirement record | Id, RequirementNumber, Name, Description, Type, State, Priority, Owner, Module, ParentNumber, TracedTo, IsEmbedded |
| `ImportBatch` | CSV import transaction | Id, FileName, ImportedAt, InsertedCount, UpdatedCount, ErrorCount, SkippedCount |
| `ImportRecord` | Per-row import result | Id, ImportBatchId, RequirementNumber, Status, ErrorMessage |
| `AuditLogEntry` | Event trail | Id, EventType, Details (JSON), Timestamp |
| `DashboardLayout` | Saved widget layout | Id, Name, LayoutJson, CreatedAt, UpdatedAt |

### 4.2 Service Interfaces

| Interface | Responsibility | Implementation |
|-----------|---------------|----------------|
| `ISearchService` | Semantic search, similarity, clustering | `SearchService` |
| `IVectorStore` | Vector CRUD + similarity search | `QdrantVectorStore` |
| `IEmbeddingService` | Text-to-vector via ONNX | `OnnxEmbeddingService` |
| `IImportService` | CSV import orchestration | `ImportService` |
| `ICsvParser` | CSV file parsing | `CsvParser` |
| `IRequirementRepository` | Requirement data access | `RequirementRepository` |
| `IDashboardLayoutRepository` | Layout persistence | `DashboardLayoutRepository` |
| `IAuditService` | Event logging | `AuditService` |

### 4.3 Controllers

| Controller | Route Prefix | Endpoints |
|------------|-------------|-----------|
| `SearchController` | `/api/search` | POST search |
| `RequirementsController` | `/api/requirements` | GET list, GET by id, GET facets, GET similar, DELETE |
| `ImportController` | `/api/import` | POST csv, GET history, GET batch detail |
| `ReportsController` | `/api/reports` | GET distribution, GET traceability, GET clusters |
| `DashboardController` | `/api/dashboard` | GET/POST/DELETE layouts |
| `AuditController` | `/api/audit` | GET logs |

### 4.4 Frontend Libraries

| Library | Purpose | Contents |
|---------|---------|----------|
| `api` | HTTP client layer | 6 services, 16+ TypeScript interfaces |
| `components` | Shared UI | 16 Material-based components (buttons, dialogs, toast, etc.) |
| `features` | Page modules | Dashboard, Search, Import, Requirements |
| `trace-q` | Shell app | Routing, layout, config |

## 5. Cross-Cutting Concerns

### 5.1 Logging

- **Serilog** with console + rolling daily file sinks (`./logs/traceq-{date}.log`)
- Structured logging with context enrichment

### 5.2 Error Handling

- Global exception handler in ASP.NET Core pipeline
- Per-row error capture in CSV import
- Frontend: `TqToastService` for user-facing errors, `TqErrorPageComponent` for fatal errors

### 5.3 Health Checks

- `AirGapHealthCheck`: Verifies ONNX model files exist and Qdrant is reachable
- Exposed at `/health` endpoint

### 5.4 Security

- Kestrel bound to `127.0.0.1` only (no public interface)
- CORS restricted to `localhost:4200`
- No external HTTP clients, no cloud API keys
- Comprehensive audit trail for all mutations and queries

## 6. Class Diagram — Full System

![System Class Diagram](class-diagram.puml.png)

*Source: [class-diagram.puml](class-diagram.puml)*
