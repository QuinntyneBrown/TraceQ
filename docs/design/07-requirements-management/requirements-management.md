# Requirements Management - Detail Design

## 1. Overview

Requirements Management provides CRUD operations for PLM requirements with faceted browsing, keyword search, pagination, sorting, and dual-database synchronization. The frontend displays a searchable, sortable table of all requirements with detail views and delete capability. Deletion removes records from both SQLite and Qdrant to maintain consistency.

## 2. Components

### 2.1 Frontend

#### RequirementsComponent

Angular page routed at `/requirements`. Provides the primary user interface for browsing, inspecting, and managing requirements.

**Template elements:**

- **Search input** - A text input field for keyword search across RequirementNumber, Name, and Description. Debounced to avoid excessive API calls during typing.
- **Requirements table** - Displays requirements with columns: Number, Name, Type, State, Priority, Module. Each column header supports click-to-sort with ascending/descending toggle.
- **Pagination controls** - Page navigation with configurable page size (default 20, max 100). Displays current page, total pages, and total count.
- **Row actions** - Each row provides a "View" button (opens detail dialog), a "Delete" button (opens destructive confirmation dialog), and a "Find Similar" button (navigates to the search page with the requirement pre-selected).
- **Loading indicator** - Shown while list or detail requests are in flight.

**Behavior:**

- On initialization, calls `RequirementsService.list()` with default parameters to load the first page.
- Keyword search triggers a debounced call to `RequirementsService.list({ search })`, resetting to page 1.
- Column header clicks update `sortBy` and `sortDirection` parameters, then reload.
- Pagination controls update `page` and `pageSize` parameters, then reload.
- "View" opens `TqDetailDialogComponent` with the full requirement record.
- "Delete" opens `TqDestructiveDialogComponent`; on confirmation, calls `RequirementsService.delete(id)` and refreshes the table.
- "Find Similar" navigates to `/search` with the requirement ID as a route parameter.

#### RequirementsService (Angular API Client)

Handles HTTP communication with the backend requirements endpoints.

- `list(params?: { search?, sortBy?, sortDirection?, page?, pageSize? }): Observable<PaginatedResult<Requirement>>` - Calls `GET /api/requirements` with query parameters and returns paginated results.
- `get(id: string): Observable<Requirement>` - Calls `GET /api/requirements/{id}` to fetch a single requirement.
- `getSimilar(id: string, top?: number): Observable<SearchResult[]>` - Calls `GET /api/requirements/{id}/similar` to find similar requirements.
- `delete(id: string): Observable<void>` - Calls `DELETE /api/requirements/{id}` to remove a requirement from both data stores.
- `getFacets(): Observable<Facets>` - Calls `GET /api/requirements/facets` to retrieve distinct values for each facet field with counts.

#### TqDetailDialogComponent

A read-only dialog component that displays all fields of a requirement record. Shows: RequirementNumber, Name, Description, Type, State, Priority, Module, Owner, CreatedAt, UpdatedAt, and the TracedTo list (related requirement numbers). Opened from the requirements table row action.

#### TqDestructiveDialogComponent

A confirmation dialog for destructive actions (deletion). Displays a warning message explaining that deletion removes the requirement from both SQLite and Qdrant and cannot be undone. Requires the user to explicitly confirm before proceeding. Returns a boolean result to the caller.

### 2.2 Backend

#### RequirementsController

REST controller providing CRUD endpoints for requirements.

**Endpoints:**

- **GET /api/requirements** - List requirements with optional query parameters:

  | Parameter     | Type   | Default | Description                                          |
  |---------------|--------|---------|------------------------------------------------------|
  | search        | string | null    | Keyword search across number, name, description      |
  | sortBy        | string | "number"| Column to sort by (number, name, type, state, priority, modified) |
  | sortDirection | string | "asc"   | Sort direction (asc, desc)                           |
  | page          | int    | 1       | Page number (1-based)                                |
  | pageSize      | int    | 20      | Items per page (1-100)                               |

  Calls `IRequirementRepository.GetAllAsync` and returns `PaginatedResultDto<RequirementDto>`.

- **GET /api/requirements/{id}** - Fetch a single requirement by ID. Calls `IRequirementRepository.GetByIdAsync`. Returns `RequirementDto` or 404.

- **GET /api/requirements/facets** - Retrieve facet values for filter dropdowns. Calls `IRequirementRepository.GetFacetsAsync`. Returns `FacetsDto`.

- **GET /api/requirements/{id}/similar** - Find requirements similar to the given one. Accepts optional `top` query parameter (default 10). Calls `ISearchService.FindSimilarAsync`. Returns `List<SearchResultDto>`.

