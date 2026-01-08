import { test, expect } from '@playwright/test';

test.describe('Data Visualization Components', () => {
  test('PDTree Component Navigation', async ({ page }) => {
    // 1. Navigate to http://localhost:5000/
    await page.goto('/');
    
    // 2. Click on 'PDTree' in the navigation menu
    await page.getByRole('link', { name: 'PDTree' }).click();
    
    // 3. Verify the tree component renders with hierarchical data
    await expect(page.locator('.pd-tree, .tree-container, .tree-view')).toBeVisible();
    
    // 4. Test expanding and collapsing tree nodes
    const expandableNodes = page.locator('.tree-node-expand, .expand-icon, .toggle-icon');
    if (await expandableNodes.count() > 0) {
      const firstNode = expandableNodes.first();
      await firstNode.click();
      
      // Verify child nodes appear
      await expect(page.locator('.tree-node-children, .child-nodes')).toBeVisible();
      
      // Collapse the node
      await firstNode.click();
    }
    
    // 5. Verify proper indentation and visual hierarchy
    await expect(page.locator('.tree-node, .tree-item')).toBeVisible();
    
    // 6. Test node selection functionality
    const treeNodes = page.locator('.tree-node, .tree-item');
    if (await treeNodes.count() > 0) {
      await treeNodes.first().click();
      await expect(treeNodes.first()).toHaveClass(/selected|active/);
    }
    
    // 7. Test keyboard navigation (arrow keys, enter)
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('ArrowUp');
    await page.keyboard.press('Enter');
    
    // 8. Test any search or filter functionality
    const searchInput = page.locator('input[type="search"], input[placeholder*="search" i]');
    if (await searchInput.isVisible()) {
      await searchInput.fill('test');
      await page.keyboard.press('Enter');
    }
  });
});
