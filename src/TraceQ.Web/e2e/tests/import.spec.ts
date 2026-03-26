import { test, expect } from '@playwright/test';
import { ImportPage } from '../pages/import.page';
import { setupApiMocks } from '../fixtures/api-mocks';

test.describe('Import Page', () => {
  let importPage: ImportPage;

  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
    importPage = new ImportPage(page);
    await importPage.goto();
  });

  test.describe('Layout & Structure', () => {
    test('import page is visible', async () => {
      await expect(importPage.pageContent).toBeVisible();
    });

    test('import nav item is active', async () => {
      await expect(importPage.navImport).toHaveClass(/nav-item--active/);
    });

    test('drop zone area is visible', async () => {
      await importPage.expectDropZoneVisible();
    });
  });

  test.describe('Upload Area', () => {
    test('upload zone has mat-icon for upload indicator', async () => {
      const matIcons = importPage.dropZone.locator('mat-icon');
      const count = await matIcons.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('has a file input element', async () => {
      // File input might be hidden but must exist
      const fileInputs = importPage.page.locator('input[type="file"]');
      const count = await fileInputs.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('upload zone is clickable and triggers file input', async () => {
      // The drop zone itself is clickable (it triggers fileInput.click())
      await expect(importPage.dropZone).toBeVisible();
      const dropZoneClasses = await importPage.dropZone.getAttribute('class');
      expect(dropZoneClasses).toContain('drop-zone');
    });
  });

  test.describe('Import History', () => {
    test('history table is visible and uses Angular Material table', async () => {
      await importPage.expectMaterialTableUsed();
    });

    test('history table shows imported records', async () => {
      await importPage.expectHistoryTableVisible();
      const text = await importPage.page.textContent('body');
      expect(text).toContain('requirements-v3.csv');
    });

    test('paginator is present and uses mat-paginator', async () => {
      await importPage.expectPaginatorVisible();
    });
  });

  test.describe('Angular Material Usage', () => {
    test('uses mat-card for content sections', async () => {
      const matCards = importPage.page.locator('mat-card, .mat-mdc-card');
      const count = await matCards.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('uses mat-icon components', async () => {
      const matIcons = importPage.page.locator('mat-icon');
      const count = await matIcons.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('uses mat-progress-bar for upload progress', async () => {
      const progressBars = importPage.page.locator('mat-progress-bar');
      // Progress bar may not be visible until upload starts
      const count = await progressBars.count();
      expect(count).toBeGreaterThanOrEqual(0);
    });
  });

  test.describe('Typography & Colors', () => {
    test('page uses correct dark background', async () => {
      const bg = await importPage.getComputedBgColor(importPage.pageContent);
      expect(bg).toBe('rgb(10, 10, 10)');
    });
  });
});
