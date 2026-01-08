import { test, expect } from '@playwright/test';

test.describe('Data Visualization Components', () => {
  test('PDTable Component Functionality', async ({ page }) => {
    // 1. Navigate to http://localhost:5000/
    await page.goto('/');
    
    // 2. Click on 'PDTable' in the navigation menu
    await page.getByRole('link', { name: 'PDTable', exact: true }).click();
    
    // 3. Verify the table renders with sample data
    await expect(page.locator('table')).toBeVisible();
      const rowCount = await page.locator('tbody tr').count();
      expect(rowCount).toBeGreaterThan(0);
    
    // 4. Test column sorting by clicking on the first sortable column header
    const sortableHeaders = page.locator('thead th:has(.pd-sort)');
    const sortableCount = await sortableHeaders.count();
    expect(sortableCount).toBeGreaterThan(0);
    const firstSortableHeader = sortableHeaders.first();
    await firstSortableHeader.click();

    // 5. Verify sort indicator appears only in the clicked (sortable) header
    await expect(firstSortableHeader.locator('.pd-sort')).toBeVisible();
    // Ensure other sortable headers do not have a visible sort indicator (if any others exist)
    for (let i = 1; i < sortableCount; i++) {
      const otherHeader = sortableHeaders.nth(i);
      await expect(otherHeader.locator('.pd-sort')).not.toBeVisible();
    }
    
    // 6. Test table pagination if available
    const paginationButtons = page.locator('.pagination button, .page-link');
    if (await paginationButtons.count() > 0) {
      const nextButton = paginationButtons.filter({ hasText: /next|>/i }).first();
      if (await nextButton.isVisible()) {
        await nextButton.click();
        await expect(page.locator('tbody tr')).toBeVisible();
      }
    }
    
    // 7. Test responsive behavior by resizing browser window
    await page.setViewportSize({ width: 768, height: 600 });
    await expect(page.locator('table')).toBeVisible();
    
    // 8. Reset viewport
    await page.setViewportSize({ width: 1280, height: 720 });
  });
});
