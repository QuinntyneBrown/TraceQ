# Similarity Clustering - Detail Design

## 1. Overview

Similarity clustering identifies groups of semantically related requirements using a Union-Find algorithm. For each embedded requirement, the system generates its embedding, searches Qdrant for the top-20 most similar vectors, and unions any pair whose cosine similarity score meets or exceeds a configurable threshold (default 0.85). The resulting clusters (groups of 2+ requirements) are returned with pairwise similarity scores, limited to 50 clusters.

## 2. Components

### 2.1 Frontend

#### SimilarityClustersComponent

Dashboard widget that displays similarity clusters. Provides a configurable threshold slider (default 0.85) that controls the minimum cosine similarity required to group requirements together.

**Template elements:**

- **Threshold slider** - A range input (0.50 to 1.00, step 0.01) with a default value of 0.85. Changing the value triggers a new clustering request.
- **Cluster list** - Displays up to 50 clusters, each showing its member requirement numbers and names.
- **Pairwise scores** - Within each cluster, shows the similarity scores between member pairs.
- **Loading indicator** - Shown while the clustering request is in flight.

**Behavior:**

- On initialization and threshold change, calls `ReportsService.getSimilarityClusters(threshold)`.
- Clusters are displayed in descending order of cluster size (number of members).
- Each cluster is expandable to show the pairwise similarity scores between its members.

#### ReportsService (Angular API Client)

Handles HTTP communication with the backend reports endpoints.

- `getSimilarityClusters(threshold?: number): Observable<SimilarityCluster[]>` - Calls `GET /api/reports/similarity-clusters?threshold={threshold}` and returns the cluster list.

### 2.2 Backend

#### ReportsController

**Route:** `GET /api/reports/similarity-clusters`

**Query parameters:**

| Parameter | Type  | Default | Description                                    |
|-----------|-------|---------|------------------------------------------------|
| threshold | float | 0.85    | Minimum cosine similarity to union two requirements |

**Response body:** `List<SimilarityClusterDto>`

| Field          | Type                     | Description                              |
|----------------|--------------------------|------------------------------------------|
| ClusterId      | int                      | Sequential cluster identifier            |
| Members        | List\<ClusterMemberDto\> | Requirements in this cluster             |
| PairwiseScores | List\<PairwiseScoreDto\> | Similarity scores between member pairs   |

**Flow:** Validates the threshold parameter (must be between 0.0 and 1.0), then delegates to `ISearchService.GetSimilarityClustersAsync(threshold)`. Returns the cluster list.

#### ISearchService / SearchService

The core orchestration service for similarity clustering.

- **GetSimilarityClustersAsync(float threshold) -> List\<SimilarityClusterDto\>**:
  1. Fetches all embedded requirements from `IRequirementRepository`.
  2. Initializes Union-Find data structure (`parent[]` and `rank[]`) with one entry per requirement.
  3. For each requirement: generates an embedding via `IEmbeddingService.GenerateEmbeddingAsync`, then calls `IVectorStore.SearchAsync(vector, top=20)` to find the 20 nearest neighbors.
  4. For each search result with score >= threshold: calls `Union(req, result)` to merge their sets, and tracks the pairwise score.
  5. Groups requirements by their Union-Find root (using `Find` with path compression).
  6. Filters to clusters containing 2 or more members.
  7. Returns the first 50 clusters as `SimilarityClusterDto[]`, ordered by descending cluster size.

#### Union-Find Algorithm

Implemented as an inner data structure within SearchService for the duration of the clustering operation.

- **Find(x)** - Returns the root representative of the set containing x. Uses path compression: during traversal, each node is updated to point directly to the root, yielding O(alpha(n)) amortized time.
- **Union(x, y)** - Merges the sets containing x and y. Uses union by rank: the tree with smaller rank is attached under the tree with larger rank. If ranks are equal, one is chosen as root and its rank is incremented.
- **Data structures:**
  - `parent[]` - Maps each requirement index to its parent index. Initially `parent[i] = i`.
  - `rank[]` - Tracks tree depth upper bound for union by rank. Initially all zeros.

#### IEmbeddingService / OnnxEmbeddingService

Generates 384-dimensional embedding vectors from requirement text. Called once per requirement during the clustering loop. See the [Vector Embedding detail design](../04-vector-embedding/vector-embedding.md) for full implementation details.

#### IVectorStore / QdrantVectorStore

Searches for the top-20 most similar vectors per requirement embedding. Returns `List<(Guid id, float score)>` pairs. The top-20 limit bounds the number of comparisons while capturing the most relevant neighbors for clustering.

#### IRequirementRepository / RequirementRepository

Provides the list of all embedded requirements to seed the clustering process.

- **GetAllEmbeddedAsync() -> List\<Requirement\>**: Fetches all requirements that have been successfully embedded (i.e., have a corresponding vector in Qdrant).

### 2.3 DTOs

#### SimilarityClusterDto

| Field          | Type                     | Description                            |
|----------------|--------------------------|----------------------------------------|
| ClusterId      | int                      | Sequential cluster identifier (1-based)|
| Members        | List\<ClusterMemberDto\> | Requirements belonging to this cluster |
| PairwiseScores | List\<PairwiseScoreDto\> | Similarity scores between member pairs |

#### ClusterMemberDto

| Field             | Type   | Description                       |
|-------------------|--------|-----------------------------------|
| Id                | Guid   | Requirement identifier            |
| RequirementNumber | string | Human-readable requirement number |
| Name              | string | Requirement title                 |

#### PairwiseScoreDto

| Field    | Type  | Description                          |
|----------|-------|--------------------------------------|
| SourceId | Guid  | First requirement in the pair        |
| TargetId | Guid  | Second requirement in the pair       |
| Score    | float | Cosine similarity score (0.0 - 1.0) |

## 3. Class Diagram

![Class Diagram](class-diagram.puml.png)

[PlantUML Source](class-diagram.puml)

## 4. Sequence Diagram

![Sequence Diagram](sequence-diagram.puml.png)

[PlantUML Source](sequence-diagram.puml)

## 5. C4 Diagram

![C4 Diagram](c4-similarity-clustering.drawio.png)

[Draw.io Source](c4-similarity-clustering.drawio)
