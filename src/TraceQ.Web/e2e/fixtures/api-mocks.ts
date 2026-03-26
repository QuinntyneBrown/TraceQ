import { Page } from '@playwright/test';

export const MOCK_REQUIREMENTS = [
  {
    id: '1',
    requirementNumber: 'REQ-001',
    name: 'Thermal Protection System Interface Control',
    description: 'The thermal protection system shall provide interface control documentation.',
    type: 'Functional',
    state: 'Approved',
    priority: 'High',
    owner: 'John Doe',
    module: 'TPS-CORE',
    parent: null,
    tracedTo: ['REQ-010', 'REQ-015'],
    isEmbedded: true,
    createdAt: '2025-01-15T10:00:00Z',
    modifiedAt: '2025-03-01T14:30:00Z',
  },
  {
    id: '2',
    requirementNumber: 'REQ-002',
    name: 'Data Format Specification',
    description: 'All data formats shall comply with the XML schema definition.',
    type: 'Non-Functional',
    state: 'Draft',
    priority: 'Medium',
    owner: 'Jane Smith',
    module: 'DATA-FMT',
    parent: 'REQ-001',
    tracedTo: [],
    isEmbedded: false,
    createdAt: '2025-02-01T09:00:00Z',
    modifiedAt: '2025-02-28T11:00:00Z',
  },
  {
    id: '3',
    requirementNumber: 'REQ-003',
    name: 'Performance Threshold Monitor',
    description: 'The system shall monitor performance thresholds continuously.',
    type: 'Functional',
    state: 'Approved',
    priority: 'Critical',
    owner: 'John Doe',
    module: 'TPS-CORE',
    parent: null,
    tracedTo: ['REQ-001'],
    isEmbedded: true,
    createdAt: '2025-01-20T08:00:00Z',
    modifiedAt: '2025-03-10T16:00:00Z',
  },
];

export const MOCK_FACETS = {
  types: [
    { value: 'Functional', count: 820 },
    { value: 'Non-Functional', count: 310 },
    { value: 'Interface', count: 117 },
  ],
  states: [
    { value: 'Approved', count: 640 },
    { value: 'Draft', count: 380 },
    { value: 'Under Review', count: 227 },
  ],
  priorities: [
    { value: 'Critical', count: 120 },
    { value: 'High', count: 430 },
    { value: 'Medium', count: 520 },
    { value: 'Low', count: 177 },
  ],
  modules: [
    { value: 'TPS-CORE', count: 340 },
    { value: 'DATA-FMT', count: 220 },
    { value: 'COMM-SYS', count: 180 },
  ],
  owners: [
    { value: 'John Doe', count: 560 },
    { value: 'Jane Smith', count: 420 },
    { value: 'Bob Wilson', count: 267 },
  ],
};

export const MOCK_DISTRIBUTION = [
  { label: 'Functional', count: 820 },
  { label: 'Non-Functional', count: 310 },
  { label: 'Interface', count: 117 },
];

export const MOCK_TRACEABILITY = {
  totalCount: 1247,
  tracedCount: 978,
  untracedCount: 269,
  coveragePercent: 78.4,
  traceLinkDistribution: [
    { linkCount: 0, requirementCount: 269 },
    { linkCount: 1, requirementCount: 420 },
    { linkCount: 2, requirementCount: 340 },
    { linkCount: 3, requirementCount: 218 },
  ],
  untracedRequirements: MOCK_REQUIREMENTS.slice(0, 2),
};

export const MOCK_CLUSTERS = [
  {
    clusterId: 'c1',
    similarity: 0.92,
    members: [
      { requirementId: '1', requirementNumber: 'REQ-001', name: 'Thermal Protection System Interface Control' },
      { requirementId: '3', requirementNumber: 'REQ-003', name: 'Performance Threshold Monitor' },
    ],
  },
];

export const MOCK_IMPORT_HISTORY = [
  {
    batchId: 'b1',
    fileName: 'requirements-v3.csv',
    importedAt: '2025-03-20T14:30:00Z',
    insertedCount: 142,
    updatedCount: 15,
    errorCount: 3,
    skippedCount: 0,
  },
  {
    batchId: 'b2',
    fileName: 'tps-module.csv',
    importedAt: '2025-03-18T09:15:00Z',
    insertedCount: 67,
    updatedCount: 8,
    errorCount: 0,
    skippedCount: 2,
  },
];

export const MOCK_SEARCH_RESULTS = [
  {
    requirement: MOCK_REQUIREMENTS[0],
    similarityScore: 0.95,
  },
  {
    requirement: MOCK_REQUIREMENTS[2],
    similarityScore: 0.87,
  },
];

export async function setupApiMocks(page: Page): Promise<void> {
  // Requirements list
  await page.route('**/api/requirements?*', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        items: MOCK_REQUIREMENTS,
        totalCount: MOCK_REQUIREMENTS.length,
        page: 1,
        pageSize: 20,
      }),
    });
  });

  // Requirements by ID
  await page.route('**/api/requirements/1', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_REQUIREMENTS[0]),
    });
  });

  // Similar requirements
  await page.route('**/api/requirements/*/similar*', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_SEARCH_RESULTS),
    });
  });

  // Delete requirement
  await page.route('**/api/requirements/*', (route) => {
    if (route.request().method() === 'DELETE') {
      route.fulfill({ status: 204 });
    } else {
      route.fallback();
    }
  });

  // Facets
  await page.route('**/api/requirements/facets', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_FACETS),
    });
  });

  // Search
  await page.route('**/api/search', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_SEARCH_RESULTS),
    });
  });

  // Distribution
  await page.route('**/api/reports/distribution/*', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_DISTRIBUTION),
    });
  });

  // Traceability
  await page.route('**/api/reports/traceability', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_TRACEABILITY),
    });
  });

  // Similarity clusters
  await page.route('**/api/reports/similarity-clusters*', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(MOCK_CLUSTERS),
    });
  });

  // Import CSV
  await page.route('**/api/import/csv', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        insertedCount: 142,
        updatedCount: 15,
        errorCount: 3,
        skippedCount: 0,
      }),
    });
  });

  // Import history
  await page.route('**/api/import/history*', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        items: MOCK_IMPORT_HISTORY,
        totalCount: MOCK_IMPORT_HISTORY.length,
        page: 1,
        pageSize: 10,
      }),
    });
  });

  // Dashboard layouts
  await page.route('**/api/dashboard/layouts', (route) => {
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    });
  });
}
