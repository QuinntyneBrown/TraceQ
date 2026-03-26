# Semantic Search - Detail Design

## 1. Overview

Semantic search allows systems engineers to find requirements by meaning rather than exact keyword match. The user enters a natural-language query plus optional metadata filters. The system embeds the query text into a 384-dimensional vector using a local ONNX model (all-MiniLM-L6-v2), searches Qdrant for the top-N most similar requirement vectors using cosine similarity, then fetches full requirement details from SQLite to return ranked results.

This approach enables engineers to discover related requirements even when they use different terminology, improving traceability coverage and reducing the risk of missed dependencies.

## 2. Components

### 2.1 Frontend

#### SearchComponent

Angular page routed at `/search`. Provides the primary user interface for semantic search.

**Template elements:**

- **Query input** - A text input field where the user enters a natural-language search query. Debounced to avoid excessive API calls during typing.
- **Filter dropdowns** - Dropdowns for type, state, priority, module, and owner. Values are loaded dynamically from the facets endpoint (`GET /api/requirements/facets`) on component initialization.
- **Top-N selector** - Numeric input controlling how many results to return (default: 20, max: 100).
- **Results table** - Displays ranked results with columns for requirement number, name, similarity score (displayed as a percentage), and a "Find Similar" action button that triggers a secondary search using that requirement's embedding.
- **Loading indicator** - Shown while the search request is in flight.

**Behavior:**

- On submit, constructs a `SearchRequest` object from the query text, selected filters, and top-N value, then calls `SearchService.search()`.
- The "Find Similar" button calls `SearchService.findSimilar(requirementId)` and replaces the current results.
- Results are displayed in descending order of similarity score.

#### SearchService (Angular API Client)

Handles HTTP communication with the backend search endpoints.

- `search(request: SearchRequest): Observable<SearchResult[]>` - Posts the search request to `POST /api/search` and returns the ranked results.
- `findSimilar(requirementId: string, top?: number): Observable<SearchResult[]>` - Posts to `POST /api/search/similar/{id}` to find requirements similar to an existing one.

#### RequirementsService (Angular API Client)

Provides metadata used by the search UI.

- `getFacets(): Observable<Facets>` - Calls `GET /api/requirements/facets` to retrieve distinct values for each filter field (type, state, priority, module, owner) along with their counts. Used to populate filter dropdowns.

### 2.2 Backend

#### SearchController

**Route:** `POST /api/search`

**Request body:** `SearchRequestDto`

| Field   | Type             | Description                                  |
|---------|------------------|----------------------------------------------|
| Query   | string           | Natural-language search query                |
| Top     | int              | Maximum number of results to return (1-100)  |
| Filters | SearchFiltersDto | Optional metadata filters                    |

**Response body:** `List<SearchResultDto>`

| Field             | Type   | Description                          |
|-------------------|--------|--------------------------------------|
| Id                | Guid   | Requirement identifier               |
| RequirementNumber | string | Human-readable requirement number    |
| Name              | string | Requirement title                    |
| Description       | string | Full requirement description         |
| Score             | float  | Cosine similarity score (0.0 - 1.0) |

**Flow:** Validates the request, calls `ISearchService.SemanticSearchAsync`, then calls `IAuditService.LogSearchAsync` to record the query for audit purposes. Returns the ranked result list.

**Additional endpoint:** `POST /api/search/similar/{id}` accepts a requirement ID and optional top-N parameter. Delegates to `ISearchService.FindSimilarAsync`.

#### ISearchService / SearchService

The core orchestration service for semantic search operations.

- **SemanticSearchAsync(SearchRequestDto request) -> List\<SearchResultDto\>**: Generates a query embedding by calling `IEmbeddingService.GenerateEmbeddingAsync(request.Query)`. Builds a Qdrant filter dictionary from the `SearchFiltersDto` fields (only non-null filters are included). Calls `IVectorStore.SearchAsync(vector, request.Top, filters)` to get ranked (id, score) pairs. Fetches full `Requirement` records from `IRequirementRepository.GetByIdsAsync(ids)`, preserving the rank order returned by Qdrant. Maps each requirement plus its score into a `SearchResultDto`.

