import { test, expect as baseExpect, type Page } from "@playwright/test";

// A cold server is slow to start Blazor.
const expect = baseExpect.configure({ timeout: 15000 });

// The demo chat service is one object shared by every page load, so these tests take turns.
test.describe.configure({ mode: "serial" });

const botBubble = ".pdchat-message.bubble.bot.priority-normal";
const userBubble = ".pdchat-message.bubble.user";

async function openBubbles(page: Page) {
	await page.goto("/pdchat");
	const toggle = page.locator(".pdchat-toggle-collapsed").first();
	const message = page.locator(".chat-input-container textarea");

	// The button is prerendered before Blazor is interactive, so a first click can be lost.
	await expect(async () => {
		if (!(await message.isVisible())) {
			await toggle.click({ timeout: 2000 });
		}

		await expect(message).toBeVisible({ timeout: 5000 });
	}).toPass({ timeout: 30000 });

	await page.locator("#fullWidthCheck").uncheck();
	await page.getByRole("button", { name: "Welcome" }).click();
	await message.fill("A message from me");
	await page.locator(".chat-send").click();
	await expect(page.locator(botBubble)).not.toHaveCount(0);
	await expect(page.locator(userBubble)).not.toHaveCount(0);
}

// Leaves the shared chat full width and minimized, as the other chat tests expect to find it.
async function leave(page: Page) {
	await page.locator("#fullWidthCheck").check();
	await page.getByRole("button", { name: "Minimize", exact: true }).click();
	await expect(page.locator(".pdchat-toggle-collapsed").first()).toBeVisible();
}

async function useTheme(page: Page, theme: "light" | "dark") {
	await page.evaluate((value) => document.documentElement.setAttribute("data-bs-theme", value), theme);
}

async function background(page: Page, selector: string) {
	return page.locator(selector).last().evaluate((element) => getComputedStyle(element).backgroundColor);
}

// The WCAG contrast ratio between a message's text and its background.
async function contrast(page: Page, selector: string) {
	return page.locator(selector).last().evaluate((element) => {
		const luminance = (colour: string) => {
			const [r, g, b] = colour.match(/[\d.]+/g)!.slice(0, 3).map((channel) => {
				const value = Number(channel) / 255;
				return value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
			});
			return 0.2126 * r + 0.7152 * g + 0.0722 * b;
		};
		const text = luminance(getComputedStyle(element.querySelector(".pdchat-text")!).color);
		const surface = luminance(getComputedStyle(element).backgroundColor);
		return (Math.max(text, surface) + 0.05) / (Math.min(text, surface) + 0.05);
	});
}

test.describe("PDChat: bubble messages", () => {
	test("in a dark theme, the bot's bubbles and your own keep their text readable", async ({ page }) => {
		await openBubbles(page);
		try {
			await useTheme(page, "dark");

			expect(await contrast(page, botBubble)).toBeGreaterThanOrEqual(4.5);
			expect(await contrast(page, userBubble)).toBeGreaterThanOrEqual(4.5);
		} finally {
			await useTheme(page, "light");
			await leave(page);
		}
	});

	test("in a light theme, the bot's bubbles stay light blue and your own light green", async ({ page }) => {
		await openBubbles(page);
		try {
			await useTheme(page, "light");

			expect(await background(page, botBubble)).toBe("rgb(230, 240, 255)");
			expect(await background(page, userBubble)).toBe("rgb(214, 252, 214)");
			expect(await contrast(page, botBubble)).toBeGreaterThanOrEqual(4.5);
			expect(await contrast(page, userBubble)).toBeGreaterThanOrEqual(4.5);
		} finally {
			await leave(page);
		}
	});
});
