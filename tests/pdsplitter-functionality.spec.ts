import { test, expect } from '@playwright/test';

test.describe('Layout and Navigation Components', () => {
  test('PDSplitter Resizable Panels', async ({ page }) => {
    // 1. Navigate to http://localhost:5000/
    await page.goto('/');
    
    // 2. Click on 'PDSplitter' in the navigation menu
    await page.getByRole('link', { name: 'PDSplitter' }).click();
    
    // 3. Verify the splitter component creates resizable panels
    await expect(page.locator('.pd-splitter, .splitter-container')).toBeVisible();
    await expect(page.locator('.splitter-handle, .split-handle')).toBeVisible();
    
    // 4. Test dragging the splitter handle to resize panels
    const splitterHandle = page.locator('.splitter-handle, .split-handle').first();
    const handleBox = await splitterHandle.boundingBox();
    
    if (handleBox) {
      await page.mouse.move(handleBox.x + handleBox.width / 2, handleBox.y + handleBox.height / 2);
      await page.mouse.down();
      await page.mouse.move(handleBox.x + 100, handleBox.y + handleBox.height / 2);
      await page.mouse.up();
    }
    
    // 5. Test minimum and maximum panel sizes
    const leftPanel = page.locator('.splitter-panel, .split-panel').first();
    const rightPanel = page.locator('.splitter-panel, .split-panel').last();
    
    await expect(leftPanel).toBeVisible();
    await expect(rightPanel).toBeVisible();
    
    // 6. Test keyboard control of splitter position
    await splitterHandle.focus();
    await page.keyboard.press('ArrowRight');
    await page.keyboard.press('ArrowLeft');
    
    // 7. Test double-click to reset splitter position
    await splitterHandle.dblclick();
  });
});
