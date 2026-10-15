import { test, expect, type Locator, type Page } from "@playwright/test";

// PDMediaPlayer's script (PDMediaPlayer.razor.js) cannot run under bUnit, so these tests drive the demo page in a
// real browser and check what the media element itself does. The demo plays a one minute synthesised WAV with
// markers at 5, 12, 18 (dead air, 3 s), 30, 30.6, 35, 40 (dead air), 47 and 50 seconds, and a 2 second seek lead.

type MediaState = { currentTime: number; paused: boolean; rate: number; defaultRate: number; preservesPitch: boolean; muted: boolean };

const mediaState = (player: Locator, selector = "audio") =>
	player.locator(selector).evaluate((e: HTMLMediaElement) => ({
		currentTime: e.currentTime,
		paused: e.paused,
		rate: e.playbackRate,
		defaultRate: e.defaultPlaybackRate,
		preservesPitch: (e as HTMLMediaElement & { preservesPitch: boolean }).preservesPitch,
		muted: e.muted,
	})) as Promise<MediaState>;

// moves the seek slider as a drag does: input events while dragging, then change on release
const dragSliderTo = (player: Locator, seconds: number) =>
	player.locator("input.pdmp-seek").evaluate((e: HTMLInputElement, value) => {
		e.value = String(value);
		e.dispatchEvent(new Event("input", { bubbles: true }));
		e.dispatchEvent(new Event("change", { bubbles: true }));
	}, seconds);

const expectTimeNear = async (player: Locator, seconds: number) =>
	expect.poll(async () => (await mediaState(player)).currentTime).toBeCloseTo(seconds, 0);

async function openPlayer(page: Page): Promise<Locator> {
	await page.goto("/pdmediaplayer");
	const player = page.locator(".pd-media-player").first();
	await expect(player.locator(".pdmp-duration")).toHaveText("1:00", { timeout: 30_000 });
	return player;
}

test.describe("PDMediaPlayer", () => {
	test("plays, changes speed with pitch preserved, and seeks with the slider", async ({ page }) => {
		const player = await openPlayer(page);

		await player.locator("button.pdmp-play").click();
		await expect.poll(async () => (await mediaState(player)).currentTime).toBeGreaterThan(0.5);
		expect((await mediaState(player)).paused).toBe(false);
		await expect(player.locator("button.pdmp-play")).toHaveAttribute("aria-label", "Pause");

		await player.locator("select.pdmp-rate").selectOption("1.5");
		await expect.poll(async () => (await mediaState(player)).rate).toBe(1.5);
		const state = await mediaState(player);
		expect(state.defaultRate).toBe(1.5);
		expect(state.preservesPitch).toBe(true);

		await player.locator("button.pdmp-play").click();
		await dragSliderTo(player, 25);
		await expectTimeNear(player, 25);
	});

	test("jumps between markers with the buttons, the shortcuts and the list", async ({ page }) => {
		const player = await openPlayer(page);
		await dragSliderTo(player, 20);
		await expectTimeNear(player, 20);

		// next marker: the clipping at 30 s, less the 2 s lead
		await player.locator("button.pdmp-next-marker").click();
		await expectTimeNear(player, 28);

		await player.locator("input.pdmp-seek").focus();
		await page.keyboard.press("l");
		await expectTimeNear(player, 28.6);
		await page.keyboard.press("j");
		await expectTimeNear(player, 28);
		await page.keyboard.press("[");
		await expectTimeNear(player, 16);
		await expect(player.locator(".pdmp-current .pdmp-marker-label")).toHaveText("3 s of silence");

		await player.locator(".pdmp-marker-button", { hasText: "Weather" }).click();
		await expectTimeNear(player, 48);
	});

	test("hides a kind from the bar, the list and navigation", async ({ page }) => {
		const player = await openPlayer(page);
		await expect(player.locator(".pdmp-tick")).toHaveCount(9);

		await player.locator(".pdmp-kind-chip", { hasText: "Clipping" }).click();

		await expect(player.locator(".pdmp-tick")).toHaveCount(6);
		await expect(player.locator(".pdmp-marker-button")).toHaveCount(6);
		await dragSliderTo(player, 20);
		await expectTimeNear(player, 20);
		await player.locator("button.pdmp-next-marker").click();
		// the clipping at 30 s is skipped: the interview at 35 s, less the lead
		await expectTimeNear(player, 33);
	});

	test("shortcuts only work with focus inside the player", async ({ page }) => {
		const player = await openPlayer(page);

		await page.locator("h1").first().click();
		await page.keyboard.press("k");
		await page.waitForTimeout(500);
		expect((await mediaState(player)).paused).toBe(true);

		await player.locator("input.pdmp-seek").focus();
		await page.keyboard.press("k");
		await expect.poll(async () => (await mediaState(player)).paused).toBe(false);
		await page.keyboard.press("m");
		await expect.poll(async () => (await mediaState(player)).muted).toBe(true);
		await page.keyboard.press(">");
		await expect.poll(async () => (await mediaState(player)).rate).toBe(1.25);
	});

	test("navigating away detaches the script without errors", async ({ page }) => {
		const errors: string[] = [];
		page.on("pageerror", (e) => errors.push(e.message));
		const player = await openPlayer(page);
		await player.locator("button.pdmp-play").click();
		await expect.poll(async () => (await mediaState(player)).paused).toBe(false);

		await page.getByRole("link", { name: "Home" }).first().click();
		await expect(page.locator(".pd-media-player")).toHaveCount(0);
		await page.waitForTimeout(1000);

		expect(errors).toEqual([]);
	});
});
