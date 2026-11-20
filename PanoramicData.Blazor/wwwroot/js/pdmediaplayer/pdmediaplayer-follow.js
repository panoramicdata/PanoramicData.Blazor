// PDMediaPlayer: keeping the view on the playhead of a live or playing visualisation.

import { nowSeconds, timeRange } from "./pdmediaplayer-layout.js";

// ---- following the playhead ----

// the media's own length, when there is media (a live feed has no end to stop at)
function mediaEnd(v) {
	return v.live && v.live.count > 0
		? Infinity
		: v.spectrogram
			? v.spectrogram.seconds
			: v.el.duration;
}

// moves the time view, if the options ask for it and the user has not taken over, so the playhead is where it should be
export function applyFollow(v) {
	const mode = v.options.followMode;
	if (mode === "free" || !v.following) {
		return;
	}
	const range = timeRange(v);
	const now = nowSeconds(v);
	const start =
		mode === "centre" ? centredStart(range, now) : marginStart(v, range, now);
	if (!v.view.time && start === range.start) {
		return;
	}
	storeFollowedStart(v, start, range.seconds);
}

function centredStart(range, now) {
	return now - range.seconds / 2;
}

// Margin mode: the view stays put until the playhead is closer than the margin to an edge, then moves (continuously or a page
// at a time) to put it back at the margin
function marginStart(v, range, now) {
	const margin = (v.options.followMarginPercent / 100) * range.seconds;
	const high = range.start + range.seconds - margin;
	const low = range.start + margin;
	if (now > high) {
		return v.options.followScroll === "pageturn"
			? now - margin
			: now - range.seconds + margin;
	}
	if (now < low) {
		return v.options.followScroll === "pageturn"
			? now - range.seconds + margin
			: now - margin;
	}
	return range.start;
}

// remembers the followed view; near the end the view stops scrolling, if asked to, and a view of a whole recording never
// scrolls to before its start in Margin mode
function storeFollowedStart(v, wanted, span) {
	const end = mediaEnd(v);
	let start = wanted;
	if (v.options.followStopsAtEnd && Number.isFinite(end)) {
		start = Math.min(start, Math.max(end - span, 0));
	}
	if (v.options.followMode === "margin") {
		start = Math.max(start, 0);
	}
	v.view.time =
		v.options.windowSeconds > 0
			? { span, back: nowSeconds(v) - (start + span) }
			: { span, start };
}
