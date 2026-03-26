# Dashboard & Reporting - Detail Design

## 1. Overview

The Dashboard provides an analytics overview composed of three independent widgets: distribution charts (bar charts for type/state/priority/module/owner), traceability coverage (percentage and untraced requirements), and similarity clusters. Dashboard layouts (widget positions/configurations) can be saved and loaded via a JSON blob stored in SQLite. Each widget loads its data independently from the Reports API.

## 2. Components

### 2.1 Frontend

#### DashboardComponent

Angular page routed at `/dashboard`. Serves as the composition root for the three analytics widgets and manages layout persistence.

**Template elements:**

- **Widget grid** - A flexible grid layout containing the three widget components. Widget positions and sizes are configurable and persisted.
- **Layout selector** - Dropdown to load a previously saved layout by name.
- **Save layout button** - Saves the current widget positions/sizes/configurations as a named layout.
- **Delete layout button** - Removes a saved layout.

**Behavior:**

- On initialization, loads the default or last-used layout configuration and initializes all three widget components in parallel.
- Each widget component independently fetches its own data from the Reports API.
- Layout changes (drag/resize) are tracked locally and can be persisted via the save action.

#### DistributionChartComponent (Widget)

Displays requirement distributions as Chart.js bar charts with a configurable field selector.

**Template elements:**

- **Field selector** - Dropdown to choose the distribution field: type, state, priority, module, or owner.
- **Bar chart** - Chart.js bar chart with category labels on the X-axis and counts on the Y-axis.
- **Loading indicator** - Shown while distribution data is loading.

**Behavior:**

- On initialization, loads distribution data for the default field (type) via `ReportsService.getDistribution('type')`.
- When the user changes the field selector, fetches new distribution data and re-renders the chart.
- Chart colors are assigned per category for visual distinction.

#### TraceabilityComponent (Widget)

Displays traceability coverage metrics and lists untraced requirements.

**Template elements:**

- **Coverage percentage** - Large display showing the percentage of requirements that have at least one trace link.
- **Untraced requirements list** - Table listing requirements with no trace links, showing requirement number and name.
- **Trace link distribution** - Summary of trace link counts (e.g., how many requirements have 1, 2, 3+ links).

**Behavior:**

- On initialization, fetches traceability data via `ReportsService.getTraceability()`.
- Coverage percentage is calculated as (traced requirements / total requirements) * 100.

#### SimilarityClustersComponent (Widget)

Displays groups of semantically similar requirements identified by vector similarity.

**Template elements:**

- **Threshold slider** - Configurable similarity threshold (default: 0.85, range: 0.50-1.00).
- **Cluster list** - Expandable list of cluster groups, each showing the member requirements and pairwise similarity scores.
- **Loading indicator** - Shown while cluster data is loading.

**Behavior:**

- On initialization, fetches clusters via `ReportsService.getSimilarityClusters(0.85)`.
- When the user adjusts the threshold, re-fetches clusters with the new threshold value.
- Maximum of 50 clusters are displayed to keep the UI responsive.

#### DashboardService (Angular API Client)

Handles HTTP communication with the backend dashboard layout endpoints.

- `getLayouts(): Observable<DashboardLayout[]>` - Calls `GET /api/dashboard/layouts` to retrieve all saved layouts.
- `saveLayout(layout: {name: string, layoutJson: string}): Observable<DashboardLayout>` - Posts to `POST /api/dashboard/layouts` to save a layout configuration.
- `deleteLayout(id: string): Observable<void>` - Calls `DELETE /api/dashboard/layouts/{id}` to remove a saved layout.

#### ReportsService (Angular API Client)

Handles HTTP communication with the backend reports endpoints.

- `getDistribution(field: string): Observable<DistributionItem[]>` - Calls `GET /api/reports/distribution/{field}` to retrieve distribution data for the specified field.
- `getTraceability(): Observable<TraceabilityReport>` - Calls `GET /api/reports/traceability` to retrieve traceability coverage data.
- `getSimilarityClusters(threshold?: number): Observable<SimilarityCluster[]>` - Calls `GET /api/reports/similarity-clusters?threshold={threshold}` to retrieve similarity cluster data.

### 2.2 Backend

#### DashboardController

**Route:** `GET /api/dashboard/layouts`

Lists all saved dashboard layouts. Calls `IDashboardLayoutRepository.GetAllAsync()`. Returns `List<DashboardLayoutDto>`.

**Route:** `POST /api/dashboard/layouts`

Saves a new or updated dashboard layout.

| Field      | Type   | Description                                      |
|------------|--------|--------------------------------------------------|
| Name       | string | User-defined name for the layout                 |
| LayoutJson | string | Serialized JSON of widget positions/sizes/config |

