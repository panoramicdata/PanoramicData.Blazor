import { test, expect as baseExpect } from "@playwright/test";

// A cold server is slow to start Blazor.
const expect = baseExpect.configure({ timeout: 15000 });

test.describe("PDFormBody: hidden field labels", () => {
	test("each field's hidden label is positioned within its own field, so a scrolling panel clips it", async ({ page }) => {
		await page.goto("/pdform");
		const labels = page.locator(".pd-form-field > label.sr-only");

		// The button is prerendered before Blazor is interactive, so a first click can be lost.
		await expect(async () => {
			await page.getByRole("button", { name: "Create Person" }).click({ timeout: 2000 });
			await expect(labels.first()).toBeAttached({ timeout: 3000 });
		}).toPass({ timeout: 30000 });

		const placement = await labels.evaluateAll((elements) => elements.map((label) => ({
			position: getComputedStyle(label).position,
			insideItsField: label.offsetParent === label.parentElement,
		})));

		// The demo's Font Awesome stylesheet makes sr-only absolute, as Magic Suite's does.
		expect(placement.length).toBeGreaterThan(3);
		expect(placement.every((label) => label.position === "absolute")).toBe(true);
		expect(placement.filter((label) => !label.insideItsField)).toEqual([]);
	});
});
