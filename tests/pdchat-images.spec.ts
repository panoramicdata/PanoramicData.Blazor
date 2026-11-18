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

// Pastes a PNG drawn in the page, as solid colour or as noise that PNG cannot compress; returns its size in bytes.
async function pasteDrawnPng(
	page: Page,
	width: number,
	height: number,
	noise: boolean,
) {
	const textarea = page.locator(
		".chat-input-container textarea[data-images-ready]",
	);
	return textarea.evaluate(
		async (element, [w, h, n]) => {
			const canvas = document.createElement("canvas");
			canvas.width = w;
			canvas.height = h;
			const context = canvas.getContext("2d")!;
			if (n) {
				// getRandomValues fills at most 64 KB a call; alpha stays opaque.
				const pixels = context.createImageData(w, h);
				for (let i = 0; i < pixels.data.length; i += 65536) {
					crypto.getRandomValues(pixels.data.subarray(i, i + 65536));
				}

				for (let i = 3; i < pixels.data.length; i += 4) {
					pixels.data[i] = 255;
				}

				context.putImageData(pixels, 0, 0);
			} else {
				context.fillStyle = "#3a7bd5";
				context.fillRect(0, 0, w, h);
			}

			const blob = await (await fetch(canvas.toDataURL("image/png"))).blob();
			const data = new DataTransfer();
			data.items.add(new File([blob], "drawn.png", { type: "image/png" }));
			element.dispatchEvent(
				new ClipboardEvent("paste", {
					clipboardData: data,
					bubbles: true,
					cancelable: true,
				}),
			);
			return blob.size;
		},
		[width, height, noise] as const,
	);
}

// What the chat holds for the first attached image: its type, pixel size and size in bytes.
async function firstAttachedImage(page: Page) {
	const image = page.locator(".pdchat-image-thumb img").first();
	await expect(image).toHaveCount(1);
	return image.evaluate(async (img: HTMLImageElement) => {
		await img.decode();
		const [header, base64] = img.src.split(",");
		return {
			type: header.slice("data:".length, header.indexOf(";")),
			width: img.naturalWidth,
			height: img.naturalHeight,
			bytes: atob(base64).length,
		};
	});
}

test.describe("PDChat: images are shrunk before they are sent", () => {
	test("an image larger than 1600 pixels is scaled down to 1600 on its long side and sent as a JPEG", async ({
		page,
	}) => {
		await openChat(page);

		await pasteDrawnPng(page, 4000, 3000, false);

		const image = await firstAttachedImage(page);
		expect(image.type).toBe("image/jpeg");
		expect(image.width).toBe(1600);
		expect(image.height).toBe(1200);
	});

	test("an image over 1.5 MB is compressed as a JPEG at its own size", async ({
		page,
	}) => {
		await openChat(page);

		const original = await pasteDrawnPng(page, 1200, 1000, true);
		expect(original).toBeGreaterThan(1.5 * 1024 * 1024);

		const image = await firstAttachedImage(page);
		expect(image.type).toBe("image/jpeg");
		expect(image.width).toBe(1200);
		expect(image.height).toBe(1000);
		expect(image.bytes).toBeLessThan(original);
	});

	test("a small image is sent as it is", async ({ page }) => {
		await openChat(page);

		const original = await pasteDrawnPng(page, 800, 600, false);

		const image = await firstAttachedImage(page);
		expect(image.type).toBe("image/png");
		expect(image.width).toBe(800);
		expect(image.height).toBe(600);
		expect(image.bytes).toBe(original);
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
