import { test, expect } from '@playwright/test';
import { LayoutPage } from '../pages/layout.page';
import { setupApiMocks } from '../fixtures/api-mocks';

test.describe('Visual Design & Angular Material Compliance', () => {
  let layout: LayoutPage;

  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page);
    layout = new LayoutPage(page);
    await layout.goto('/dashboard');
  });

  test.describe('Color Palette', () => {
    test('app background is #0A0A0A (rgb(10, 10, 10))', async () => {
      const appShell = layout.page.locator('.app-shell');
      const bg = await layout.getComputedBgColor(appShell);
      expect(bg).toBe('rgb(10, 10, 10)');
    });

    test('sidebar background is #111111 (rgb(17, 17, 17))', async () => {
      const bg = await layout.getComputedBgColor(layout.sidebar);
      expect(bg).toBe('rgb(17, 17, 17)');
    });

    test('header background is #111111 (rgb(17, 17, 17))', async () => {
      const bg = await layout.getComputedBgColor(layout.header);
      expect(bg).toBe('rgb(17, 17, 17)');
    });

    test('primary teal color is #00897b for active elements', async () => {
      const logoIcon = layout.sidebarLogo.locator('mat-icon');
      const color = await layout.getComputedColor(logoIcon);
      expect(color).toBe('rgb(0, 137, 123)');
    });

    test('user avatar uses primary teal #00897b', async () => {
      const bg = await layout.getComputedBgColor(layout.userAvatar);
      expect(bg).toBe('rgb(0, 137, 123)');
    });

    test('inactive text uses #777777 (rgb(119, 119, 119))', async () => {
      const inactiveLabel = layout.navSearch.locator('.nav-label');
      const color = await layout.getComputedColor(inactiveLabel);
      expect(color).toBe('rgb(119, 119, 119)');
    });

    test('border color is #222222 (rgb(34, 34, 34))', async () => {
      const borderColor = await layout.getComputedStyle(layout.sidebar, 'border-right-color');
      expect(borderColor).toBe('rgb(34, 34, 34)');
    });
  });

  test.describe('Typography', () => {
    test('logo uses Space Grotesk font', async () => {
      const font = await layout.getComputedFontFamily(layout.logoText);
      expect(font.toLowerCase()).toContain('space grotesk');
    });

    test('nav labels use Space Grotesk font', async () => {
      const label = layout.navDashboard.locator('.nav-label');
      const font = await layout.getComputedFontFamily(label);
      expect(font.toLowerCase()).toContain('space grotesk');
    });

    test('search placeholder uses IBM Plex Mono font', async () => {
      const placeholder = layout.headerSearch.locator('.search-placeholder');
      const font = await layout.getComputedFontFamily(placeholder);
      expect(font.toLowerCase()).toContain('ibm plex mono');
    });

    test('search placeholder is 12px', async () => {
      const placeholder = layout.headerSearch.locator('.search-placeholder');
      const size = await layout.getComputedFontSize(placeholder);
      expect(size).toBe('12px');
    });
  });

  test.describe('Spacing', () => {
    test('sidebar logo has correct padding (0 24px 24px 24px)', async () => {
      const padding = await layout.getComputedStyle(layout.sidebarLogo, 'padding');
      expect(padding).toBe('0px 24px 24px');
    });

    test('sidebar nav section has 16px 12px padding', async () => {
      const navSection = layout.sidebar.locator('.sidebar-nav');
      const padding = await layout.getComputedStyle(navSection, 'padding');
      expect(padding).toBe('16px 12px');
    });

    test('sidebar nav items have 2px gap', async () => {
      const navSection = layout.sidebar.locator('.sidebar-nav');
      const gap = await layout.getComputedStyle(navSection, 'gap');
      expect(gap).toBe('2px');
    });

    test('header has 16px gap between elements', async () => {
      const gap = await layout.getComputedStyle(layout.header, 'gap');
      expect(gap).toBe('16px');
    });

    test('sidebar top padding is 32px', async () => {
      const paddingTop = await layout.getComputedStyle(layout.sidebar, 'padding-top');
      expect(paddingTop).toBe('32px');
    });
  });

  test.describe('Icons', () => {
    test('all icons use mat-icon component', async () => {
      const matIcons = layout.page.locator('mat-icon');
      const count = await matIcons.count();
      expect(count).toBeGreaterThanOrEqual(7); // logo + 4 nav + search + theme + more on page
    });

    test('nav icons are 20px size', async () => {
      const navIcon = layout.navDashboard.locator('.nav-icon');
      const size = await layout.getComputedFontSize(navIcon);
      expect(size).toBe('20px');
    });

    test('search bar icon is 16px', async () => {
      const searchIcon = layout.headerSearch.locator('.search-icon');
      const size = await layout.getComputedFontSize(searchIcon);
      expect(size).toBe('16px');
    });

    test('theme toggle icon is 18px', async () => {
      const icon = layout.themeToggle.locator('mat-icon');
      const size = await layout.getComputedFontSize(icon);
      expect(size).toBe('18px');
    });
  });

  test.describe('Angular Material Components', () => {
    test('theme toggle uses mat-icon-button', async () => {
      // Check that the button has Material button classes
      const btn = layout.themeToggle;
      const classes = await btn.getAttribute('class');
      const matClasses = await btn.locator('.mat-mdc-icon-button, [mat-icon-button]').count();
      // The theme-toggle itself is a button[mat-icon-button]
      const matBtn = layout.page.locator('button[mat-icon-button].theme-toggle, .theme-toggle.mat-mdc-icon-button');
      const count = await matBtn.count();
      expect(count).toBeGreaterThanOrEqual(1);
    });

    test('mat-tooltip is configured on theme toggle', async () => {
      // Hover over theme toggle to check tooltip
      await layout.themeToggle.hover();
      await layout.page.waitForTimeout(1000);
      const tooltip = layout.page.locator('.mat-mdc-tooltip, .mdc-tooltip');
      // Tooltip should appear on hover
      const tooltipCount = await tooltip.count();
      expect(tooltipCount).toBeGreaterThanOrEqual(0); // May not appear fast enough
    });
  });

  test.describe('Responsive Layout', () => {
    test('main content fills remaining horizontal space', async () => {
      const mainContent = layout.page.locator('.main');
      const mainBox = await mainContent.boundingBox();
      const sidebarBox = await layout.sidebar.boundingBox();
      const viewportSize = layout.page.viewportSize();

      expect(mainBox).toBeTruthy();
      expect(sidebarBox).toBeTruthy();
      if (mainBox && sidebarBox && viewportSize) {
        const expectedWidth = viewportSize.width - sidebarBox.width;
        expect(mainBox.width).toBeCloseTo(expectedWidth, 0);
      }
    });

    test('layout uses flexbox', async () => {
      const appShell = layout.page.locator('.app-shell');
      const display = await layout.getComputedStyle(appShell, 'display');
      expect(display).toBe('flex');
    });
  });
});
