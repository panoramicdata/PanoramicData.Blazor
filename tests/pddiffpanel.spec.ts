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

		// The demo passes Theme="vs-dark": the diff renders on a dark background.
		await expect(page.locator(".monaco-editor.vs-dark").first()).toBeVisible();
		const background = await diff.evaluate((element) => getComputedStyle(element.querySelector(".monaco-editor")!).getPropertyValue("--vscode-editor-background").trim());
		expect(background.toLowerCase()).toBe("#1e1e1e");

		// Editing the demo textarea updates the modified side without recreating the editor.
		await diff.evaluate((element) => element.setAttribute("data-marker", "same-editor"));
		const editorNode = await diff.elementHandle();
		await page.locator("#diff-modified").fill("public string Greet() => \"Updated text\";\n");
		await expect(modifiedText).toContainText("Updated text");
		await expect(page.locator(".monaco-diff-editor[data-marker='same-editor']")).toHaveCount(1);
		expect(await page.evaluate((node) => node === document.querySelector(".monaco-diff-editor"), editorNode)).toBe(true);

		// Layout toggle.
		await expect(page.locator(".monaco-diff-editor.side-by-side")).toHaveCount(1);
		await page.getByRole("button", { name: "Inline" }).click();
		await expect(page.getByRole("button", { name: "Side by side" })).toBeVisible();
		await expect(page.locator(".monaco-diff-editor.side-by-side")).toHaveCount(0);
		await page.getByRole("button", { name: "Side by side" }).click();
		await expect(page.locator(".monaco-diff-editor.side-by-side")).toHaveCount(1);
	});

	test("navigating away disposes the panel without console errors", async ({ page }) => {
		const errors: string[] = [];
		await openDiffPanel(page, errors);
		await expect(page.locator(".monaco-diff-editor")).toBeVisible();

		await page.getByRole("link", { name: "PDPager" }).click();
		await expect(page.locator(".monaco-diff-editor")).toHaveCount(0);
		await page.waitForTimeout(500);
		expect(errors).toEqual([]);
	});

	test("stays side by side in a narrow pane", async ({ page }) => {
		await openDiffPanel(page, []);
		await expect(page.locator(".monaco-diff-editor.side-by-side")).toHaveCount(1);
		await page.setViewportSize({ width: 700, height: 900 });
		await page.waitForTimeout(500);
		await expect(page.locator(".monaco-diff-editor.side-by-side")).toHaveCount(1);
		await expect(page.getByRole("button", { name: "Inline" })).toBeVisible();
	});

	test("recovers when the page is loaded directly before Monaco is ready", async ({ page }) => {
		await page.setViewportSize({ width: 1800, height: 900 });
		await page.goto("/pddiffpanel");
		await expect(page.locator(".monaco-diff-editor")).toBeVisible({ timeout: 15000 });
		await expect(page.locator(".monaco-editor.modified-in-monaco-diff-editor .lines-content > .view-lines")).toContainText("Hello");
		await expect(page.locator("#blazor-error-ui")).toBeHidden();
	});

	test("scroll position survives a live refresh", async ({ page }) => {
		await openDiffPanel(page, []);
		await expect(page.locator(".monaco-diff-editor")).toBeVisible();

		const lines = Array.from({ length: 200 }, (_, i) => `line ${i + 1};`);
		await page.locator("#diff-original").fill(lines.join("\n") + "\n");
		await page.locator("#diff-modified").fill(lines.join("\n") + "\nextra;\n");
		await expect(page.locator(".monaco-editor.modified-in-monaco-diff-editor .lines-content > .view-lines")).toContainText("line 1;");

		const scrollTop = () => page.evaluate(() => {
			const id = document.querySelector(".monaco-editor-container")!.id;
			return (window as any).blazorMonaco.editors.find((h: any) => h.id === id + "_modified").editor.getScrollTop();
		});
		await page.evaluate(() => {
			const id = document.querySelector(".monaco-editor-container")!.id;
			(window as any).blazorMonaco.editors.find((h: any) => h.id === id + "_modified").editor.setScrollTop(1000);
		});
		await expect.poll(scrollTop).toBeGreaterThan(900);
		const before = await scrollTop();

		// A live refresh: change one line inside the scrolled-to viewport.
		const edited = [...lines];
		edited[60] = "line 61 EDITED;";
		await page.locator("#diff-modified").fill(edited.join("\n") + "\nextra;\n");
		await expect(page.locator(".monaco-editor.modified-in-monaco-diff-editor .lines-content > .view-lines")).toContainText("EDITED");

		expect(Math.abs((await scrollTop()) - before)).toBeLessThan(100);
	});

	test("mounting and unmounting does not grow BlazorMonaco's editor registry", async ({ page }) => {
		await openDiffPanel(page, []);
		await expect(page.locator(".monaco-diff-editor")).toBeVisible();
		const count = () => page.evaluate(() => (window as any).blazorMonaco.editors.length);
		const mounted = await count();

		for (let i = 0; i < 3; i++) {
			await page.getByRole("link", { name: "PDPager" }).click();
			await expect(page.locator(".monaco-diff-editor")).toHaveCount(0);
			await page.getByRole("link", { name: "PDDiffPanel" }).click();
			await expect(page.locator(".monaco-diff-editor")).toBeVisible();
			await expect(page.locator(".monaco-editor.modified-in-monaco-diff-editor .lines-content > .view-lines")).toContainText("Hello");
		}

		expect(await count()).toBe(mounted);
		await page.getByRole("link", { name: "PDPager" }).click();
		await expect(page.locator(".monaco-diff-editor")).toHaveCount(0);
		expect(await count()).toBe(mounted - 3);
	});
});
