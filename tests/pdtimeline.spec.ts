import { test, expect } from '@playwright/test';

test.describe('Data Visualization Components', () => {
  test('PDTimeline Component', async ({ page }) => {
    // 1. Navigate to http://localhost:5000/
    await page.goto('/');
    
    // 2. Click on 'PDTimeline' in the navigation menu
    await page.getByRole('link', { name: 'PDTimeline' }).click();
    
    // 3. Verify the timeline component renders with time-based data
    await expect(page.locator('svg, .timeline-container, .pd-timeline')).toBeVisible();
    
    // 4. Test timeline zoom functionality (zoom in/out)
    const zoomInButton = page.locator('button').filter({ hasText: /zoom.*in|\+/i }).first();
    if (await zoomInButton.isVisible()) {
      await zoomInButton.click();
    }
    
    const zoomOutButton = page.locator('button').filter({ hasText: /zoom.*out|\-/i }).first();
    if (await zoomOutButton.isVisible()) {
      await zoomOutButton.click();
    }
    
    // 5. Test timeline panning by dragging
    const timelineElement = page.locator('svg, .timeline-container, .pd-timeline').first();
    const box = await timelineElement.boundingBox();
    if (box) {
      await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
      await page.mouse.down();
      await page.mouse.move(box.x + box.width / 2 + 50, box.y + box.height / 2);
      await page.mouse.up();
    }
    
    // 6. Test timeline responsiveness on different screen sizes
    await page.setViewportSize({ width: 768, height: 600 });
    await expect(timelineElement).toBeVisible();
    await page.setViewportSize({ width: 1280, height: 720 });
  });
});
