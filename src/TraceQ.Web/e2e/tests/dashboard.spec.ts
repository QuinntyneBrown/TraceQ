import { test, expect } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { setupApiMocks } from '../fixtures/api-mocks';

test.describe('Dashboard Page', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
    dashboard = new DashboardPage(page);
    await dashboard.goto();
  });

  test.describe('Layout & Structure', () => {
    test('dashboard page is visible', async () => {
      await dashboard.expectDashboardVisible();
    });

    test('dashboard navigation item is active', async () => {
      await expect(dashboard.navDashboard).toHaveClass(/nav-item--active/);
    });

    test('widget grid uses CSS grid layout', async () => {
      await dashboard.expectWidgetGridLayout();
    });

    test('widget grid has 20px gap between widgets', async () => {
      const gap = await dashboard.getComputedStyle(dashboard.widgetGrid, 'gap');
      expect(gap).toBe('20px');
    });

    test('dashboard has 24px padding', async () => {
      const padding = await dashboard.getComputedStyle(dashboard.dashboardContent, 'padding');
      expect(padding).toBe('24px');
    });
  });

  test.describe('Distribution Charts', () => {
    test('distribution charts are rendered', async () => {
      await dashboard.expectDistributionChartsVisible();
    });

    test('charts use mat-card from Angular Material', async () => {
      const matCards = dashboard.page.locator('feat-distribution-chart mat-card, feat-distribution-chart .mat-mdc-card');
      const count = await matCards.count();
      expect(count).toBeGreaterThanOrEqual(2);
    });

    test('chart titles use correct font (Space Grotesk)', async () => {
      // Find chart title text
      const titles = dashboard.page.locator('.chart-title, feat-distribution-chart h2, feat-distribution-chart .title');
      if (await titles.count() > 0) {
        const font = await dashboard.getComputedFontFamily(titles.first());
        expect(font.toLowerCase()).toContain('space grotesk');
      }
    });
  });

  test.describe('Traceability Widget', () => {
    test('traceability widget is rendered', async () => {
      await expect(dashboard.traceabilityWidget).toBeVisible();
    });
  });

  test.describe('Angular Material Usage', () => {
    test('page uses mat-icon components', async () => {
      const matIcons = dashboard.page.locator('mat-icon');
      const count = await matIcons.count();
      expect(count).toBeGreaterThanOrEqual(5);
    });

    test('page uses mat-card for widgets', async () => {
      const matCards = dashboard.page.locator('mat-card, .mat-mdc-card');
      const count = await matCards.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('refresh buttons use mat-icon-button or tq-icon-button', async () => {
      const buttons = dashboard.page.locator('tq-icon-button, button[mat-icon-button]');
      const count = await buttons.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });
  });
});
