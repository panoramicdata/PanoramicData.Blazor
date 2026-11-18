import { test, expect as baseExpect, type Page } from "@playwright/test";

// A cold server is slow to start Blazor.
const expect = baseExpect.configure({ timeout: 15000 });

// The demo chat service is one object shared by every page load, so these tests take turns.
test.describe.configure({ mode: "serial" });

const phoneWidths = [360, 390, 414];

async function openDockedToSide(page: Page, width: number) {
	await page.setViewportSize({ width, height: 844 });
	await page.goto("/pdchat");
	const toggle = page.locator(".pdchat-toggle-collapsed").first();
	const unpin = page.getByRole("button", { name: "Unpin from Side", exact: true });
	const dock = page.getByRole("button", { name: "Dock to Side", exact: true });

	// The button is prerendered before Blazor is interactive, so a first click can be lost.
	await expect(async () => {
		if (!(await unpin.isVisible()) && !(await dock.isVisible())) {
			await toggle.click({ timeout: 2000 });
		}

		await expect(unpin.or(dock)).toBeVisible({ timeout: 5000 });
	}).toPass({ timeout: 30000 });

	if (await dock.isVisible()) {
		await dock.click();
	}

	await expect(unpin).toBeAttached();
}

// Leaves the shared chat floating and minimized, as the other chat tests expect to find it.
async function leaveDockedToSide(page: Page) {
	await page.setViewportSize({ width: 1280, height: 844 });
	await page.getByRole("button", { name: "Unpin from Side", exact: true }).click();
	await page.getByRole("button", { name: "Minimize", exact: true }).click();
	await expect(page.locator(".pdchat-toggle-collapsed").first()).toBeVisible();
}

async function rightEdge(page: Page, name: string) {
	const box = await page.getByRole("button", { name, exact: true }).boundingBox();
	return box ? box.x + box.width : Number.NaN;
}

test.describe("PDChat: docked to the side", () => {
	for (const width of phoneWidths) {
		test(`at ${width}px, Minimize and every other header button are on the screen`, async ({ page }) => {
			await openDockedToSide(page, width);
			try {
				expect(await rightEdge(page, "Minimize")).toBeLessThanOrEqual(width);

				for (const button of await page.locator(".pdchat-header button:visible").all()) {
					const box = await button.boundingBox();
					expect(box!.x).toBeGreaterThanOrEqual(0);
					expect(box!.x + box!.width).toBeLessThanOrEqual(width);
				}
			} finally {
				await leaveDockedToSide(page);
			}
		});

		test(`at ${width}px, the chat covers the page, which keeps the whole width`, async ({ page }) => {
			await openDockedToSide(page, width);
			try {
				const chat = await page.locator(".pdchat-chat-panel").boundingBox();
				const content = await page.locator(".pdchat-content-panel").boundingBox();
				const message = await page.locator(".pdchat-container textarea").first().boundingBox();

				expect(chat!.width).toBe(width);
				expect(content!.width).toBe(width);
				expect(message!.width).toBeGreaterThan(width - 40);
			} finally {
				await leaveDockedToSide(page);
			}
		});
	}

	test("at 390px, Minimize brings back the bubble and the whole page", async ({ page }) => {
		await openDockedToSide(page, 390);
		await page.getByRole("button", { name: "Minimize", exact: true }).click();

		await expect(page.locator(".pdchat-toggle-collapsed").first()).toBeVisible();
		const content = await page.locator(".pdchat-content-panel").boundingBox();
		expect(content!.width).toBe(390);

		await openDockedToSide(page, 1280);
		await leaveDockedToSide(page);
	});

	test("at 1280px, the chat shares the screen with the page", async ({ page }) => {
		await openDockedToSide(page, 1280);
		try {
			const chat = await page.locator(".pdchat-chat-panel").boundingBox();
			const content = await page.locator(".pdchat-content-panel").boundingBox();

			expect(chat!.width).toBeGreaterThan(200);
			expect(content!.width).toBeGreaterThan(600);
			expect(chat!.x).toBeGreaterThanOrEqual(content!.x + content!.width);
			expect(await rightEdge(page, "Minimize")).toBeLessThanOrEqual(1280);
		} finally {
			await leaveDockedToSide(page);
		}
	});
});
