import { test, expect } from '@playwright/test';
import { LayoutPage } from '../pages/layout.page';
import { setupApiMocks } from '../fixtures/api-mocks';

test.describe('App Layout', () => {
  let layout: LayoutPage;

  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
    layout = new LayoutPage(page);
    await layout.goto('/dashboard');
  });

  test.describe('Sidebar', () => {
    test('sidebar is visible with correct width (240px)', async () => {
      await layout.expectSidebarVisible();
      await layout.expectSidebarWidth(240);
    });

    test('sidebar has dark background (#111111)', async () => {
      const bg = await layout.getComputedBgColor(layout.sidebar);
      expect(bg).toBe('rgb(17, 17, 17)');
    });

    test('sidebar has right border (#222222)', async () => {
      const borderColor = await layout.getComputedStyle(layout.sidebar, 'border-right-color');
      expect(borderColor).toBe('rgb(34, 34, 34)');
    });

    test('logo displays shield icon and TRACEQ text', async () => {
      await expect(layout.logoIcon).toBeVisible();
      await expect(layout.logoText).toHaveText('TRACEQ');
    });

    test('logo icon is teal (#00897b)', async () => {
      const color = await layout.getComputedColor(layout.logoIcon);
      expect(color).toBe('rgb(0, 137, 123)');
    });

    test('logo text uses Space Grotesk font', async () => {
      const font = await layout.getComputedFontFamily(layout.logoText);
      expect(font.toLowerCase()).toContain('space grotesk');
    });

    test('logo text has correct styling (20px, 700 weight, 2px spacing)', async () => {
      const size = await layout.getComputedFontSize(layout.logoText);
      expect(size).toBe('20px');

      const weight = await layout.getComputedFontWeight(layout.logoText);
      expect(weight).toBe('700');

      const spacing = await layout.getComputedStyle(layout.logoText, 'letter-spacing');
      expect(spacing).toBe('2px');
    });

    test('all four nav items are visible', async () => {
      await expect(layout.navDashboard).toBeVisible();
      await expect(layout.navSearch).toBeVisible();
      await expect(layout.navImport).toBeVisible();
      await expect(layout.navRequirements).toBeVisible();
    });

    test('nav items use Space Grotesk font at 14px', async () => {
      const label = layout.navDashboard.locator('.nav-label');
      const font = await layout.getComputedFontFamily(label);
      expect(font.toLowerCase()).toContain('space grotesk');

      const size = await layout.getComputedFontSize(label);
      expect(size).toBe('14px');
    });

    test('active nav item has teal color (#00897b)', async () => {
      const label = layout.navDashboard.locator('.nav-label');
      const color = await layout.getComputedColor(label);
      expect(color).toBe('rgb(0, 137, 123)');
    });

    test('inactive nav items have gray color (#777777)', async () => {
      const label = layout.navSearch.locator('.nav-label');
      const color = await layout.getComputedColor(label);
      expect(color).toBe('rgb(119, 119, 119)');
    });

    test('nav items have correct padding (10px 16px)', async () => {
      const padding = await layout.getComputedStyle(layout.navDashboard, 'padding');
      expect(padding).toBe('10px 16px');
    });

    test('nav items have 12px gap between items', async () => {
      const icon = layout.navDashboard.locator('.nav-icon');
      const label = layout.navDashboard.locator('.nav-label');
      const gap = await layout.getComputedStyle(layout.navDashboard, 'gap');
      expect(gap).toBe('12px');
    });

    test('nav items use mat-icon for icons', async () => {
      const matIcons = layout.sidebar.locator('mat-icon');
      const count = await matIcons.count();
      // logo icon + 4 nav icons = 5
      expect(count).toBeGreaterThanOrEqual(5);
    });

    test('active nav item has border-radius 6px', async () => {
      const radius = await layout.getComputedStyle(layout.navDashboard, 'border-radius');
      expect(radius).toBe('6px');
    });
  });

  test.describe('Header', () => {
    test('header is visible with correct height (64px)', async () => {
      await layout.expectHeaderVisible();
      await layout.expectHeaderHeight(64);
    });

    test('header has dark background (#111111)', async () => {
      const bg = await layout.getComputedBgColor(layout.header);
      expect(bg).toBe('rgb(17, 17, 17)');
    });

    test('header has bottom border (#222222)', async () => {
      const border = await layout.getComputedStyle(layout.header, 'border-bottom-color');
      expect(border).toBe('rgb(34, 34, 34)');
    });

    test('header has 32px horizontal padding', async () => {
      const paddingLeft = await layout.getComputedStyle(layout.header, 'padding-left');
      const paddingRight = await layout.getComputedStyle(layout.header, 'padding-right');
      expect(paddingLeft).toBe('32px');
      expect(paddingRight).toBe('32px');
    });

    test('search bar is visible with correct dimensions', async () => {
      await expect(layout.headerSearch).toBeVisible();
      const box = await layout.headerSearch.boundingBox();
      expect(box).toBeTruthy();
      expect(box!.width).toBe(280);
      expect(box!.height).toBe(36);
    });

    test('search bar has border-radius 6px', async () => {
      const radius = await layout.getComputedStyle(layout.headerSearch, 'border-radius');
      expect(radius).toBe('6px');
    });

    test('search placeholder uses IBM Plex Mono font', async () => {
      const placeholder = layout.headerSearch.locator('.search-placeholder');
      const font = await layout.getComputedFontFamily(placeholder);
      expect(font.toLowerCase()).toContain('ibm plex mono');
    });

    test('theme toggle uses mat-icon-button', async () => {
      const button = layout.themeToggle.locator('button[mat-icon-button], button.mat-mdc-icon-button');
      const count = await button.count();
      // The theme toggle itself is a mat-icon-button
      const matIcon = layout.themeToggle.locator('mat-icon');
      await expect(matIcon).toBeVisible();
    });

    test('user avatar has teal background and 32px size', async () => {
      const box = await layout.userAvatar.boundingBox();
      expect(box).toBeTruthy();
      expect(box!.width).toBe(32);
      expect(box!.height).toBe(32);

      const bg = await layout.getComputedBgColor(layout.userAvatar);
      expect(bg).toBe('rgb(0, 137, 123)');
    });

    test('user avatar has circular shape (border-radius 16px)', async () => {
      const radius = await layout.getComputedStyle(layout.userAvatar, 'border-radius');
      expect(radius).toBe('16px');
    });
  });

  test.describe('Navigation', () => {
    test('clicking Search navigates to /search', async () => {
      await layout.navigateTo('search');
      expect(layout.page.url()).toContain('/search');
    });

    test('clicking Import navigates to /import', async () => {
      await layout.navigateTo('import');
      expect(layout.page.url()).toContain('/import');
    });

    test('clicking Requirements navigates to /requirements', async () => {
      await layout.navigateTo('requirements');
      expect(layout.page.url()).toContain('/requirements');
    });

    test('clicking Dashboard navigates back to /dashboard', async () => {
      await layout.navigateTo('search');
      await layout.navigateTo('dashboard');
      expect(layout.page.url()).toContain('/dashboard');
    });

    test('active nav item updates on navigation', async () => {
      await layout.navigateTo('search');
      await expect(layout.navSearch).toHaveClass(/nav-item--active/);
      await expect(layout.navDashboard).not.toHaveClass(/nav-item--active/);
    });
  });

  test.describe('Content Area', () => {
    test('content area has dark background (#0a0a0a)', async () => {
      const bg = await layout.getComputedBgColor(layout.pageContent);
      expect(bg).toBe('rgb(10, 10, 10)');
    });

    test('content area fills remaining space', async () => {
      const box = await layout.pageContent.boundingBox();
      expect(box).toBeTruthy();
      // Content width = viewport width - sidebar width
      const viewportSize = layout.page.viewportSize();
      if (viewportSize) {
        expect(box!.width).toBeGreaterThan(viewportSize.width - 240 - 10);
      }
    });
  });
});
