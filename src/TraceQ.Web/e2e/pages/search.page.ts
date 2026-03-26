import { type Locator, type Page, expect } from '@playwright/test';
import { LayoutPage } from './layout.page';

export class SearchPage extends LayoutPage {
  readonly searchInput: Locator;
  readonly searchButton: Locator;
  readonly filterSelects: Locator;
  readonly resultsList: Locator;
  readonly resultCards: Locator;
  readonly emptyState: Locator;
  readonly spinner: Locator;

  constructor(page: Page) {
    super(page);
    this.searchInput = page.locator('mat-form-field input[type="text"], input[matInput]').first();
    this.searchButton = page.locator('tq-button').filter({ hasText: /search/i }).first();
    this.filterSelects = page.locator('mat-select');
    this.resultsList = page.locator('.search-results, .results-list');
    this.resultCards = page.locator('mat-card.result-card');
    this.emptyState = page.locator('tq-empty-state');
    this.spinner = page.locator('mat-spinner');
  }

  async goto() {
    await super.goto('/search');
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
    await this.page.waitForLoadState('networkidle');
  }

  async expectResultsVisible() {
    await expect(this.resultCards.first()).toBeVisible({ timeout: 5000 });
  }

  async expectEmptyState() {
    await expect(this.emptyState).toBeVisible();
  }

  async getResultCount(): Promise<number> {
    return this.resultCards.count();
  }

  async expectFiltersPresent() {
    const filterCount = await this.filterSelects.count();
    expect(filterCount).toBeGreaterThanOrEqual(1);
  }

  async expectSearchInputUsesMonoFont() {
    const fontFamily = await this.getComputedFontFamily(this.searchInput);
    expect(fontFamily.toLowerCase()).toContain('ibm plex mono');
  }
}
