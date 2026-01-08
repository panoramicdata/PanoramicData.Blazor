import { test, expect } from '@playwright/test';

test.describe('Core Navigation and Layout', () => {
  test('Mobile Navigation Toggle', async ({ page }) => {
    // 1. Navigate to http://localhost:5000/
    await page.goto('/');
    
    // 2. Set browser viewport to mobile size (375x667)
    await page.setViewportSize({ width: 375, height: 667 });
    
    // 3. Verify the hamburger menu button is visible
    const menuToggle = page.locator('.navbar-toggler');
    await expect(menuToggle).toBeVisible();
    
    // 4. Click the hamburger menu toggle button
    await menuToggle.click();
    
    // 5. Verify the navigation menu expands and shows all component links
    const navMenu = page.locator('.navbar-collapse');
    await expect(navMenu).not.toHaveClass(/collapse/);
    await expect(page.getByRole('link', { name: 'PDTable' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'PDTimeline' })).toBeVisible();
    
    // 6. Click on a component link (e.g., 'PDTable')
    await page.getByRole('link', { name: 'PDTable' }).click();
    
    // 7. Verify the page navigates correctly
    await expect(page.url()).toContain('pdtable');
    
    // 8. Verify the mobile menu collapses after navigation
    await expect(navMenu).toHaveClass(/collapse/);
    
    // 9. Click the hamburger menu button again to test toggle functionality
    await menuToggle.click();
    await expect(navMenu).not.toHaveClass(/collapse/);
  });
});
