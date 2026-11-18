import { test, expect as baseExpect, type Page } from "@playwright/test";

// A cold server is slow to start Blazor.
const expect = baseExpect.configure({ timeout: 15000 });

// The demo chat service is one object shared by every page load, so these tests take turns.
test.describe.configure({ mode: "serial" });

// A fake microphone, allowed without a prompt, so the simulated Voice Mode can start.
test.use({
	launchOptions: {
		args: ["--use-fake-ui-for-media-stream", "--use-fake-device-for-media-stream"],
	},
});

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

const picker = (page: Page) => page.locator(".pdchat-voice-mode-picker select");
const earButton = (page: Page) => page.locator(".pdchat-voice-toggle");

test.describe("PDChat: voice controls", () => {
	test.afterEach(async ({ page }) => {
		// Leave the shared demo with the microphone off.
		if ((await picker(page).count()) > 0 && (await picker(page).inputValue()) !== "MicOff") {
			await picker(page).selectOption("MicOff");
		}
	});

	test("the picker shows the mode when the ear button resumes listening after Mic off, and Mic off after a reload", async ({
		page,
	}) => {
		await openChat(page);
		await picker(page).selectOption("Hal");
		await expect(earButton(page)).toHaveAttribute("aria-pressed", "true");
		await picker(page).selectOption("MicOff");
		await expect(earButton(page)).toHaveAttribute("aria-pressed", "false");

		await earButton(page).click();

		await expect(earButton(page)).toHaveAttribute("aria-pressed", "true");
		await expect(picker(page)).toHaveValue("Hal");

		await openChat(page);
		await expect(picker(page)).toHaveValue("MicOff");
	});

	test("Alt+M in the chat cycles the voice choices, and does nothing outside the chat", async ({ page }) => {
		await openChat(page);
		await expect(picker(page)).toHaveValue("MicOff");

		await page.locator(".chat-input-container textarea").focus();
		await page.keyboard.press("Alt+KeyM");
		await expect(picker(page)).toHaveValue("Hal");
		await page.keyboard.press("Alt+KeyM");
		await expect(picker(page)).toHaveValue("Dictate");

		await page.locator('input[placeholder="Enter chat title"]').focus();
		await page.keyboard.press("Alt+KeyM");
		await expect(picker(page)).toHaveValue("Dictate");
	});

	test("the ear button carries a Mic off badge only while the microphone is off", async ({ page }) => {
		await openChat(page);
		await expect(earButton(page).locator(".pdchat-voice-off-badge")).toBeVisible();

		await picker(page).selectOption("Hal");

		await expect(earButton(page)).toHaveAttribute("aria-pressed", "true");
		await expect(earButton(page).locator(".pdchat-voice-off-badge")).toHaveCount(0);
	});
});
