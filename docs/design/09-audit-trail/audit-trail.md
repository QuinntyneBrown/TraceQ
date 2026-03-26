# Audit Trail -- Detail Design

## 1. Overview

The Audit Trail provides a comprehensive event log for all significant system operations: imports, searches, and deletions. Each event is recorded in SQLite with a timestamp, event type, and JSON details. The audit log is queryable by event type with pagination, providing accountability and traceability for compliance in Space & Defense environments. Serilog also writes structured log entries to rolling daily files.

## 2. Components

### 2.1 Frontend

#### AuditService (Angular API Client)

| Method | Signature |
|---|---|
| `list` | `list(params?: AuditQueryParams): Observable<PaginatedResult<AuditLogEntry>>` |

Params: `eventType` (optional filter: Import, Search, Delete), `page`, `pageSize`.

### 2.2 Backend

#### AuditController

| Endpoint | Method | Description |
|---|---|---|
| `/api/audit` | GET | Query audit log entries. Query params: `eventType` (optional filter: Import/Search/Delete), `page`, `pageSize`. |

Delegates to `IAuditService.GetLogsAsync`.

#### IAuditService / AuditService

| Method | Description |
|---|---|
| `LogImportAsync(fileName, insertedCount, updatedCount, errorCount)` | Creates `AuditLogEntry` with `EventType=Import`, Details JSON = `{fileName, inserted, updated, errors}`. |
| `LogSearchAsync(query, resultCount)` | Creates `AuditLogEntry` with `EventType=Search`, Details JSON = `{query, resultCount}`. |
| `LogDeletionAsync(requirementNumber)` | Creates `AuditLogEntry` with `EventType=Delete`, Details JSON = `{requirementNumber}`. |
| `GetLogsAsync(eventType?, page, pageSize)` | Queries `AuditLogEntries` table with optional `eventType` filter, ordered by `Timestamp DESC`, paginated. |

All write methods also log via Serilog for a file-based audit trail.

### 2.3 Domain Models

#### AuditLogEntry

| Field | Type | Description |
|---|---|---|
| Id | Guid | Primary key. |
| EventType | AuditEventType | Enum value indicating the type of event. |
| Details | string | JSON-serialized event details. |
| Timestamp | DateTime | UTC timestamp when the event occurred. |

#### AuditEventType

| Value | Description |
|---|---|
| Import | A CSV import operation completed. |
| Search | A semantic search was performed. |
| Delete | A requirement was deleted. |

### 2.4 Callers (who writes audit entries)

| Caller | When | Method Called |
|---|---|---|
| ImportController | After successful import | `LogImportAsync` |
| SearchController | After search execution | `LogSearchAsync` |
| RequirementsController | After requirement deletion | `LogDeletionAsync` |

### 2.5 Data Stores

| Store | Usage |
|---|---|
| **SQLite** | `AuditLogEntries` table with index on `Timestamp` for efficient ordering. |
| **File system** | Serilog rolling daily logs at `./logs/traceq-{date}.log`. |

## 3. Class Diagram

![Class Diagram](class-diagram.puml.png)

## 4. Sequence Diagram

![Sequence Diagram](sequence-diagram.puml.png)

## 5. C4 Diagram

![C4 Diagram](c4-audit-trail.drawio.png)