- **DELETE /api/requirements/{id}** - Delete a requirement from both data stores. Calls `IRequirementRepository.DeleteAsync(id)` to remove from SQLite, then `IVectorStore.DeleteAsync(id)` to remove from Qdrant, then `IAuditService.LogDeletionAsync` to record the action. Returns 204 No Content on success, 404 if not found.

#### IRequirementRepository / RequirementRepository

Data access layer for requirement records stored in SQLite.

- **GetAllAsync(string? search, string sortBy, string sortDirection, int page, int pageSize) -> Task\<PaginatedResultDto\<Requirement\>\>**: Constructs a query with optional `WHERE RequirementNumber LIKE @search OR Name LIKE @search OR Description LIKE @search` clause for keyword filtering. Applies dynamic `ORDER BY` based on the `sortBy` and `sortDirection` parameters. Uses `OFFSET/FETCH` for pagination. Returns results along with total count for pagination metadata.

- **GetByIdAsync(Guid id) -> Task\<Requirement?\>**: Single record lookup by primary key. Returns null if not found.

- **GetFacetsAsync() -> Task\<FacetsDto\>**: Executes `GROUP BY` aggregation queries for each facet field (Type, State, Priority, Module, Owner) against the Requirements table. Returns distinct values with their occurrence counts.

- **UpsertAsync(Requirement requirement) -> Task**: Inserts a new requirement or updates an existing one by matching on RequirementNumber. Used by the CSV import pipeline.

- **DeleteAsync(Guid id) -> Task\<bool\>**: Removes the requirement record from the Requirements table. Returns true if a row was deleted, false if the ID was not found.

#### IVectorStore / QdrantVectorStore

- **DeleteAsync(Guid id) -> Task**: Removes the point with the given ID from the Qdrant `requirements` collection. This keeps the vector store in sync with SQLite after a deletion. Logs a warning and continues if Qdrant is unreachable.

#### IAuditService / AuditService

- **LogDeletionAsync(string requirementNumber) -> Task**: Inserts a row into the `AuditLogEntry` table recording the deletion action, the deleted requirement's number, a timestamp, and the authenticated user's identity.

### 2.3 DTOs

#### PaginatedResultDto\<T\>

| Field      | Type    | Description                          |
|------------|---------|--------------------------------------|
| Items      | List\<T\> | Current page of results            |
| TotalCount | int     | Total number of matching records     |
| Page       | int     | Current page number (1-based)        |
| PageSize   | int     | Items per page                       |
| TotalPages | int     | Computed ceiling of TotalCount/PageSize |

#### RequirementDto

All requirement fields: Id, RequirementNumber, Name, Description, Type, State, Priority, Module, Owner, TracedTo (list of related requirement numbers), CreatedAt, UpdatedAt.

#### FacetsDto

| Field    | Type                   | Description                 |
|----------|------------------------|-----------------------------|
| Type     | List\<FacetValueDto\>  | Distinct type values        |
| State    | List\<FacetValueDto\>  | Distinct state values       |
| Priority | List\<FacetValueDto\>  | Distinct priority values    |
| Module   | List\<FacetValueDto\>  | Distinct module values      |
| Owner    | List\<FacetValueDto\>  | Distinct owner values       |

#### FacetValueDto

| Field | Type   | Description                    |
|-------|--------|--------------------------------|
| Value | string | The distinct facet value       |
| Count | int    | Number of requirements with this value |

### 2.4 Data Stores

#### SQLite

- **Requirements table** - Contains full requirement metadata: Id (Guid PK), RequirementNumber (unique index, max 50), Name (max 500), Description (max 5000), Type (max 100), State (max 100), Priority (max 100), Module (max 200), Owner (max 200), TracedTo (JSON array), CreatedAt, UpdatedAt.
- **AuditLogEntry table** - Stores audit records for deletions and other actions.

#### Qdrant

- **Collection:** `requirements`
- **Vector dimensions:** 384
- **Distance metric:** Cosine
- Deletion of a requirement removes its vector point from this collection to keep both stores in sync.

## 3. Class Diagram

![Class Diagram](class-diagram.puml.png)

[PlantUML Source](class-diagram.puml)

## 4. Sequence Diagram

![Sequence Diagram](sequence-diagram.puml.png)

[PlantUML Source](sequence-diagram.puml)

## 5. C4 Diagram

![C4 Diagram](c4-requirements-management.drawio.png)

[Draw.io Source](c4-requirements-management.drawio)
