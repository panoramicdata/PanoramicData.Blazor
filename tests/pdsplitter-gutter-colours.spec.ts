import { test, expect as baseExpect, type Page } from "@playwright/test";

// A cold server is slow to start Blazor.
const expect = baseExpect.configure({ timeout: 15000 });

const gutter = ".pdsplitter > .gutter";

async function openSplitters(page: Page) {
	await page.goto("/pdsplitter");
	await expect(page.locator(gutter).first()).toBeVisible();
	await page.mouse.move(0, 0);
}

async function background(page: Page) {
	return page.locator(gutter).first().evaluate((element) => getComputedStyle(element).backgroundColor);
}

// The colour a CSS value, such as a variable with its fallback, resolves to on this page.
async function resolve(page: Page, value: string) {
	return page.evaluate((colour) => {
		const probe = document.createElement("div");
		probe.style.backgroundColor = colour;
		document.body.appendChild(probe);
		const resolved = getComputedStyle(probe).backgroundColor;
		probe.remove();
		return resolved;
	}, value);
}

test.describe("PDSplitter gutter colours", () => {
	test("a gutter is light grey, and the primary colour under the pointer after a short delay", async ({ page }) => {
		await openSplitters(page);
		expect(await background(page)).toBe("rgb(238, 238, 238)");

		await page.locator(gutter).first().hover();

		expect(await page.locator(gutter).first().evaluate((element) => getComputedStyle(element).transitionDelay)).toBe("0.2s");
		await expect.poll(() => background(page)).toBe(await resolve(page, "var(--bs-primary, #0d6efd)"));
	});

	test("a host's variables colour the gutter and its highlight", async ({ page }) => {
		await openSplitters(page);
		await page.addStyleTag({ content: ":root { --pd-splitter-gutter-bg: rgb(1, 2, 3); --pd-splitter-gutter-hover-bg: rgb(4, 5, 6); }" });
		await expect.poll(() => background(page)).toBe("rgb(1, 2, 3)");

		await page.locator(gutter).first().hover();

		await expect.poll(() => background(page)).toBe("rgb(4, 5, 6)");
	});
});
