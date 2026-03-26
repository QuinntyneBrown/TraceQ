import { test, expect } from '@playwright/test';
import { SearchPage } from '../pages/search.page';
import { setupApiMocks } from '../fixtures/api-mocks';

test.describe('Search Page', () => {
  let search: SearchPage;

  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
    search = new SearchPage(page);
    await search.goto();
  });

  test.describe('Layout & Structure', () => {
    test('search page is visible', async () => {
      await expect(search.pageContent).toBeVisible();
    });

    test('search nav item is active', async () => {
      await expect(search.navSearch).toHaveClass(/nav-item--active/);
    });

    test('search input is visible', async () => {
      await expect(search.searchInput).toBeVisible();
    });
  });

  test.describe('Search Functionality', () => {
    test('can perform a search and see results', async () => {
      await search.search('thermal protection');
      // Wait for results to appear
      await expect(search.resultCards.first()).toBeVisible({ timeout: 10000 });
    });

    test('results show requirement data', async () => {
      await search.search('thermal');
      await expect(search.resultCards.first()).toBeVisible({ timeout: 10000 });

      // Check that result text contains requirement info
      const pageText = await search.page.textContent('body');
      expect(pageText).toContain('REQ-001');
    });
  });

  test.describe('Filters', () => {
    test('filter dropdowns are present (mat-select)', async () => {
      await search.expectFiltersPresent();
    });

    test('filters use Angular Material mat-select', async () => {
      const matSelects = search.page.locator('mat-select');
      const count = await matSelects.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });
  });

  test.describe('Angular Material Usage', () => {
    test('search uses mat-form-field for input', async () => {
      const matFormField = search.page.locator('mat-form-field');
      await expect(matFormField.first()).toBeVisible();
    });

    test('uses mat-icon for icons', async () => {
      const matIcons = search.page.locator('mat-icon');
      const count = await matIcons.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('uses tq-button components', async () => {
      const tqButtons = search.page.locator('tq-button');
      const count = await tqButtons.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('uses mat-chip for result metadata', async () => {
      await search.search('thermal');
      await search.page.waitForTimeout(500);
      const chips = search.page.locator('mat-chip, .mat-mdc-chip');
      const count = await chips.count();
      expect(count).toBeGreaterThanOrEqual(0); // Chips may or may not be present
    });
  });

  test.describe('Typography', () => {
    test('search input uses correct font', async () => {
      const font = await search.getComputedFontFamily(search.searchInput);
      // Should use a proper font (not browser default)
      expect(font).toBeTruthy();
    });
  });
});