Calls `IDashboardLayoutRepository.SaveAsync(layout)`. Returns the saved `DashboardLayoutDto`.

**Route:** `DELETE /api/dashboard/layouts/{id}`

Deletes a saved layout by ID. Calls `IDashboardLayoutRepository.DeleteAsync(id)`. Returns 204 No Content.

#### ReportsController

**Route:** `GET /api/reports/distribution/{field}`

Field parameter accepts: type, state, priority, module, or owner. Calls `IRequirementRepository.GetDistributionAsync(field)` which performs a `GROUP BY` on the specified field. Returns `List<DistributionDto>`.

| Field | Type   | Description                     |
|-------|--------|---------------------------------|
| Label | string | Category label (field value)    |
| Count | int    | Number of matching requirements |

**Route:** `GET /api/reports/traceability`

Calls `IRequirementRepository.GetTraceabilityCoverageAsync()`. Returns a `TraceabilityReportDto` containing coverage percentage, list of untraced requirements, and trace link distribution.

**Route:** `GET /api/reports/similarity-clusters`

Accepts optional query parameter `threshold` (default: 0.85). Calls `ISearchService.GetSimilarityClustersAsync(threshold)` which queries Qdrant for pairwise similarities above the threshold and groups them into clusters. Returns `List<SimilarityClusterDto>`.

#### IDashboardLayoutRepository / DashboardLayoutRepository

Data access layer for dashboard layout records stored in SQLite.

- **GetAllAsync() -> List\<DashboardLayout\>**: Fetches all saved layouts ordered by UpdatedAt descending.
- **GetByIdAsync(Guid id) -> DashboardLayout?**: Fetches a single layout by ID.
- **SaveAsync(DashboardLayout layout) -> DashboardLayout**: Inserts a new layout or updates an existing one. Sets CreatedAt on insert, UpdatedAt on both insert and update.
- **DeleteAsync(Guid id) -> Task**: Removes a layout by ID.

#### IRequirementRepository (Extended)

- **GetDistributionAsync(string field) -> List\<DistributionDto\>**: Executes a `GROUP BY` on the specified field (type, state, priority, module, or owner) and returns (label, count) pairs sorted by count descending.

- **GetTraceabilityCoverageAsync() -> TraceabilityReportDto**: Calculates trace coverage by joining Requirements with TraceLinks. Returns the coverage percentage, untraced requirement list, and trace link count distribution.

#### ISearchService (Extended)

- **GetSimilarityClustersAsync(double threshold) -> List\<SimilarityClusterDto\>**: Queries Qdrant for all pairwise similarities above the given threshold. Groups requirements into clusters using a union-find approach. Returns up to 50 clusters with their member requirements and pairwise scores.

### 2.3 Domain Models

#### DashboardLayout

| Field      | Type     | Description                                      |
|------------|----------|--------------------------------------------------|
| Id         | Guid     | Primary key                                      |
| Name       | string   | User-defined layout name                         |
| LayoutJson | string   | Serialized JSON of widget positions/sizes/config |
| CreatedAt  | DateTime | Timestamp of creation                            |
| UpdatedAt  | DateTime | Timestamp of last update                         |

### 2.4 DTOs

#### DashboardLayoutDto

| Field      | Type     | Description                                      |
|------------|----------|--------------------------------------------------|
| Id         | Guid     | Layout identifier                                |
| Name       | string   | Layout name                                      |
| LayoutJson | string   | Serialized widget configuration JSON             |
| CreatedAt  | DateTime | Creation timestamp                               |
| UpdatedAt  | DateTime | Last update timestamp                            |

#### DistributionDto

| Field | Type   | Description                     |
|-------|--------|---------------------------------|
| Label | string | Category label (field value)    |
| Count | int    | Number of matching requirements |

### 2.5 Data Stores

#### SQLite

- **DashboardLayouts table** - Stores saved layout configurations: Id (Guid PK), Name, LayoutJson, CreatedAt, UpdatedAt.
- **Requirements table** - Queried for distribution aggregations via GROUP BY.
- **TraceLinks table** - Queried for traceability coverage calculations.

#### Qdrant

- **Collection:** `requirements`
- Used by `GetSimilarityClustersAsync` to find pairwise similarities above the configured threshold for cluster grouping.

## 3. Class Diagram

![Class Diagram](class-diagram.puml.png)

[PlantUML Source](class-diagram.puml)

## 4. Sequence Diagram

![Sequence Diagram](sequence-diagram.puml.png)

[PlantUML Source](sequence-diagram.puml)

## 5. C4 Diagram

![C4 Diagram](c4-dashboard-reporting.drawio.png)

[Draw.io Source](c4-dashboard-reporting.drawio)
