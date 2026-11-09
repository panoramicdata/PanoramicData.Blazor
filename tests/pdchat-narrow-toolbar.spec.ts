import { test, expect as baseExpect, type Page } from "@playwright/test";

// A cold server is slow to start Blazor.
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

async function selectWidth(page: Page) {
	const box = await page.locator(".pdchat-voice-mode-picker select").boundingBox();
	return box ? box.width : Number.NaN;
}

test.describe("PDChat: voice mode picker width", () => {
	test("in a corner-docked chat the picker is its arrow alone and keeps its name and choices", async ({
		page,
	}) => {
		await openChat(page);
		const select = page.locator(".pdchat-voice-mode-picker select");

		expect(await selectWidth(page)).toBeLessThanOrEqual(20);
		await expect(select).toHaveAccessibleName("Voice mode");
		await expect(select.locator("option").first()).toHaveText("Mic off");
	});

	test("full screen, the picker is wide enough to show the mode's name", async ({ page }) => {
		await openChat(page);
		const fullscreen = page.getByRole("button", { name: "Fullscreen", exact: true });
		const restore = page.getByRole("button", { name: "Restore", exact: true });

		await fullscreen.click();
		try {
			await expect(restore).toBeVisible();
			expect(await selectWidth(page)).toBeGreaterThan(40);
		} finally {
			await restore.click();
			await expect(fullscreen).toBeVisible();
		}
	});
});
