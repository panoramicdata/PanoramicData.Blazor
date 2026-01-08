import { test, expect } from '@playwright/test';

test.describe('Core Navigation and Layout', () => {
  test('Homepage Display and Navigation', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: /PanoramicData\.Blazor/ })).toBeVisible();
    await expect(page.locator('text=version')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'UI components for Blazor' })).toBeVisible();
    await expect(page.locator('text=Magic Suite')).toBeVisible();
    await expect(page.getByRole('link', { name: 'GitHub', exact: true })).toBeVisible();
    const componentList = page.locator('ul').first();
    await expect(componentList.locator('text=Block Overlay')).toBeVisible();
    await expect(componentList.locator('text=Table')).toBeVisible();
    await expect(componentList.locator('text=Pager')).toBeVisible();
    await expect(componentList.locator('text=Tree')).toBeVisible();
    await expect(componentList.locator('text=Splitter')).toBeVisible();
    await expect(componentList.locator('text=File Explorer')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Getting Started' })).toBeVisible();
    const installationLink = page.getByRole('link', { name: /Installation/i });
    await expect(installationLink).toBeVisible();
    await installationLink.click();
    await expect(page).toHaveURL(/installation/);
  });

  test('Navigation menu contains all major component links', async ({ page }) => {
    await page.goto('/');
    const navMenu = page.locator('nav');
    await expect(navMenu.locator('text=Timeline')).toBeVisible();
    await expect(navMenu.locator('text=Table')).toBeVisible();
    await expect(navMenu.locator('text=Tree')).toBeVisible();
    await expect(navMenu.locator('text=Form')).toBeVisible();
    await expect(navMenu.locator('text=Splitter')).toBeVisible();
    await expect(navMenu.locator('text=Monaco Editor')).toBeVisible();
  });

  test('Clicking component links navigates to correct demo pages', async ({ page }) => {
    await page.goto('/');
    const links = [
      { name: 'Timeline', urlPart: 'timeline' },
      { name: 'Table', urlPart: 'table' },
      { name: 'Tree', urlPart: 'tree' },
      { name: 'Form', urlPart: 'form' },
      { name: 'Splitter', urlPart: 'splitter' },
      { name: 'Monaco Editor', urlPart: 'monaco' }
    ];
    for (const { name, urlPart } of links) {
      const link = page.getByRole('link', { name });
      await expect(link).toBeVisible();
      await link.click();
      await expect(page).toHaveURL(new RegExp(urlPart, 'i'));
      await page.goBack();
    }
  });

  test('Footer displays copyright and GitHub link', async ({ page }) => {
    await page.goto('/');
    const footer = page.locator('footer');
    await expect(footer).toBeVisible();
    await expect(footer.locator('text=©')).toBeVisible();
    await expect(footer.getByRole('link', { name: 'GitHub', exact: true })).toBeVisible();
  });

  test('Search bar is present and functional', async ({ page }) => {
    await page.goto('/');
    const searchInput = page.getByRole('textbox', { name: /search/i });
    await expect(searchInput).toBeVisible();
    await searchInput.fill('Table');
    await expect(page.locator('text=Table')).toBeVisible();
  });
});