# TraceQ.Api.Tests

Unit and integration tests for the TraceQ API layer. Tests cover all REST controllers, CORS policy, and health endpoint behavior.

## Running

```bash
dotnet test
```

## Test classes

| Class | File | Coverage |
|-------|------|----------|
| `ImportControllerTests` | Controllers/ | CSV upload validation, import results, audit logging, batch history |
| `SearchControllerTests` | Controllers/ | Semantic search request validation, result handling, error cases |
| `RequirementsControllerTests` | Controllers/ | CRUD operations, facets, pagination, similarity search |
| `ReportsControllerTests` | Controllers/ | Distribution reports, traceability coverage, similarity clusters |
| `DashboardControllerTests` | Controllers/ | Dashboard layout CRUD and validation |
| `AuditControllerTests` | Controllers/ | Audit log pagination and event type filtering |
| `SecurityAndHealthTests` | Root | CORS policy validation, health endpoint integration |

## Approach

- Controller tests mock service dependencies via **Moq** and verify HTTP status codes, response bodies, and service interactions
- `SecurityAndHealthTests` uses `Microsoft.AspNetCore.Mvc.Testing` with an in-memory test server for integration-level verification
- Assertions use **FluentAssertions**

## Dependencies

- xUnit 2.5.3
- Moq 4.20.72
- FluentAssertions 8.9.0
- Microsoft.AspNetCore.Mvc.Testing 8.*
