// PDMediaPlayer: zooming, panning and overview dragging of a visualisation, and drawing it.

import {
	drawBackground,
	drawFrequencyAxis,
	drawOverlays,
	drawTimeAxis,
} from "./pdmediaplayer-drawing.js";
import { scales } from "./pdmediaplayer-axes.js";
import { applyFollow } from "./pdmediaplayer-follow.js";
import {
	clampNumber,
	fromPixel,
	fullMaxHz,
	layout,
	liveEnd,
	minFrequencySpan,
	nowSeconds,
	plotSeconds,
	tinyAmount,
} from "./pdmediaplayer-layout.js";

const minTimeSpan = 0.5;
const wheelZoomPerPixel = 0.002;
const maxViewZoom = 5000;

// the longest time span worth showing: everything there is, or at least the sliding window
function maxTimeSpan(v) {
	const live = v.live && v.live.count > 0 ? v.live : null;
	const history = live
		? Math.min(liveEnd(live), live.capacity * live.columnSeconds)
		: plotSeconds(v);
	return Math.max(history || 1, v.options.windowSeconds || 0, minTimeSpan);
}

// remembers a time view, or forgets it when it is the default view anyway
function setTimeView(v, start, span) {
	const limit = maxTimeSpan(v);
	const clamped = clampNumber(span, minTimeSpan, limit);
	v.view.time =
		v.options.windowSeconds > 0
			? windowedView(v, start, clamped, limit)
			: wholeView(start, clamped, limit);
}

function wholeView(start, span, limit) {
	if (span >= limit - tinyAmount) {
		return null;
	}
	return { span, start: clampNumber(start, 0, limit - span) };
}

// for a sliding window the view is a span and how far back from the present it ends
function windowedView(v, start, span, limit) {
	const now = nowSeconds(v);
	const live = v.live && v.live.count > 0;
	// a live feed has no future to look at, but a recording has: the view may end as late as the end of the media
	const earliestBack = live ? 0 : now - limit;
	const latestBack = Math.min(limit - span, now - span);
	const back = clampNumber(now - (start + span), earliestBack, latestBack);
	const isDefault =
		Math.abs(back) < tinyAmount &&
		Math.abs(span - v.options.windowSeconds) < tinyAmount;
	return isDefault ? null : { span, back };
}

function setFrequencyView(v, low, span) {
	const full = fullMaxHz(v);
	const clamped = clampNumber(span, minFrequencySpan, full);
	const from = clampNumber(low, 0, full - clamped);
	v.view.freq =
		clamped >= full - tinyAmount ? null : { low: from, high: from + clamped };
}

// zooms the time axis by a factor (below 1 zooms in), keeping the time at anchor where it is on the screen
export function zoomTime(v, factor, anchor) {
	const l = layout(v);
	const next = clampNumber(l.seconds * factor, minTimeSpan, maxTimeSpan(v));
	const ratio = (anchor - l.start) / l.seconds;
	setTimeView(v, anchor - ratio * next, next);
}

export function panTime(v, fraction) {
	const l = layout(v);
	setTimeView(v, l.start + fraction * l.seconds, l.seconds);
}

export function zoomFrequency(v, factor, anchor) {
	const l = layout(v);
	const span = l.maxHz - l.minHz;
	const next = clampNumber(span * factor, minFrequencySpan, fullMaxHz(v));
	setFrequencyView(v, anchor - ((anchor - l.minHz) / span) * next, next);
}

export function panFrequency(v, fraction) {
	const l = layout(v);
	const span = l.maxHz - l.minHz;
	setFrequencyView(v, l.minHz + fraction * span, span);
}

// Wheel with a modifier key: Ctrl zooms time, Alt zooms frequency, Shift pans time, Shift and Alt pan frequency. Without a
// modifier the wheel scrolls the page as usual.
function wheelMode(e) {
	if (e.shiftKey && e.altKey) {
		return "panFrequency";
	}
	if (e.shiftKey) {
		return "panTime";
	}
	if (e.ctrlKey || e.metaKey) {
		return "zoomTime";
	}
	return e.altKey ? "zoomFrequency" : null;
}

export function onWheel(v, e) {
	const mode = wheelMode(e);
	if (!mode || !v.options.wheelZoom) {
		return;
	}
	e.preventDefault();
	v.following = false;
	const delta = clampNumber(e.deltaY || e.deltaX, -200, 200);
	const l = layout(v);
	const at = fromPixel(v, e) || {
		seconds: l.start + l.seconds / 2,
		hz: (l.minHz + l.maxHz) / 2,
	};
	const zoom = Math.exp(delta * wheelZoomPerPixel);
	const pan = (delta / 100) * 0.15;
	const actions = {
		zoomTime: () => zoomTime(v, zoom, at.seconds),
		zoomFrequency: () => zoomFrequency(v, zoom, at.hz),
		panTime: () => panTime(v, pan),
		panFrequency: () => panFrequency(v, -pan),
	};
	actions[mode]();
	draw(v);
}

