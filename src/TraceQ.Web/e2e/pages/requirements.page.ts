import { type Locator, type Page, expect } from '@playwright/test';
import { LayoutPage } from './layout.page';

export class RequirementsPage extends LayoutPage {
  readonly searchInput: Locator;
  readonly requirementsTable: Locator;
  readonly tableRows: Locator;
  readonly paginator: Locator;
  readonly sortHeaders: Locator;
  readonly deleteButtons: Locator;
  readonly emptyState: Locator;

  constructor(page: Page) {
    super(page);
    this.searchInput = page.locator('input[matInput]').first();
    this.requirementsTable = page.locator('mat-table, table[mat-table]');
    this.tableRows = page.locator('mat-row, tr[mat-row]');
    this.paginator = page.locator('mat-paginator');
    this.sortHeaders = page.locator('mat-header-cell[mat-sort-header], th[mat-sort-header]');
    this.deleteButtons = page.locator('tq-icon-button');
    this.emptyState = page.locator('tq-empty-state');
  }

  async goto() {
    await super.goto('/requirements');
  }

  async expectTableVisible() {
    await expect(this.requirementsTable).toBeVisible();
  }

  async expectRowCount(count: number) {
    const rows = await this.tableRows.count();
    expect(rows).toBe(count);
  }

  async expectPaginatorVisible() {
    await expect(this.paginator).toBeVisible();
  }

  async clickRow(index: number) {
    await expect(this.tableRows.first()).toBeVisible({ timeout: 5000 });
    await this.tableRows.nth(index).click();
    await this.page.waitForTimeout(1000);
  }

  async expectDetailDialogOpen() {
    const dialog = this.page.locator('.cdk-overlay-pane mat-dialog-container, .cdk-overlay-pane tq-detail-dialog, .cdk-overlay-pane feat-requirement-detail');
    await expect(dialog.first()).toBeVisible({ timeout: 5000 });
  }

  async expectMaterialSortUsed() {
    const sortCount = await this.sortHeaders.count();
    expect(sortCount).toBeGreaterThanOrEqual(1);
  }

  async expectMaterialTableUsed() {
    const matTable = this.page.locator('mat-table, table[mat-table]');
    await expect(matTable).toBeVisible();
  }

  async expectMaterialPaginatorUsed() {
    const matPaginator = this.page.locator('mat-paginator');
    await expect(matPaginator).toBeVisible();
  }

  async search(keyword: string) {
    await this.searchInput.fill(keyword);
    await this.searchInput.press('Enter');
    await this.page.waitForLoadState('networkidle');
  }
}
