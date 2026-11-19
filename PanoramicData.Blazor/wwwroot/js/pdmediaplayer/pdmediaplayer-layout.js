// PDMediaPlayer: where the visualisation sits on its canvas, which part of the time and frequency range shows, and the live spectrogram ring.

import { heatColour } from "./pdmediaplayer-spectrogram.js";

const maxLiveColumns = 16384;
export const minFrequencySpan = 100;
export const tinyAmount = Math.pow(10, -9);

export function clampNumber(value, low, high) {
	return Math.min(Math.max(value, low), Math.max(low, high));
}

// ---- the live spectrogram: a ring of columns held on an off-screen canvas, one pixel wide each ----

export function liveCapacity(options, columnSeconds) {
	const window = options.windowSeconds;
	const span =
		window > 0
			? Math.min(options.maxDuration, window * 10)
			: options.maxDuration;
	return Math.min(maxLiveColumns, Math.ceil(span / columnSeconds) + 2);
}

export function liveFits(live, options, bins, columnSeconds) {
	return (
		live &&
		live.bins === bins &&
		live.columnSeconds === columnSeconds &&
		live.capacity === liveCapacity(options, columnSeconds)
	);
}

export function createLive(options, bins, columnSeconds) {
	const capacity = liveCapacity(options, columnSeconds);
	const canvas = document.createElement("canvas");
	canvas.width = capacity;
	canvas.height = bins;
	const result = {
		bins,
		columnSeconds,
		capacity,
		maxFrequency: options.maxFrequency,
		canvas,
		context: canvas.getContext("2d"),
		count: 0,
	};
	return result;
}

export function liveEnd(live) {
	return live.count * live.columnSeconds;
}

export function writeColumn(live, levels, offset) {
	const image = live.context.createImageData(1, live.bins);
	for (let b = 0; b < live.bins; b++) {
		const [red, green, blue] = heatColour(levels[offset + b] / 255);
		image.data.set([red, green, blue, 255], (live.bins - 1 - b) * 4);
	}
	live.context.putImageData(image, live.count % live.capacity, 0);
	live.count++;
}

// ---- layout and axes ----

function dataSeconds(v) {
	let seconds = 0;
	for (let i = 0; i < v.keyPoints.length; i += 3) {
		seconds = Math.max(seconds, v.keyPoints[i] + 0.5);
	}
	for (let i = 0; i < v.landmarks.length; i += 4) {
		seconds = Math.max(seconds, v.landmarks[i], v.landmarks[i + 2]);
	}
	return seconds;
}

export function plotSeconds(v) {
	if (v.live && v.live.count > 0) {
		return liveEnd(v.live);
	}
	if (v.spectrogram) {
		return v.spectrogram.seconds;
	}
	const duration = v.el.duration;
	return Number.isFinite(duration) && duration > 0 ? duration : dataSeconds(v);
}

// the time the plot ends at: the end of the live data, or the playhead
export function nowSeconds(v) {
	return v.live && v.live.count > 0 ? liveEnd(v.live) : v.el.currentTime || 0;
}

// the first time shown and how many seconds are shown: all of it, or a window that slides to end at the present
// the first time shown and how many seconds are shown: all of it, or a window that slides to end at the present, either of
// which the user may have zoomed and panned
export function timeRange(v) {
	const windowSeconds = v.options.windowSeconds;
	const view = v.view.time;
	let start = 0;
	let seconds = 1;
	if (windowSeconds > 0) {
		seconds = view ? view.span : windowSeconds;
		start = nowSeconds(v) - (view ? view.back : 0) - seconds;
	} else if (view) {
		start = view.start;
		seconds = view.span;
	} else {
		seconds = plotSeconds(v) || 1;
	}
	const range = { start, seconds };
	return range;
}

export function fullMaxHz(v) {
	return v.spectrogram ? v.spectrogram.maxHz : v.options.maxFrequency;
}

function frequencyRange(v) {
	const full = fullMaxHz(v);
	const view = v.view.freq;
	if (!view) {
		return { minHz: 0, maxHz: full };
	}
	const span = clampNumber(view.high - view.low, minFrequencySpan, full);
	const low = clampNumber(view.low, 0, full - span);
	return { minHz: low, maxHz: low + span };
}

function sizeCanvas(v) {
	const dpr = window.devicePixelRatio || 1;
	const width = v.canvas.clientWidth || 600;
	const height = v.canvas.clientHeight || 200;
	v.canvas.width = Math.round(width * dpr);
	v.canvas.height = Math.round(height * dpr);
	return { dpr, width, height };
}

export function layout(v) {
	const dpr = window.devicePixelRatio || 1;
	const width = v.canvas.clientWidth || 600;
	const height = v.canvas.clientHeight || 200;
	if (
		v.canvas.width !== Math.round(width * dpr) ||
		v.canvas.height !== Math.round(height * dpr)
	) {
		sizeCanvas(v);
	}
	const top = 8;
	const bottom = 22;
	const result = {
		dpr,
		width,
		height,
		left: 0,
		top,
		pw: width,
		ph: height - top - bottom,
		...timeRange(v),
		...frequencyRange(v),
	};
	return result;
}

export function fromPixel(v, e) {
	const l = layout(v);
	const rect = v.canvas.getBoundingClientRect();
	const x = e.clientX - rect.left - l.left;
	const y = e.clientY - rect.top - l.top;
	if (x < 0 || y < 0 || x > l.pw || y > l.ph) {
		return null;
	}
	const result = {
		seconds: l.start + (x / l.pw) * l.seconds,
		hz: l.minHz + (1 - y / l.ph) * (l.maxHz - l.minHz),
	};
	return result;
}