// ---- publishing the view, and the overview ----

// the length of everything there is to see: the live data so far, or the media
function totalSeconds(v) {
	if (v.live && v.live.count > 0) {
		return liveEnd(v.live);
	}
	const duration = v.el.duration;
	return Number.isFinite(duration) && duration > 0 ? duration : plotSeconds(v);
}

// Publishes the view as CSS variables on the player: the zoom and offset that the seek bar and piano roll use to follow it, and
// the fractions that place the highlight and the playhead on the overview
function publishView(v, l) {
	const total = totalSeconds(v) || l.seconds;
	const left = clampNumber(l.start / total, 0, 1);
	const values = {
		"--pdmp-zoom": clampNumber(total / l.seconds, 1, maxViewZoom),
		"--pdmp-offset": l.start / total,
		"--pdmp-view-left": left,
		"--pdmp-view-width": clampNumber(l.seconds / total, 0, 1 - left),
		"--pdmp-playhead": clampNumber(nowSeconds(v) / total, 0, 1),
	};
	const key = JSON.stringify(values);
	if (!v.root || key === v.published) {
		return;
	}
	v.published = key;
	for (const [name, value] of Object.entries(values)) {
		v.root.style.setProperty(name, String(value));
	}
}

function overviewOf(e) {
	return e.target instanceof Element
		? e.target.closest(".pdmp-overview")
		: null;
}

// what a press on the overview does: resize from an edge, pan from the highlight, or move the view to where it was pressed
function dragMode(e) {
	if (e.target.classList.contains("pdmp-overview-handle-start")) {
		return "start";
	}
	if (e.target.classList.contains("pdmp-overview-handle-end")) {
		return "end";
	}
	return e.target.closest(".pdmp-overview-window") ? "pan" : "jump";
}

export function overviewDown(v, e) {
	const overview = overviewOf(e);
	if (!overview || e.button !== 0) {
		return;
	}
	e.preventDefault();
	const rect = overview.getBoundingClientRect();
	const l = layout(v);
	const total = totalSeconds(v) || l.seconds;
	const drag = {
		mode: dragMode(e),
		x0: e.clientX,
		start: l.start,
		span: l.seconds,
		total,
		width: rect.width || 1,
	};
	if (drag.mode === "jump") {
		drag.start = ((e.clientX - rect.left) / drag.width) * total - l.seconds / 2;
		drag.mode = "pan";
		setTimeView(v, drag.start, drag.span);
	}
	v.following = false;
	v.drag = drag;
	overview.setPointerCapture(e.pointerId);
	draw(v);
}

export function overviewMove(v, e) {
	const drag = v.drag;
	if (!drag) {
		return;
	}
	const dt = ((e.clientX - drag.x0) / drag.width) * drag.total;
	if (drag.mode === "start") {
		const end = drag.start + drag.span;
		const start = clampNumber(drag.start + dt, 0, end - minTimeSpan);
		setTimeView(v, start, end - start);
	} else if (drag.mode === "end") {
		setTimeView(
			v,
			drag.start,
			clampNumber(drag.span + dt, minTimeSpan, drag.total - drag.start),
		);
	} else {
		setTimeView(v, drag.start + dt, drag.span);
	}
	draw(v);
}

// the wheel on the overview zooms about the pointer, or with Shift pans
export function overviewWheel(v, e) {
	const overview = overviewOf(e);
	if (!overview || !v.options.wheelZoom) {
		return;
	}
	e.preventDefault();
	v.following = false;
	const rect = overview.getBoundingClientRect();
	const total = totalSeconds(v) || 1;
	const delta = clampNumber(e.deltaY || e.deltaX, -200, 200);
	if (e.shiftKey) {
		panTime(v, (delta / 100) * 0.15);
	} else {
		zoomTime(
			v,
			Math.exp(delta * wheelZoomPerPixel),
			((e.clientX - rect.left) / (rect.width || 1)) * total,
		);
	}
	draw(v);
}

export function draw(v) {
	if (!v.dotNetRef) {
		return;
	}
	applyFollow(v);
	const l = layout(v);
	publishView(v, l);
	const ctx = v.canvas.getContext("2d");
	const sc = scales(l);
	drawBackground(ctx, v, l);
	drawTimeAxis(ctx, v, l, sc);
	if (v.options.frequencyLabels) {
		drawFrequencyAxis(ctx, l, sc);
	}
	drawOverlays(ctx, v, l, sc);
}
