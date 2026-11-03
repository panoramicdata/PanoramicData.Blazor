import { test, expect as baseExpect, type Locator, type Page } from "@playwright/test";

// A cold server is slow to start Blazor.
const expect = baseExpect.configure({ timeout: 15000 });

// The demo chat service is one object shared by every page load, so these tests take turns.
test.describe.configure({ mode: "serial" });

const summaryDraft =
	"I like several flavours, pistachio most of all, and I would happily eat more today.";

async function openForm(page: Page): Promise<Locator> {
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

	const forms = page.locator(".pdchat-form");
	const before = await forms.count();
	await message.fill("Show me the form");
	await page.locator(".chat-send").click();
	await expect(forms).toHaveCount(before + 1);

	return forms.last();
}

// Leaves the shared chat minimized, as the other chat tests expect to find it.
async function leave(page: Page, form: Locator) {
	await form.locator(".pdchat-form-dismiss").click();
	await page.getByRole("button", { name: "Minimize", exact: true }).click();
	await expect(page.locator(".pdchat-toggle-collapsed").first()).toBeVisible();
}

// An answered tab's name ends in a dot, so a tab is found by its header text.
async function showTab(form: Locator, header: string, question: string) {
	await form.getByRole("tab").filter({ hasText: header }).click();
	await expect(form.locator(".pdchat-form-question")).toHaveText(question);
}

// The text lines a button's label takes up, from its content height and line height.
async function labelLines(button: Locator) {
	return button.evaluate((element) => {
		const styles = getComputedStyle(element);
		const content = element.clientHeight - parseFloat(styles.paddingTop) - parseFloat(styles.paddingBottom);
		return Math.round(content / parseFloat(styles.lineHeight));
	});
}

test.describe("PDFormMessage: buttons", () => {
	test("Next and Submit answers each fit their label on one line", async ({ page }) => {
		const form = await openForm(page);
		try {
			const button = form.locator(".pdchat-form-submit");
			await expect(button).toHaveText("Next");
			expect(await labelLines(button)).toBe(1);

			await showTab(form, "Summary", "Here is a suggested summary - please edit it.");
			await expect(button).toHaveText("Submit answers");
			expect(await labelLines(button)).toBe(1);
		} finally {
			await leave(page, form);
		}
	});
});

test.describe("PDFormMessage: two multi-line text questions in a row", () => {
	test("after Next, Summary shows its own draft, not the text typed on Why", async ({ page }) => {
		const form = await openForm(page);
		try {
			await showTab(form, "Why", "Why that shop?");
			const box = form.locator(".pdchat-form-textarea");
			await expect(box).toHaveValue("");
			await box.fill("It is near the beach");

			await form.getByRole("button", { name: "Next", exact: true }).click();
			await expect(form.locator(".pdchat-form-question")).toHaveText("Here is a suggested summary - please edit it.");

			await expect(box).toHaveValue(summaryDraft);
		} finally {
			await leave(page, form);
		}
	});

	test("going back, Why is empty, not showing the text typed on Summary", async ({ page }) => {
		const form = await openForm(page);
		try {
			await showTab(form, "Summary", "Here is a suggested summary - please edit it.");
			const box = form.locator(".pdchat-form-textarea");
			await expect(box).toHaveValue(summaryDraft);
			await box.fill("Pistachio, always");

			await showTab(form, "Why", "Why that shop?");

			await expect(box).toHaveValue("");
		} finally {
			await leave(page, form);
		}
	});

	test("each keeps its own text, moving forwards, backwards and by clicking a tab", async ({ page }) => {
		const form = await openForm(page);
		try {
			const box = form.locator(".pdchat-form-textarea");
			await showTab(form, "Why", "Why that shop?");
			await box.fill("It is near the beach");
			await form.getByRole("button", { name: "Next", exact: true }).click();
			await expect(box).toHaveValue(summaryDraft);
			await box.fill("Pistachio, always");

			await showTab(form, "Why", "Why that shop?");
			await expect(box).toHaveValue("It is near the beach");

			await form.getByRole("button", { name: "Next", exact: true }).click();
			await expect(form.locator(".pdchat-form-question")).toHaveText("Here is a suggested summary - please edit it.");
			await expect(box).toHaveValue("Pistachio, always");

			await showTab(form, "Shop", "Which shop do you buy it from?");
			await showTab(form, "Why", "Why that shop?");
			await expect(box).toHaveValue("It is near the beach");
		} finally {
			await leave(page, form);
		}
	});
});
