# TraceQ.Web

Angular 21 frontend for TraceQ. Multi-project monorepo workspace with Angular Material, lazy-loaded features, and a shared component library.

## Getting started

```bash
npm install
npm start
```

Opens on `http://localhost:4200`. Expects the API running at `http://localhost:5000`.

## Workspace structure

The Angular workspace contains four projects under `projects/`:

| Project | Type | Description |
|---------|------|-------------|
| `trace-q` | Application | Main app shell — routing, layout, navigation |
| `features` | Library | Feature pages: Dashboard, Search, Import, Requirements |
| `components` | Library | Reusable UI components (`Tq`-prefixed) |
| `api` | Library | API services, models, and base URL configuration |

## Routes

| Path | Feature | Description |
|------|---------|-------------|
| `/dashboard` | Dashboard | Analytics widgets — distribution charts, traceability, similarity clusters |
| `/search` | Search | Semantic search with faceted filters |
| `/import` | Import | Drag-and-drop CSV upload with import history |
| `/requirements` | Requirements | Paginated table with detail view and similarity lookup |

All routes are lazy-loaded from the `features` library.

## Feature components

### Dashboard
- `DashboardComponent` — widget grid layout
- `DistributionChartComponent` — requirement distribution by field
- `TraceabilityComponent` — trace-link coverage metrics
- `SimilarityClustersComponent` — duplicate/related requirement clusters

### Search
- `SearchComponent` — natural-language query with filter chips and ranked result cards

### Import
- `ImportComponent` — file upload zone, progress indicator, import history table

### Requirements
- `RequirementsComponent` — sortable/paginated Material table
- `RequirementDetailComponent` — full detail view with trace links and similar requirements

## Shared components (`components` library)

Reusable UI building blocks with `Tq` prefix:

- **Buttons**: `TqButtonComponent`, `TqIconButtonComponent`, `TqFabComponent`, `TqButtonGroupComponent`
- **Dialogs**: `TqConfirmDialogComponent`, `TqFormDialogComponent`, `TqDetailDialogComponent`, `TqDestructiveDialogComponent`
- **Feedback**: `TqToastComponent` + `TqToastService`, `TqValidationBannerComponent`, `TqInlineErrorComponent`
- **States**: `TqEmptyStateComponent`, `TqErrorPageComponent`

## API services (`api` library)

| Service | Key methods |
|---------|-------------|
| `SearchService` | `search(request)` |
| `ImportService` | `uploadCsv(file)`, `getHistory(params)`, `getBatch(id)` |
| `RequirementsService` | `list(params)`, `get(id)`, `getSimilar(id)`, `getFacets()`, `delete(id)` |
| `ReportsService` | `getDistribution(field)`, `getTraceability()`, `getSimilarityClusters(threshold)` |
| `DashboardService` | `getLayouts()`, `saveLayout(layout)`, `deleteLayout(id)` |
| `AuditService` | `list(params)` |

API base URL defaults to `http://localhost:5000` via the `API_BASE_URL` injection token.

## Scripts

| Command | Description |
|---------|-------------|
| `npm start` | Start dev server (`ng serve trace-q`) |
| `npm run build` | Production build (`ng build trace-q`) |
| `npm run watch` | Build in watch mode |
| `npm test` | Run unit tests with Vitest |
| `ng e2e` | Run E2E tests with Playwright |

## Key dependencies

- Angular 21 + Angular Material + CDK
- RxJS 7.8
- TypeScript 5.9
- Vitest (unit testing)
- Playwright (E2E testing)
- Fonts: IBM Plex Mono, Space Grotesk, Material Symbols
