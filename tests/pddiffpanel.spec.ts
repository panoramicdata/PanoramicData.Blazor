import { test, expect, type Page } from "@playwright/test";

// The demo app loads Monaco asynchronously, so enter via the nav menu like the PDMonacoEditor spec
// rather than loading the page directly (a direct load races Monaco and fails for any Monaco editor).
async function openDiffPanel(page: Page, errors: string[]) {
	// Monaco falls back to the inline view when the editor is narrower than ~900px, so use a wide viewport.
	await page.setViewportSize({ width: 1800, height: 900 });
	page.on("console", (message) => {
		if (message.type() === "error") {
			errors.push(message.text());
		}
	});
	page.on("pageerror", (error) => errors.push(error.message));

	await page.goto("/");
	await page.waitForFunction(() => typeof (window as any).monaco !== "undefined");
	await page.getByRole("link", { name: "PDDiffPanel" }).click();
}

test.describe("PDDiffPanel", () => {
	test("renders a diff, live-updates and toggles layout", async ({ page }) => {
		await openDiffPanel(page, []);

		const diff = page.locator(".monaco-diff-editor");
		await expect(diff).toBeVisible();
		const modifiedText = page.locator(".monaco-editor.modified-in-monaco-diff-editor .lines-content > .view-lines");
		await expect(modifiedText).toContainText("Hello");

		// Editing the demo textarea updates the modified side without recreating the editor.
		await diff.evaluate((element) => element.setAttribute("data-marker", "same-editor"));
		await page.locator("#diff-modified").fill("public string Greet() => \"Updated text\";\n");
		await expect(modifiedText).toContainText("Updated text");
		await expect(page.locator(".monaco-diff-editor[data-marker='same-editor']")).toHaveCount(1);

		// Layout toggle.
		await expect(page.locator(".monaco-diff-editor.side-by-side")).toHaveCount(1);
		await page.getByRole("button", { name: "Inline" }).click();
		await expect(page.getByRole("button", { name: "Side by side" })).toBeVisible();
		await expect(page.locator(".monaco-diff-editor.side-by-side")).toHaveCount(0);
		await page.getByRole("button", { name: "Side by side" }).click();
		await expect(page.locator(".monaco-diff-editor.side-by-side")).toHaveCount(1);
	});

	// Known component bug: disposing the models before the diff widget has reset logs
	// "TextModel got disposed before DiffEditorWidget model got reset". Remove fixme once PDDiffPanel is fixed.
	test.fixme("navigating away disposes the panel without console errors", async ({ page }) => {
		const errors: string[] = [];
		await openDiffPanel(page, errors);
		await expect(page.locator(".monaco-diff-editor")).toBeVisible();

		await page.getByRole("link", { name: "PDPager" }).click();
		await expect(page.locator(".monaco-diff-editor")).toHaveCount(0);
		await page.waitForTimeout(500);
		expect(errors).toEqual([]);
	});
});
