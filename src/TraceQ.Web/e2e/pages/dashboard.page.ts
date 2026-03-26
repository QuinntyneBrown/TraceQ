import { type Locator, type Page, expect } from '@playwright/test';
import { LayoutPage } from './layout.page';

export class DashboardPage extends LayoutPage {
  readonly dashboardContent: Locator;
  readonly widgetGrid: Locator;
  readonly distributionCharts: Locator;
  readonly traceabilityWidget: Locator;

  constructor(page: Page) {
    super(page);
    this.dashboardContent = page.locator('.dashboard-page');
    this.widgetGrid = page.locator('.widget-grid');
    this.distributionCharts = page.locator('feat-distribution-chart');
    this.traceabilityWidget = page.locator('feat-traceability');
  }

  async goto() {
    await super.goto('/dashboard');
  }

  async expectDashboardVisible() {
    await expect(this.dashboardContent).toBeVisible();
  }

  async expectDistributionChartsVisible() {
    const count = await this.distributionCharts.count();
    expect(count).toBeGreaterThanOrEqual(2);
  }

  getDistributionChart(index: number): Locator {
    return this.distributionCharts.nth(index);
  }

  async expectWidgetGridLayout() {
    await expect(this.widgetGrid).toBeVisible();
    const style = await this.getComputedStyle(this.widgetGrid, 'display');
    expect(style).toBe('grid');
  }
}
