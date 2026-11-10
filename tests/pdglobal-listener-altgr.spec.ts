import { test, expect, type Page } from "@playwright/test";

type KeyEvent = [type: "keydown" | "keyup", init: KeyboardEventInit];

// Reads each key as an editor in an iframe hands it on, through a fresh copy of the module so no keys stay held between tests.
async function keyArgsFor(page: Page, events: KeyEvent[]) {
	return page.evaluate(async (keyEvents) => {
		const listener = await import(`/_content/PanoramicData.Blazor/PDGlobalListener.razor.js?fresh=${Math.random()}`);
		return keyEvents.map(([type, init]) => listener.getKeyArgs(new KeyboardEvent(type, init)));
	}, events);
}

const oAcute = "ó";
const ctrlDown: KeyEvent = ["keydown", { key: "Control", code: "ControlLeft", ctrlKey: true }];
const leftAltDown: KeyEvent = ["keydown", { key: "Alt", code: "AltLeft", ctrlKey: true, altKey: true }];
const rightAltDown: KeyEvent = ["keydown", { key: "AltGraph", code: "AltRight", modifierAltGraph: true }];
const oDown: KeyEvent = ["keydown", { key: oAcute, code: "KeyO", modifierAltGraph: true }];
const oUp: KeyEvent = ["keyup", { key: oAcute, code: "KeyO", modifierAltGraph: true }];

test.describe("PDGlobalListener: Ctrl and Alt that Windows reports as AltGr", () => {
	test.beforeEach(async ({ page }) => {
		await page.goto("/");
	});

	test("Ctrl and the left Alt with O, handed on from an editor, read as Ctrl+Alt+O", async ({ page }) => {
		const args = await keyArgsFor(page, [ctrlDown, leftAltDown, oDown, oUp]);

		expect(args[2]).toMatchObject({ key: oAcute, code: "KeyO", ctrlKey: true, altKey: true });
		expect(args[3]).toMatchObject({ code: "KeyO", ctrlKey: true, altKey: true });
	});

	test("the AltGr key itself still types the accented letter", async ({ page }) => {
		const args = await keyArgsFor(page, [ctrlDown, rightAltDown, oDown]);

		expect(args[2]).toMatchObject({ key: oAcute, ctrlKey: false, altKey: false });
	});

	test("once Ctrl and Alt are released, AltGr with O types again", async ({ page }) => {
		const args = await keyArgsFor(page, [
			ctrlDown,
			leftAltDown,
			["keyup", { key: "Control", code: "ControlLeft", altKey: true }],
			["keyup", { key: "Alt", code: "AltLeft" }],
			oDown,
		]);

		expect(args[4]).toMatchObject({ key: oAcute, ctrlKey: false, altKey: false });
	});
});
