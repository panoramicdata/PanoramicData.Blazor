import { test, expect, type Page } from "@playwright/test";

// Regression test for #206: disposing a PDTimeline must remove every listener its script added, and the
// module's setData export must not throw. bUnit cannot run the component's JavaScript, so this runs in a browser.

type KeyListeners = Record<"keydown" | "keyup", Set<unknown>>;
type KeyListenerWindow = { keyListeners: KeyListeners };

// init script: counts the keydown/keyup listeners currently attached to window, by function identity
function trackKeyListeners() {
	const live: KeyListeners = { keydown: new Set(), keyup: new Set() };
	const track = (
		method: "addEventListener" | "removeEventListener",
		operation: "add" | "delete",
	) => {
		const original = EventTarget.prototype[method];
		EventTarget.prototype[method] = function (
			this: EventTarget,
			type: string,
			listener: unknown,
			options?: unknown,
		) {
			if (this === window && ["keydown", "keyup"].includes(type)) {
				live[type as keyof KeyListeners][operation](listener);
			}
			Reflect.apply(original, this, [type, listener, options]);
		};
	};
	track("addEventListener", "add");
	track("removeEventListener", "delete");
	(window as unknown as KeyListenerWindow).keyListeners = live;
}

// the number of keydown/keyup listeners attached to window, as tracked by trackKeyListeners
const keyListenerCount = (page: Page) =>
	page.evaluate(() => {
		const { keydown, keyup } = (window as unknown as KeyListenerWindow)
			.keyListeners;
		return keydown.size + keyup.size;
	});

// opens the PDTimeline demo from the home page and waits until the timeline's script has attached its
// key listeners; returns the listener count from before the timeline was opened
async function openTimeline(page: Page): Promise<number> {
	await page.goto("/");
	await page.waitForLoadState("networkidle");
	const baseline = await keyListenerCount(page);
	await page.getByRole("link", { name: "PDTimeline" }).first().click();
	await expect(page.locator(".pd-timeline").first()).toBeVisible();
	await expect.poll(() => keyListenerCount(page)).toBeGreaterThan(baseline);
	return baseline;
}

test.describe("PDTimeline script lifetime", () => {
	test.beforeEach(({ page }) => page.addInitScript(trackKeyListeners));

	test("navigating away from a timeline removes its key listeners and logs no errors", async ({
		page,
	}) => {
		const errors: string[] = [];
		page.on("pageerror", (e) => errors.push(e.message));
		page.on("console", (m) => {
			if (m.type() === "error") {
				errors.push(m.text());
			}
		});

		const baseline = await openTimeline(page);

		await page.getByRole("link", { name: "Home" }).first().click();
		await expect(page.locator(".pd-timeline")).toHaveCount(0);
		await expect.poll(() => keyListenerCount(page)).toBe(baseline);

		// only errors raised by the key presses count: the demo logs unrelated ones (such as blocked audio autoplay)
		errors.length = 0;
		await page.keyboard.down("Shift");
		await page.keyboard.up("Shift");
		// an absence can only be observed over a window: a leaked handler's .NET call would fail asynchronously
		await page.waitForTimeout(500);
		expect(errors).toEqual([]);
	});

	test("setData does not throw", async ({ page }) => {
		await openTimeline(page);
		const id = await page.locator(".pd-timeline").first().getAttribute("id");

		// evaluate rejects with the error if setData throws
		await expect(
			page.evaluate(async (timelineId) => {
				const module =
					await import("/_content/PanoramicData.Blazor/PDTimeline.razor.js");
				module.setData(timelineId, []);
			}, id),
		).resolves.toBeUndefined();
	});
});
