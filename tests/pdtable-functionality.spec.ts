import { test, expect } from '@playwright/test';

test.describe('Data Visualization Components', () => {
  test('PDTable Component Functionality', async ({ page }) => {
    // 1. Navigate directly to the PDTable demo page.
    await page.goto('/pdtable');
    await expect(page.getByRole('heading', { name: 'PDTable', exact: true })).toBeVisible();
    
    // 3. Verify the table renders with sample data
    await expect(page.locator('table')).toBeVisible();
      const rowCount = await page.locator('tbody tr').count();
      expect(rowCount).toBeGreaterThan(0);
    
    // 4. Test column sorting by clicking on the first sortable column header
    const sortableHeaders = page.locator('thead th:has(.pd-sort)');
    const sortableCount = await sortableHeaders.count();
    expect(sortableCount).toBeGreaterThan(0);
    const firstSortControl = sortableHeaders.first().locator('.hdr > .pd-pointer.pd-sort');
    await firstSortControl.click();

    // 5. Verify the clicked column displays an active direction indicator.
    await expect(firstSortControl.locator('.fa-sort-up, .fa-sort-down')).toHaveCount(1);
    
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

    // Wide columns must scroll inside PDTable without widening the surrounding page or pager.
    const overflow = await page.locator('.pdtable').evaluate((tableViewport) => ({
      pageClientWidth: document.documentElement.clientWidth,
      pageScrollWidth: document.documentElement.scrollWidth,
      tableClientWidth: tableViewport.clientWidth,
      tableScrollWidth: tableViewport.scrollWidth,
    }));
    expect(overflow.tableScrollWidth).toBeGreaterThan(overflow.tableClientWidth);
    expect(overflow.pageScrollWidth).toBeLessThanOrEqual(overflow.pageClientWidth);

    const pagerBox = await page.locator('.pdpager').first().boundingBox();
    expect(pagerBox).not.toBeNull();
    expect(pagerBox!.x + pagerBox!.width).toBeLessThanOrEqual(overflow.pageClientWidth);
    
    // 8. Reset viewport
    await page.setViewportSize({ width: 1280, height: 720 });
  });
});
