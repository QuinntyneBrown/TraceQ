# Traceability Analysis - Detail Design

## 1. Overview

Traceability analysis evaluates how well requirements are linked to each other through their TracedTo field. The system counts total requirements vs. those with non-empty TracedTo values, computes coverage percentage, identifies untraced requirements, and analyzes the distribution of trace links. This helps systems engineers identify gaps in requirements traceability for compliance and V&V activities.

## 2. Components

### 2.1 Frontend

#### TraceabilityComponent

Angular dashboard widget that displays traceability coverage metrics. Provides a high-level view of how well requirements are linked.

**Template elements:**

- **Coverage percentage** - Displays the ratio of traced to total requirements as a percentage (TracedCount / TotalCount * 100). Rendered as a prominent metric card.
- **Untraced requirements list** - Lists requirements where TracedTo is null or empty, showing each requirement's number and name. Allows engineers to quickly identify gaps.
- **Trace link distribution** - Shows how many requirements have 1 link, 2 links, 3+ links, etc. Rendered as a bar chart or summary table to visualize the spread of trace depth.

**Behavior:**

- On initialization, calls `ReportsService.getTraceability()` to fetch the coverage data.
- Displays a loading indicator while the request is in flight.
- If no untraced requirements exist, shows a success message indicating full coverage.

#### ReportsService (Angular API Client)

Handles HTTP communication with the backend reports endpoint.

- `getTraceability(): Observable<TraceabilityCoverage>` - Calls `GET /api/reports/traceability` and returns the full traceability coverage response including counts, percentage, untraced list, and distribution.

### 2.2 Backend

#### ReportsController

**Route:** `GET /api/reports/traceability`

**Response body:** `TraceabilityCoverageDto`

| Field                | Type                          | Description                                              |
|----------------------|-------------------------------|----------------------------------------------------------|
| TotalCount           | int                           | Total number of requirements                             |
| TracedCount          | int                           | Number of requirements with non-empty TracedTo           |
| UntracedCount        | int                           | Number of requirements where TracedTo is null or empty   |
| CoveragePercentage   | double                        | (TracedCount / TotalCount) * 100                         |
| UntracedRequirements | List\<RequirementSummaryDto\> | Requirements missing trace links                         |
| TraceDistribution    | List\<DistributionDto\>       | Count of requirements grouped by number of trace links   |

**Flow:** Calls `IRequirementRepository.GetTraceabilityCoverageAsync()` and returns the resulting DTO directly as a 200 OK JSON response.

#### IRequirementRepository / RequirementRepository

Data access layer method for computing traceability coverage from the SQLite database.

- **GetTraceabilityCoverageAsync() -> Task\<TraceabilityCoverageDto\>**: Executes the following steps:
  1. Counts total requirements via `SELECT COUNT(*) FROM Requirements`.
  2. Counts traced requirements via `SELECT COUNT(*) FROM Requirements WHERE TracedTo IS NOT NULL AND TracedTo != ''`.
  3. Computes `CoveragePercentage = (TracedCount / TotalCount) * 100`. Handles the zero-total edge case by returning 0%.
  4. Fetches untraced requirements via `SELECT Id, RequirementNumber, Name FROM Requirements WHERE TracedTo IS NULL OR TracedTo = ''`.
  5. Computes trace link distribution by iterating over requirements with non-empty TracedTo, counting the number of comma-separated items in each TracedTo value, and grouping into buckets (e.g., "1 link", "2 links", "3+ links").
  6. Assembles and returns a `TraceabilityCoverageDto`.

### 2.3 DTOs & Models

#### TraceabilityCoverageDto

| Field                | Type                          | Description                                            |
|----------------------|-------------------------------|--------------------------------------------------------|
| TotalCount           | int                           | Total number of requirements                           |
| TracedCount          | int                           | Requirements with non-empty TracedTo                   |
| UntracedCount        | int                           | Requirements where TracedTo is null or empty            |
| CoveragePercentage   | double                        | Traced / Total * 100                                   |
| UntracedRequirements | List\<RequirementSummaryDto\> | List of untraced requirements                          |
| TraceDistribution    | List\<DistributionDto\>       | Distribution of trace link counts                      |

#### RequirementSummaryDto

| Field             | Type   | Description                       |
|-------------------|--------|-----------------------------------|
| Id                | Guid   | Requirement identifier            |
| RequirementNumber | string | Human-readable requirement number |
| Name              | string | Requirement title                 |

#### DistributionDto

| Field | Type   | Description                              |
|-------|--------|------------------------------------------|
| Label | string | Bucket label (e.g., "1 link", "2 links") |
| Count | int    | Number of requirements in this bucket    |

### 2.4 Data Store

#### SQLite

- **Requirements table** - Contains full requirement metadata including the `TracedTo` field, which stores comma-separated requirement IDs representing trace links to other requirements.
- **Coverage metrics** - Derived dynamically from SQL `COUNT` and `WHERE` queries against the Requirements table. Not stored separately.
- **Trace distribution** - Computed at query time by parsing the comma-separated TracedTo values and grouping by link count.

## 3. Class Diagram

![Class Diagram](class-diagram.puml.png)

[PlantUML Source](class-diagram.puml)

## 4. Sequence Diagram

![Sequence Diagram](sequence-diagram.puml.png)

[PlantUML Source](sequence-diagram.puml)

## 5. C4 Diagram

![C4 Diagram](c4-traceability-analysis.drawio.png)

[Draw.io Source](c4-traceability-analysis.drawio)
