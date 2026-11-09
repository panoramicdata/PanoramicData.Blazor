import { test, expect as baseExpect, type Page } from "@playwright/test";

// A cold server is slow to start Blazor, and the demo reply takes about ten seconds.
const expect = baseExpect.configure({ timeout: 15000 });

// The demo chat service is one object shared by every page load, so these tests take turns.
test.describe.configure({ mode: "serial" });

async function openChat(page: Page) {
	await page.goto("/pdchat");
	const toggle = page.locator(".pdchat-toggle-collapsed").first();
	const textarea = page.locator(".chat-input-container textarea");
	// The button is prerendered before Blazor is interactive, so a first click can be lost.
	await expect(async () => {
		if (!(await textarea.isVisible())) {
			await toggle.click({ timeout: 2000 });
		}

		await expect(textarea).toBeVisible({ timeout: 5000 });
	}).toPass({ timeout: 30000 });
}

function progressModePicker(page: Page) {
	return page.locator("select", {
		has: page.locator("option", { hasText: "Ticker, the latest step" }),
	});
}

test.describe("PDChat: progress ticker", () => {
	test("shows one step at a time, rolls the last one out, counts the time and gives way to the answer", async ({
		page,
	}) => {
		// Opening the chat can take half a minute on a cold server, and the demo reply takes ten seconds.
		test.setTimeout(60000);
		await openChat(page);
		await progressModePicker(page).selectOption({ label: "Ticker, the latest step" });
		try {
			// Not exact: the button's name starts with its icon's glyph.
			await page.getByRole("button", { name: "Progress Steps" }).click();
			await expect(page.locator(".pdchat-progress-all summary").last()).toHaveText("3 steps so far");

			// The browser's DOM, not just the render tree: a step that rolled out must not stay on screen. Read in
			// one go, because a new step arrives every 1.4 seconds.
			await expect
				.poll(() =>
					page.evaluate(() => {
						const steps = [...document.querySelectorAll(".pdchat-progress-ticker-step")];
						const current = steps.filter((step) => !step.classList.contains("leaving"));
						return `${steps.length} shown, ${current.length} current`;
					}),
				)
				.toBe("2 shown, 1 current");
			await expect
				.poll(() =>
					page.evaluate(() => {
						const current = document.querySelector(".pdchat-progress-ticker-step:not(.leaving)");
						const listed = [...document.querySelectorAll(".pdchat-progress-all li")].map((li) => li.textContent);
						return current?.textContent === listed.at(-1);
					}),
				)
				.toBe(true);
			await expect(page.locator(".pdchat-progress-ticker-step.leaving")).toHaveCSS("opacity", "0");

			// The clock's counter is a registered integer that the browser advances.
			const elapsed = page.locator(".pdchat-progress-elapsed");
			const seconds = () =>
				elapsed.evaluate((element) =>
					getComputedStyle(element).getPropertyValue("--pdchat-progress-seconds").trim(),
				);
			const first = Number(await seconds());
			expect(Number.isInteger(first)).toBe(true);
			await expect.poll(async () => Number(await seconds())).toBeGreaterThan(first);

			await expect(page.locator(".pdchat-text").last()).toContainText("Your Weekly Health schedule ran at 06:00");
			await expect(page.locator(".pdchat-progress-ticker")).toHaveCount(0);
		} finally {
			await progressModePicker(page).selectOption({ label: "List, every step" });
		}
	});
});
