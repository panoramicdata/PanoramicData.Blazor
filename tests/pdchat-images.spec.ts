import { test, expect as baseExpect, type Page } from "@playwright/test";

// A cold server is slow to start Blazor and to decode the first image.
const expect = baseExpect.configure({ timeout: 15000 });

// The demo chat service is one object shared by every page load, so these tests take turns.
test.describe.configure({ mode: "serial" });

// Lets the screenshot button capture this tab without the permission prompt, which a test cannot answer.
test.use({
	launchOptions: {
		args: ["--auto-accept-this-tab-capture", "--use-fake-ui-for-media-stream"],
	},
});

const pngBase64 =
	"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

async function openChat(page: Page) {
	await page.goto("/pdchat");
	const toggle = page.locator(".pdchat-toggle-collapsed").first();
	const textarea = page.locator(
		".chat-input-container textarea[data-images-ready]",
	);
	// The button is prerendered before Blazor is interactive, so a first click can be lost.
	await expect(async () => {
		if (!(await textarea.isVisible())) {
			await toggle.click({ timeout: 2000 });
		}

		await expect(textarea).toBeVisible({ timeout: 5000 });
	}).toPass({ timeout: 30000 });
}

test.describe("PDChat: Mic off, model label and images", () => {
	test("Mic off is the first listening choice and is selected while the microphone is off", async ({
		page,
	}) => {
		await openChat(page);

		const options = page.locator(".pdchat-voice-mode-picker option");
		await expect(options.first()).toHaveText("Mic off");
		await expect(page.locator(".pdchat-voice-mode-picker select")).toHaveValue(
			"MicOff",
		);
	});

	test("the answering model is labelled in the toolbar", async ({ page }) => {
		await openChat(page);

		await expect(page.locator(".pdchat-answering-model")).toHaveText(
			"Dumb Model",
		);
	});

	test("pasted, picked and dropped images stack, number, remove and send", async ({
		page,
	}) => {
		await openChat(page);
		const textarea = page.locator(
			".chat-input-container textarea[data-images-ready]",
		);
		const thumbs = page.locator(".pdchat-image-thumb");

		// Ctrl+V with an image on the clipboard.
		await textarea.evaluate((element, base64) => {
			const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
			const data = new DataTransfer();
			data.items.add(new File([bytes], "pasted.png", { type: "image/png" }));
			element.dispatchEvent(
				new ClipboardEvent("paste", {
					clipboardData: data,
					bubbles: true,
					cancelable: true,
				}),
			);
		}, pngBase64);
		await expect(thumbs).toHaveCount(1);

		// The attach button's file picker.
		await page.locator(".pdchat-image-file-input").setInputFiles({
			name: "picked.png",
			mimeType: "image/png",
			buffer: Buffer.from(pngBase64, "base64"),
		});
		await expect(thumbs).toHaveCount(2);

		// Dropping a file on the text box.
		await page.locator(".chat-input-container").evaluate((element, base64) => {
			const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
			const data = new DataTransfer();
			data.items.add(new File([bytes], "dropped.png", { type: "image/png" }));
			element.dispatchEvent(
				new DragEvent("drop", {
					dataTransfer: data,
					bubbles: true,
					cancelable: true,
				}),
			);
		}, pngBase64);
		await expect(thumbs).toHaveCount(3);
		await expect(page.locator(".pdchat-image-thumb figcaption")).toHaveText([
			"1",
			"2",
			"3",
		]);

		// The strip sits above the text box.
		const strip = await page.locator(".pdchat-image-strip").boundingBox();
		const box = await textarea.boundingBox();
		expect(strip!.y + strip!.height).toBeLessThanOrEqual(box!.y + 1);

		await page.locator(".pdchat-image-remove").first().click();
		await expect(thumbs).toHaveCount(2);

		await textarea.fill("What changed?");
		await page.locator(".chat-send").click();

		// The images are part of the user's turn, as wide as the message.
		// The demo service is shared by every page load, so look at the newest user message only.
		const message = page.locator(".pdchat-message.user").last();
		const sent = message.locator(".pdchat-message-image");
		await expect(sent).toHaveCount(2);
		await expect(thumbs).toHaveCount(0);
		const imageBox = await sent.first().boundingBox();
		const textBox = await message.locator(".pdchat-text").boundingBox();
		expect(imageBox!.width).toBeGreaterThan(textBox!.width * 0.9);
	});
});

test.describe("PDChat: files the chat cannot take", () => {
	test("picking more images than fit keeps the ones that fit and says why", async ({
		page,
	}) => {
		await openChat(page);
		const png = Buffer.from(pngBase64, "base64");

		await page.locator(".pdchat-image-file-input").setInputFiles(
			[1, 2, 3, 4, 5, 6].map((i) => ({
				name: `picked${i}.png`,
				mimeType: "image/png",
				buffer: png,
			})),
		);

		await expect(page.locator(".pdchat-image-thumb")).toHaveCount(5);
		await expect(page.locator(".pdchat-image-notice")).toHaveText(
			"A message can carry at most 5 images.",
		);
	});

	test("a picked or dropped file that is not an image is refused with a notice", async ({
		page,
	}) => {
		await openChat(page);

		await page.locator(".pdchat-image-file-input").setInputFiles({
			name: "notes.pdf",
			mimeType: "application/pdf",
			buffer: Buffer.from("%PDF-1.4"),
		});
		await expect(page.locator(".pdchat-image-thumb")).toHaveCount(0);
		await expect(page.locator(".pdchat-image-notice")).toHaveText(
			"Only images can be attached.",
		);

		// Left to the browser, a dropped file would open in the tab.
		const url = page.url();
		const handled = await page
			.locator(".chat-input-container")
			.evaluate((element) => {
				const data = new DataTransfer();
				data.items.add(
					new File(["%PDF-1.4"], "dropped.pdf", { type: "application/pdf" }),
				);
				const drop = new DragEvent("drop", {
					dataTransfer: data,
					bubbles: true,
					cancelable: true,
				});
				element.dispatchEvent(drop);
				return drop.defaultPrevented;
			});
		expect(handled).toBe(true);
		expect(page.url()).toBe(url);
	});
});

test.describe("PDChat: screenshot of this tab", () => {
	test("the screenshot button adds a numbered image of the tab", async ({
		page,
	}) => {
		await openChat(page);

		await page.locator(".pdchat-image-screenshot").click();

		await expect(page.locator(".pdchat-image-thumb")).toHaveCount(1, {
			timeout: 15000,
		});
		await expect(page.locator(".pdchat-image-notice")).toHaveCount(0);
	});
});
