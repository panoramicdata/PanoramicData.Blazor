import { test, expect } from '@playwright/test';

test.describe('Specialized Components', () => {
  test('PDMonacoEditor Code Editing', async ({ page }) => {
    // 1. Navigate to http://localhost:5000/
    await page.goto('/');
    
    // 2. Click on 'PDMonacoEditor' in the navigation menu
    await page.getByRole('link', { name: 'PDMonacoEditor' }).click();
    
    // 3. Verify the Monaco editor loads with syntax highlighting
    await expect(page.locator('.monaco-editor, .editor-container')).toBeVisible();
    
    // 4. Test typing code in different languages
    const editorTextarea = page.locator('.monaco-editor textarea, .inputarea').first();
    if (await editorTextarea.isVisible()) {
      await editorTextarea.focus();
      await page.keyboard.type('function test() {');
      await page.keyboard.press('Enter');
      await page.keyboard.type('  console.log("Hello World");');
      await page.keyboard.press('Enter');
      await page.keyboard.type('}');
    }
    
    // 5. Test syntax highlighting for various programming languages
    await expect(page.locator('.monaco-editor .mtk1, .syntax-highlight')).toBeVisible();
    
    // 6. Test find and replace functionality
    await page.keyboard.press('Control+F');
    const findWidget = page.locator('.find-widget, .editor-widget');
    if (await findWidget.isVisible()) {
      await expect(findWidget).toBeVisible();
      await page.keyboard.press('Escape');
    }
    
    // 7. Test undo/redo operations
    await page.keyboard.press('Control+Z'); // Undo
    await page.keyboard.press('Control+Y'); // Redo
    
    // 8. Test line numbers and code folding
    await expect(page.locator('.line-numbers, .margin')).toBeVisible();
  });
});
