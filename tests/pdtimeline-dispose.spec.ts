import { test, expect } from '@playwright/test';

// Regression test for #206: disposing a PDTimeline must remove every listener its script added, and the
// module's setData export must not throw. bUnit cannot run the component's JavaScript, so this runs in a browser.

test.describe('PDTimeline script lifetime', () => {
  test.beforeEach(async ({ page }) => {
    // count the keydown/keyup listeners currently attached to window, by function identity
    await page.addInitScript(() => {
      const live = { keydown: new Set<unknown>(), keyup: new Set<unknown>() };
      const add = EventTarget.prototype.addEventListener;
      const remove = EventTarget.prototype.removeEventListener;
      EventTarget.prototype.addEventListener = function (type: string, listener: unknown, options?: unknown) {
        if (this === window && (type === 'keydown' || type === 'keyup')) {
          live[type].add(listener);
        }
        return add.call(this, type, listener as EventListener, options as AddEventListenerOptions);
      };
      EventTarget.prototype.removeEventListener = function (type: string, listener: unknown, options?: unknown) {
        if (this === window && (type === 'keydown' || type === 'keyup')) {
          live[type].delete(listener);
        }
        return remove.call(this, type, listener as EventListener, options as EventListenerOptions);
      };
      (window as unknown as { keyListenerCount: () => number }).keyListenerCount =
        () => live.keydown.size + live.keyup.size;
    });
  });

  test('navigating away from a timeline removes its key listeners and logs no errors', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', e => errors.push(e.message));
    page.on('console', m => { if (m.type() === 'error') { errors.push(m.text()); } });

    await page.goto('/');
    await page.waitForLoadState('networkidle');
    const baseline = await page.evaluate(() => (window as unknown as { keyListenerCount: () => number }).keyListenerCount());

    await page.getByRole('link', { name: 'PDTimeline' }).first().click();
    await expect(page.locator('.pd-timeline').first()).toBeVisible();
    await expect.poll(() => page.evaluate(() => (window as unknown as { keyListenerCount: () => number }).keyListenerCount()))
      .toBeGreaterThan(baseline);

    await page.getByRole('link', { name: 'Home' }).first().click();
    await expect(page.locator('.pd-timeline')).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => (window as unknown as { keyListenerCount: () => number }).keyListenerCount()))
      .toBe(baseline);

    // only errors raised by the key presses count: the demo logs unrelated ones (such as blocked audio autoplay)
    errors.length = 0;
    await page.keyboard.down('Shift');
    await page.keyboard.up('Shift');
    // an absence can only be observed over a window: a leaked handler's .NET call would fail asynchronously
    await page.waitForTimeout(500);
    expect(errors).toEqual([]);
  });

  test('setData does not throw', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    const baseline = await page.evaluate(() => (window as unknown as { keyListenerCount: () => number }).keyListenerCount());
    await page.getByRole('link', { name: 'PDTimeline' }).first().click();
    const timeline = page.locator('.pd-timeline').first();
    await expect(timeline).toBeVisible();
    const id = await timeline.getAttribute('id');
    // the timeline's script has initialised once its key listeners are attached
    await expect.poll(() => page.evaluate(() => (window as unknown as { keyListenerCount: () => number }).keyListenerCount()))
      .toBeGreaterThan(baseline);

    const error = await page.evaluate(async timelineId => {
      const module = await import('/_content/PanoramicData.Blazor/PDTimeline.razor.js');
      try {
        module.setData(timelineId, []);
        return null;
      } catch (e) {
        return String(e);
      }
    }, id);

    expect(error).toBeNull();
  });
});
