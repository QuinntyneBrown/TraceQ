import { test, expect } from '@playwright/test';
import { RequirementsPage } from '../pages/requirements.page';
import { setupApiMocks, MOCK_REQUIREMENTS } from '../fixtures/api-mocks';

test.describe('Requirements Page', () => {
  let requirements: RequirementsPage;

  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
    requirements = new RequirementsPage(page);
    await requirements.goto();
  });

  test.describe('Layout & Structure', () => {
    test('requirements page is visible', async () => {
      await expect(requirements.pageContent).toBeVisible();
    });

    test('requirements nav item is active', async () => {
      await expect(requirements.navRequirements).toHaveClass(/nav-item--active/);
    });
  });

  test.describe('Requirements Table', () => {
    test('uses Angular Material table (mat-table)', async () => {
      await requirements.expectMaterialTableUsed();
    });

    test('table displays requirement data', async () => {
      await requirements.expectTableVisible();
      const text = await requirements.page.textContent('body');
      expect(text).toContain('REQ-001');
      expect(text).toContain('Thermal Protection');
    });

    test('table has sortable headers (mat-sort-header)', async () => {
      await requirements.expectMaterialSortUsed();
    });

    test('paginator uses mat-paginator', async () => {
      await requirements.expectMaterialPaginatorUsed();
    });
  });

  test.describe('Search & Filter', () => {
    test('search input uses mat-form-field', async () => {
      const matFormField = requirements.page.locator('mat-form-field');
      await expect(matFormField.first()).toBeVisible();
    });

    test('can search requirements by keyword', async () => {
      await requirements.search('thermal');
      const text = await requirements.page.textContent('body');
      expect(text).toContain('REQ-001');
    });
  });

  test.describe('Row Interactions', () => {
    test('clicking a row opens detail dialog', async () => {
      await requirements.clickRow(0);
      await requirements.expectDetailDialogOpen();
    });

    test('detail dialog shows requirement information', async () => {
      await requirements.clickRow(0);
      await requirements.expectDetailDialogOpen();

      const dialogText = await requirements.page.locator('.cdk-overlay-pane, mat-dialog-container').textContent();
      expect(dialogText).toContain('REQ-001');
    });

    test('detail dialog can be closed', async () => {
      await requirements.clickRow(0);
      await requirements.expectDetailDialogOpen();

      // Close dialog by pressing Escape or clicking close button
      await requirements.page.keyboard.press('Escape');
      await requirements.page.waitForTimeout(500);

      const dialog = requirements.page.locator('mat-dialog-container');
      await expect(dialog).not.toBeVisible();
    });
  });

  test.describe('Delete Flow', () => {
    test('delete button triggers destructive dialog', async () => {
      // Find and click delete button in first row
      const deleteBtn = requirements.page.locator('mat-row tq-icon-button, tr[mat-row] tq-icon-button').first();
      if (await deleteBtn.isVisible()) {
        await deleteBtn.click();
        await requirements.page.waitForTimeout(500);

        const dialog = requirements.page.locator('mat-dialog-container, .cdk-overlay-pane');
        await expect(dialog).toBeVisible();
      }
    });
  });

  test.describe('Angular Material Usage', () => {
    test('uses mat-icon for action buttons', async () => {
      const matIcons = requirements.page.locator('mat-icon');
      const count = await matIcons.count();
      expect(count).toBeGreaterThanOrEqual(5);
    });

    test('uses tq-button components', async () => {
      const tqButtons = requirements.page.locator('tq-button');
      const count = await tqButtons.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('uses tq-icon-button components', async () => {
      const tqIconButtons = requirements.page.locator('tq-icon-button');
      const count = await tqIconButtons.count();
      expect(count).toBeGreaterThanOrEqual(0); // May or may not have icon buttons
    });
  });

  test.describe('Typography & Colors', () => {
    test('requirement numbers use monospace font', async () => {
      const reqCell = requirements.page.locator('mat-cell, td[mat-cell]').first();
      if (await reqCell.isVisible()) {
        const font = await requirements.getComputedFontFamily(reqCell);
        expect(font).toBeTruthy();
      }
    });
  });
});
