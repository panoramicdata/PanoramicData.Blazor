import { test, expect as baseExpect, type Locator } from "@playwright/test";

// A cold server is slow to start Blazor.
const expect = baseExpect.configure({ timeout: 15000 });

// The text lines an element's text takes up, from its content height and line height.
async function textLines(element: Locator) {
	return element.evaluate((node) => {
		const styles = getComputedStyle(node);
		const content = node.clientHeight - parseFloat(styles.paddingTop) - parseFloat(styles.paddingBottom);
		return Math.round(content / parseFloat(styles.lineHeight));
	});
}

test.describe("PDChat: author name in the full-width layout", () => {
	test("a short name with an author image sits on one line, with the image above it", async ({ page }) => {
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

		try {
			const word = `hello${Date.now()}`;
			await message.fill(word);
			await page.locator(".chat-send").click();

			const reply = page.locator(".pdchat-message.full-width.bot").filter({ hasText: word }).last();
			const name = reply.locator(".pdchat-username");
			const image = reply.locator(".pdchat-author-image");
			await expect(name).toHaveText("DumbBot");
			await expect(image).toBeVisible();

			expect(await textLines(name)).toBe(1);
			const imageBox = await image.boundingBox();
			const nameBox = await name.boundingBox();
			expect(imageBox!.y + imageBox!.height).toBeLessThanOrEqual(nameBox!.y + 1);
		} finally {
			// Leaves the shared chat minimized, as the other chat tests expect to find it.
			await page.getByRole("button", { name: "Minimize", exact: true }).click();
		}
	});
});