- **FindSimilarAsync(Guid requirementId, int top) -> List\<SearchResultDto\>**: Looks up the requirement by ID via the repository. Generates an embedding from its concatenated name and description text. Searches Qdrant with that embedding vector, excluding the source requirement's own ID from results. Returns ranked results using the same mapping logic as `SemanticSearchAsync`.

#### IEmbeddingService / OnnxEmbeddingService

Generates 384-dimensional embedding vectors from text using a local ONNX runtime model.

- **GenerateEmbeddingAsync(string text) -> float[384]**: Tokenizes the input text using WordPiece tokenization with a maximum of 256 tokens. Runs ONNX inference using the all-MiniLM-L6-v2 model. Performs mean pooling of the hidden states using the attention mask to handle variable-length inputs. L2-normalizes the resulting vector to a unit vector so that dot product equals cosine similarity.

**Implementation details:**

- Registered as a singleton to amortize model load cost.
- Uses a thread-safe `InferenceSession` from the ONNX Runtime library.
- The ONNX model file is bundled with the application (approximately 80 MB).
- Input text is truncated at 256 tokens to stay within model limits while covering the vast majority of requirement text lengths.

#### IVectorStore / QdrantVectorStore

Abstracts interaction with the Qdrant vector database.

- **SearchAsync(float[384] queryVector, int top, Dictionary\<string, string\> filters) -> List\<(Guid id, float score)\>**: Constructs a Qdrant search request with the provided query vector and limit. If filters are provided, builds a Qdrant `Filter` object with `Match` conditions on the corresponding payload fields (e.g., `type`, `state`, `module`). Executes the search via gRPC and returns the matched point IDs with their similarity scores.

**Graceful degradation:** If Qdrant is unreachable, the service logs a warning and returns an empty result list rather than throwing an exception. This allows the application to remain functional (with degraded search) even if the vector database is temporarily unavailable.

#### IRequirementRepository / RequirementRepository

Data access layer for requirement records stored in SQLite.

- **GetByIdsAsync(IEnumerable\<Guid\> ids) -> List\<Requirement\>**: Batch fetches requirement records using a `SELECT ... WHERE Id IN (...)` query. Results are returned in the order matching the input ID list to preserve ranking.

- **GetFacetsAsync() -> FacetsDto**: Executes `GROUP BY` aggregation queries for each facet field (type, state, priority, module, owner) and returns the distinct values with their counts. Used by the `/api/requirements/facets` endpoint.

#### IAuditService / AuditService

Records search activity for compliance and analytics.

- **LogSearchAsync(string queryText, int resultCount) -> Task**: Inserts a row into the `AuditLogEntry` table with the query text, result count, timestamp, and the authenticated user's identity. This provides a full audit trail of search activity.

### 2.3 Data Stores

#### SQLite

- **Requirements table** - Contains full requirement metadata: Id (Guid PK), RequirementNumber, Name, Description, Type, State, Priority, Module, Owner, CreatedAt, UpdatedAt.
- **AuditLogEntry table** - Stores audit records: Id, Action, QueryText, ResultCount, UserId, Timestamp.
- **Facets** - Derived dynamically via `GROUP BY` queries against the Requirements table. Not stored separately.

#### Qdrant

- **Collection:** `requirements`
- **Vector dimensions:** 384
- **Distance metric:** Cosine
- **Payload fields** (indexed for filtering):
  - `number` (keyword) - Requirement number
  - `name` (text) - Requirement name
  - `type` (keyword) - Requirement type
  - `state` (keyword) - Requirement state
  - `module` (keyword) - Module assignment

Vectors are upserted during the CSV import / embedding pipeline and are queried during semantic search.

## 3. Class Diagram

![Class Diagram](class-diagram.puml.png)

[PlantUML Source](class-diagram.puml)

## 4. Sequence Diagram

![Sequence Diagram](sequence-diagram.puml.png)

[PlantUML Source](sequence-diagram.puml)

## 5. C4 Diagram

![C4 Diagram](c4-semantic-search.drawio.png)

[Draw.io Source](c4-semantic-search.drawio)
