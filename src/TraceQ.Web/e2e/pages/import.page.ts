import { type Locator, type Page, expect } from '@playwright/test';
import { LayoutPage } from './layout.page';

export class ImportPage extends LayoutPage {
  readonly dropZone: Locator;
  readonly fileInput: Locator;
  readonly historyTable: Locator;
  readonly historyRows: Locator;
  readonly paginator: Locator;
  readonly uploadButton: Locator;

  constructor(page: Page) {
    super(page);
    this.dropZone = page.locator('.drop-zone, .upload-zone, [class*="drop"], [class*="upload-area"]');
    this.fileInput = page.locator('input[type="file"]');
    this.historyTable = page.locator('mat-table, table');
    this.historyRows = page.locator('mat-row, tr[mat-row]');
    this.paginator = page.locator('mat-paginator');
    this.uploadButton = page.locator('tq-button, .drop-zone');
  }

  async goto() {
    await super.goto('/import');
  }

  async expectDropZoneVisible() {
    await expect(this.dropZone).toBeVisible();
  }

  async expectHistoryTableVisible() {
    await expect(this.historyTable).toBeVisible();
  }

  async expectHistoryRowCount(count: number) {
    const rows = await this.historyRows.count();
    expect(rows).toBe(count);
  }

  async expectPaginatorVisible() {
    await expect(this.paginator).toBeVisible();
  }

  async expectMaterialTableUsed() {
    const matTable = this.page.locator('mat-table, table[mat-table]');
    await expect(matTable).toBeVisible();
  }
}
