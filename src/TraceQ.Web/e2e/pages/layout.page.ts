import { type Locator, type Page, expect } from '@playwright/test';

export class LayoutPage {
  readonly page: Page;

  // Sidebar
  readonly sidebar: Locator;
  readonly sidebarLogo: Locator;
  readonly logoIcon: Locator;
  readonly logoText: Locator;
  readonly navDashboard: Locator;
  readonly navSearch: Locator;
  readonly navImport: Locator;
  readonly navRequirements: Locator;

  // Header
  readonly header: Locator;
  readonly headerSearch: Locator;
  readonly themeToggle: Locator;
  readonly userAvatar: Locator;

  // Content
  readonly pageContent: Locator;

  constructor(page: Page) {
    this.page = page;

    this.sidebar = page.getByTestId('sidebar');
    this.sidebarLogo = page.getByTestId('sidebar-logo');
    this.logoIcon = this.sidebarLogo.locator('mat-icon');
    this.logoText = this.sidebarLogo.locator('.logo-text');

    this.navDashboard = page.getByTestId('nav-dashboard');
    this.navSearch = page.getByTestId('nav-search');
    this.navImport = page.getByTestId('nav-upload');
    this.navRequirements = page.getByTestId('nav-requirements');

    this.header = page.getByTestId('header');
    this.headerSearch = page.getByTestId('header-search');
    this.themeToggle = page.getByTestId('theme-toggle');
    this.userAvatar = page.getByTestId('user-avatar');

    this.pageContent = page.getByTestId('page-content');
  }

  async goto(path = '/') {
    await this.page.goto(path);
    await this.page.waitForLoadState('networkidle');
  }

  async navigateTo(item: 'dashboard' | 'search' | 'import' | 'requirements') {
    const navMap = {
      dashboard: this.navDashboard,
      search: this.navSearch,
      import: this.navImport,
      requirements: this.navRequirements,
    };
    await navMap[item].click();
    await this.page.waitForLoadState('networkidle');
  }

  getNavItem(name: string): Locator {
    return this.page.locator(`[data-testid="nav-${name}"]`);
  }

  async expectSidebarVisible() {
    await expect(this.sidebar).toBeVisible();
    await expect(this.sidebarLogo).toBeVisible();
  }

  async expectHeaderVisible() {
    await expect(this.header).toBeVisible();
    await expect(this.headerSearch).toBeVisible();
    await expect(this.themeToggle).toBeVisible();
    await expect(this.userAvatar).toBeVisible();
  }

  async expectSidebarWidth(width: number) {
    const box = await this.sidebar.boundingBox();
    expect(box).toBeTruthy();
    expect(box!.width).toBe(width);
  }

  async expectHeaderHeight(height: number) {
    const box = await this.header.boundingBox();
    expect(box).toBeTruthy();
    expect(box!.height).toBe(height);
  }

  async getComputedStyle(locator: Locator, property: string): Promise<string> {
    return locator.evaluate((el, prop) => {
      return window.getComputedStyle(el).getPropertyValue(prop);
    }, property);
  }

  async getComputedColor(locator: Locator): Promise<string> {
    return this.getComputedStyle(locator, 'color');
  }

  async getComputedBgColor(locator: Locator): Promise<string> {
    return this.getComputedStyle(locator, 'background-color');
  }

  async getComputedFontFamily(locator: Locator): Promise<string> {
    return this.getComputedStyle(locator, 'font-family');
  }

  async getComputedFontSize(locator: Locator): Promise<string> {
    return this.getComputedStyle(locator, 'font-size');
  }

  async getComputedFontWeight(locator: Locator): Promise<string> {
    return this.getComputedStyle(locator, 'font-weight');
  }
}
