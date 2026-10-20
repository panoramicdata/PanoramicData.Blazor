// PDMediaPlayer: forwards media element events to .NET and applies the commands .NET sends.
// Every decision (what to show, where to seek, how to describe an error) is made in C#; this file only
// reports what the element did and does what it is told.

const players = new WeakMap();
const immediateEvents = [
	"loadedmetadata",
	"durationchange",
	"play",
	"pause",
	"ended",
	"error",
	"seeked",
];
const throttledEvents = ["timeupdate", "progress"];

function snapshot(el) {
	const buffered = [];
	for (let i = 0; i < el.buffered.length; i++) {
		buffered.push(el.buffered.start(i), el.buffered.end(i));
	}
	return {
		currentTime: el.currentTime,
		duration: Number.isFinite(el.duration) ? el.duration : null,
		isInfinite: el.duration === Infinity,
		paused: el.paused,
		ended: el.ended,
		playbackRate: el.playbackRate,
		buffered,
		errorCode: el.error ? el.error.code : null,
		errorMessage: el.error ? el.error.message : null,
	};
}

export function attach(el, dotNetRef, intervalMs) {
	if (!el) {
		return;
	}
	detach(el);
	const player = { dotNetRef, intervalMs, last: 0, timer: null, listeners: [] };
	const send = (type) => {
		if (player.dotNetRef) {
			player.dotNetRef
				.invokeMethodAsync("OnMediaEvent", type, snapshot(el))
				.catch(() => {});
		}
	};
	// timeupdate and progress are sent at most once per interval, with a trailing send so the last position arrives
	const sendThrottled = (type) => {
		const wait = player.intervalMs - (Date.now() - player.last);
		if (wait <= 0) {
			player.last = Date.now();
			send(type);
		} else if (!player.timer) {
			player.timer = setTimeout(() => {
				player.timer = null;
				player.last = Date.now();
				send(type);
			}, wait);
		}
	};
	for (const type of immediateEvents) {
		player.listeners.push([type, () => send(type)]);
	}
	for (const type of throttledEvents) {
		player.listeners.push([type, () => sendThrottled(type)]);
	}
	for (const [type, listener] of player.listeners) {
		el.addEventListener(type, listener);
	}
	players.set(el, player);
	el.preservesPitch = true;
	// media that loaded before the script attached still reports its duration
	if (el.readyState >= 1) {
		send("loadedmetadata");
	}
}

export function detach(el) {
	clearVisualisation(el);
	if (el) {
		liveBuffers.delete(el);
	}
	const player = el ? players.get(el) : null;
	if (!player) {
		return;
	}
	for (const [type, listener] of player.listeners) {
		el.removeEventListener(type, listener);
	}
	clearTimeout(player.timer);
	player.dotNetRef = null;
	players.delete(el);
}

export function configure(el, intervalMs) {
	const player = el ? players.get(el) : null;
	if (player) {
		player.intervalMs = intervalMs;
	}
}

// resolves to null when playback starts, or to the error's name (NotAllowedError when the browser blocks it)
export function play(el) {
	return el.play().then(
		() => null,
		(e) => (e && e.name) || "Error",
	);
}

export function pause(el) {
	el?.pause();
}

export function seek(el, seconds) {
	if (el) {
		el.currentTime = seconds;
	}
}

export function setRate(el, rate) {
	if (el) {
		// the default rate survives a change of source; preservesPitch keeps voices natural at speed
		el.defaultPlaybackRate = rate;
		el.playbackRate = rate;
		el.preservesPitch = true;
	}
}

export function setVolume(el, volume, muted) {
	if (el) {
		el.volume = volume;
		el.muted = muted;
	}
}

// asks for the first byte of a source that failed, to learn its HTTP status (401, 403, 404...), and reports it
// to .NET through OnProbeResult; returns true once the request has been sent
export function probe(el, url) {
	const report = (status) => {
		const player = el ? players.get(el) : null;
		if (player && player.dotNetRef) {
			player.dotNetRef
				.invokeMethodAsync("OnProbeResult", url, status)
				.catch(() => {});
		}
	};
	const request = new XMLHttpRequest();
	request.open("GET", url);
	request.setRequestHeader("Range", "bytes=0-0");
	// the status is all that is wanted, so the body is not waited for
	request.onreadystatechange = () => {
		if (request.readyState === XMLHttpRequest.HEADERS_RECEIVED) {
			report(request.status || null);
			request.abort();
		}
	};
	request.onerror = () => report(null);
	request.send();
	return true;
}
export function getItem(key) {
	try {
		return localStorage.getItem(key);
	} catch {
		return null;
	}
}

export function setItem(key, value) {
	try {
		localStorage.setItem(key, value);
	} catch {
		// storage is full or blocked; the position is simply not remembered
	}
}

// scrolls a transcript pane so that its active line is in view, without scrolling the page
export function reveal(container) {
	const line = container ? container.querySelector(".pdmp-line-active") : null;
	if (line) {
		const top =
			line.getBoundingClientRect().top -
			container.getBoundingClientRect().top +
			container.scrollTop;
		if (
			top < container.scrollTop ||
			top + line.offsetHeight > container.scrollTop + container.clientHeight
		) {
			container.scrollTop = top - container.clientHeight / 3;
		}
	}
}

// ---------------------------------------------------------------------------------------------------------------------------------
// Visualisation: a spectrogram with labelled axes, a playhead, and optional key points and landmarks drawn over it. .NET decides what
// is shown and passes the points and lines; this code decodes the audio, makes the spectrogram, draws, and reports clicks and status.
// ---------------------------------------------------------------------------------------------------------------------------------

const visuals = new WeakMap();
const liveBuffers = new WeakMap();
const maxLiveColumns = 16384;
const timeSteps = [
	0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800,
	3600,
];
const minTimeSpan = 0.5;
const minFrequencySpan = 100;
const wheelZoomPerPixel = 0.002;
const tinyAmount = Math.pow(10, -9);
const silenceFloor = Math.pow(10, -12);
const redrawIntervalMs = 40;
const defaultLandmarkColour = "rgba(124,240,178,0.65)";
const dynamicRangeDb = 80;

export function visualise(el, canvas, dotNetRef, options) {
	clearVisualisation(el);
	if (!el || !canvas) {
		return;
	}
	const v = {
		el,
		canvas,
		dotNetRef,
		options,
		spectrogram: null,
		keyPoints: new Float64Array(0),
		landmarks: new Float64Array(0),
		colours: [],
		pending: 0,
		view: { time: null, freq: null },
		following: true,
		live: liveBuffers.get(el) || null,
		generation: 0,
		timer: 0,
		listeners: [],
		observer: null,
	};
	visuals.set(el, v);
	attachVisualListeners(v);
	if (!el.paused) {
		startRedraw(v);
	}
	if (options.showSpectrogram) {
		loadSpectrogram(v);
	} else {
		draw(v);
	}
}

export function clearVisualisation(el) {
	const v = el ? visuals.get(el) : null;
	if (!v) {
		return;
	}
	clearInterval(v.timer);
	clearTimeout(v.pending);
	for (const [type, fn] of v.listeners) {
		el.removeEventListener(type, fn);
	}
	v.canvas.removeEventListener("click", v.onClick);
	v.canvas.removeEventListener("wheel", v.onWheel);
	if (v.observer) {
		v.observer.disconnect();
	}
	v.generation++;
	v.dotNetRef = null;
	visuals.delete(el);
}

function attachVisualListeners(v) {
	const redraw = () => draw(v);
	const handlers = [
		["play", () => startRedraw(v)],
		["pause", () => stopRedraw(v)],
		["ended", () => stopRedraw(v)],
		["seeked", redraw],
		["timeupdate", redraw],
		["loadedmetadata", redraw],
		["durationchange", redraw],
	];
	for (const [type, fn] of handlers) {
		v.el.addEventListener(type, fn);
		v.listeners.push([type, fn]);
	}
	v.onClick = (e) => reportClick(v, e);
	v.canvas.addEventListener("click", v.onClick);
	v.onWheel = (e) => onWheel(v, e);
	v.canvas.addEventListener("wheel", v.onWheel, { passive: false });
	if (typeof ResizeObserver !== "undefined") {
		v.observer = new ResizeObserver(redraw);
		v.observer.observe(v.canvas);
	}
}

// Replaces the key points (time, Hz, weight triples) and the landmarks (time, Hz, time, Hz quads), both as raw float64 bytes,
// which cross the interop boundary far faster than the same numbers as JSON; colours holds a CSS colour (or null) per landmark,
// or is null when none has a colour of its own.
export function setVisualisationData(el, points, lines, colours) {
	const v = el ? visuals.get(el) : null;
	if (!v) {
		return;
	}
	v.keyPoints = toFloats(points);
	v.landmarks = toFloats(lines);
	v.colours = colours || [];
	scheduleDraw(v);
}

function toFloats(bytes) {
	if (!bytes || bytes.byteLength < 8) {
		return new Float64Array(0);
	}
	return new Float64Array(
		bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength),
	);
}

// several changes in quick succession (new columns, new points) are drawn once
function scheduleDraw(v) {
	if (v.pending) {
		return;
	}
	v.pending = setTimeout(() => {
		v.pending = 0;
		draw(v);
	}, redrawIntervalMs);
}

function clampNumber(value, low, high) {
	return Math.min(Math.max(value, low), Math.max(low, high));
}

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
	const back = clampNumber(nowSeconds(v) - (start + span), 0, limit - span);
	const isDefault =
		back < tinyAmount && Math.abs(span - v.options.windowSeconds) < tinyAmount;
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
function zoomTime(v, factor, anchor) {
	const l = layout(v);
	const next = clampNumber(l.seconds * factor, minTimeSpan, maxTimeSpan(v));
	const ratio = (anchor - l.start) / l.seconds;
	setTimeView(v, anchor - ratio * next, next);
}

function panTime(v, fraction) {
	const l = layout(v);
	setTimeView(v, l.start + fraction * l.seconds, l.seconds);
}

function zoomFrequency(v, factor, anchor) {
	const l = layout(v);
	const span = l.maxHz - l.minHz;
	const next = clampNumber(span * factor, minFrequencySpan, fullMaxHz(v));
	setFrequencyView(v, anchor - ((anchor - l.minHz) / span) * next, next);
}

function panFrequency(v, fraction) {
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

function onWheel(v, e) {
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

// Called from the control panel: zoom about the middle of the view, or pan by a fraction of it
export function zoomView(el, axis, factor) {
	const v = el ? visuals.get(el) : null;
	if (!v) {
		return;
	}
	v.following = false;
	const l = layout(v);
	if (axis === "frequency") {
		zoomFrequency(v, factor, (l.minHz + l.maxHz) / 2);
	} else {
		zoomTime(v, factor, l.start + l.seconds / 2);
	}
	draw(v);
}

export function panView(el, axis, fraction) {
	const v = el ? visuals.get(el) : null;
	if (!v) {
		return;
	}
	v.following = false;
	if (axis === "frequency") {
		panFrequency(v, fraction);
	} else {
		panTime(v, fraction);
	}
	draw(v);
}

export function resetView(el) {
	const v = el ? visuals.get(el) : null;
	if (v) {
		v.view = { time: null, freq: null };
		v.following = true;
		draw(v);
	}
}

// Resumes following the playhead
export function followPlayhead(el) {
	const v = el ? visuals.get(el) : null;
	if (v) {
		v.following = true;
		draw(v);
	}
}

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
function applyFollow(v) {
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

// appends columns to the live spectrogram: levels holds columns of bins bytes each, quiet 0 to loud 255, spread from 0 Hz up to
// the maximum frequency; every column lasts columnSeconds and follows the one before it
export function pushSpectrum(el, levels, bins, columnSeconds) {
	const v = el ? visuals.get(el) : null;
	if (!v) {
		return;
	}
	if (!liveFits(v.live, v.options, bins, columnSeconds)) {
		v.live = createLive(v.options, bins, columnSeconds);
		liveBuffers.set(el, v.live);
	}
	const columns = Math.floor(levels.length / bins);
	for (let i = 0; i < columns; i++) {
		writeColumn(v.live, levels, i * bins);
	}
	scheduleDraw(v);
}

export function clearSpectrum(el) {
	const v = el ? visuals.get(el) : null;
	liveBuffers.delete(el);
	if (v) {
		v.live = null;
		draw(v);
	}
}

function startRedraw(v) {
	clearInterval(v.timer);
	v.timer = setInterval(() => draw(v), redrawIntervalMs);
}

function stopRedraw(v) {
	clearInterval(v.timer);
	v.timer = 0;
	draw(v);
}

function reportClick(v, e) {
	const point = fromPixel(v, e);
	if (point && v.dotNetRef) {
		v.following = true;
		v.dotNetRef
			.invokeMethodAsync("OnVisualisationPointer", point.seconds, point.hz)
			.catch(() => {});
	}
}

function report(v, status) {
	if (v.dotNetRef) {
		v.dotNetRef
			.invokeMethodAsync("OnSpectrogramStatus", status)
			.catch(() => {});
	}
}

// ---- getting the audio and making the spectrogram ----

// reads a media source into an ArrayBuffer and calls done with it, or with null when it could not be read
function fetchBytes(url, done) {
	const request = new XMLHttpRequest();
	request.open("GET", url);
	request.responseType = "arraybuffer";
	request.onload = () => {
		const ok = request.status >= 200 && request.status < 300;
		done(ok ? request.response : null);
	};
	request.onerror = () => done(null);
	request.send();
}

function createAudioContext(sampleRate) {
	try {
		return new AudioContext({ sampleRate });
	} catch (error) {
		return new AudioContext();
	}
}

// decodes bytes at the wanted sample rate and calls done with the decoded audio, or with null when it cannot be decoded
function decodeBytes(bytes, sampleRate, done) {
	const context = createAudioContext(sampleRate);
	const finish = (decoded) => {
		context.close();
		done(decoded);
	};
	context.decodeAudioData(
		bytes,
		(decoded) => finish(decoded),
		() => finish(null),
	);
}

// calls done with the spectrogram (or null) and a status of ready, toolong or failed
function analyseSource(v, src, done) {
	fetchBytes(src, (bytes) => {
		if (!bytes) {
			done(null, "failed");
			return;
		}
		decodeBytes(bytes, v.options.sampleRate, (decoded) => {
			done(...spectrogramFor(v, decoded));
		});
	});
}

function spectrogramFor(v, decoded) {
	if (!decoded) {
		return [null, "failed"];
	}
	if (decoded.duration > v.options.maxDuration) {
		return [null, "toolong"];
	}
	try {
		return [makeSpectrogram(decoded, v.options), "ready"];
	} catch (error) {
		return [null, "failed"];
	}
}

function loadSpectrogram(v) {
	const generation = ++v.generation;
	// currentSrc may still name the previous resource immediately after the host replaces src.
	const src = v.el.src;
	if (!src) {
		draw(v);
		return;
	}
	report(v, "loading");
	analyseSource(v, src, (spectrogram, status) => {
		if (generation !== v.generation) {
			return;
		}
		v.spectrogram = spectrogram;
		report(v, status);
		draw(v);
	});
}

function mixToMono(decoded) {
	const mono = new Float32Array(decoded.length);
	const channels = decoded.numberOfChannels;
	for (let c = 0; c < channels; c++) {
		const channel = decoded.getChannelData(c);
		for (let i = 0; i < mono.length; i++) {
			mono[i] += channel[i] / channels;
		}
	}
	return mono;
}

// the Hann window, the twiddle factors and the bit-reversed order that a radix-2 FFT of this size needs
function fftTables(size) {
	const hann = new Float64Array(size);
	const cos = new Float64Array(size / 2);
	const sin = new Float64Array(size / 2);
	const reverse = new Uint32Array(size);
	const bits = Math.log2(size);
	for (let i = 0; i < size; i++) {
		hann[i] = 0.5 - 0.5 * Math.cos((2 * Math.PI * i) / size);
		reverse[i] = reverseBits(i, bits);
	}
	for (let i = 0; i < size / 2; i++) {
		cos[i] = Math.cos((-2 * Math.PI * i) / size);
		sin[i] = Math.sin((-2 * Math.PI * i) / size);
	}
	return { size, hann, cos, sin, reverse };
}

function reverseBits(value, bits) {
	let result = 0;
	for (let b = 0; b < bits; b++) {
		result |= ((value >> b) & 1) << (bits - 1 - b);
	}
	return result;
}

function loadFrame(mono, start, t, re, im) {
	for (let i = 0; i < t.size; i++) {
		re[t.reverse[i]] = mono[start + i] * t.hann[i];
		im[t.reverse[i]] = 0;
	}
}

function butterfly(re, im, a, b, wr, wi) {
	const tr = re[b] * wr - im[b] * wi;
	const ti = re[b] * wi + im[b] * wr;
	re[b] = re[a] - tr;
	im[b] = im[a] - ti;
	re[a] += tr;
	im[a] += ti;
}

function fftInPlace(re, im, t) {
	for (let size = 2; size <= t.size; size <<= 1) {
		const half = size >> 1;
		const step = t.size / size;
		for (let start = 0; start < t.size; start += size) {
			for (let k = 0; k < half; k++) {
				butterfly(
					re,
					im,
					start + k,
					start + k + half,
					t.cos[k * step],
					t.sin[k * step],
				);
			}
		}
	}
}

// writes the power in decibels of the first `bins` frequencies into db and returns the loudest
function powerColumn(re, im, bins, db, offset) {
	let loudest = -Infinity;
	for (let k = 0; k < bins; k++) {
		const value = 10 * Math.log10(re[k] * re[k] + im[k] * im[k] + silenceFloor);
		db[offset + k] = value;
		loudest = Math.max(loudest, value);
	}
	return loudest;
}

// Short-time Fourier transform: column m is the window that starts at m * hop samples.
function analyseFrames(mono, rate, options) {
	const t = fftTables(options.windowSize);
	const hzPerBin = rate / t.size;
	const maxHz = Math.min(options.maxFrequency, rate / 2);
	const bins = Math.min(t.size / 2 + 1, Math.ceil(maxHz / hzPerBin) + 1);
	const frames = Math.max(
		0,
		Math.floor((mono.length - t.size) / options.hopSize) + 1,
	);
	const db = new Float32Array(frames * bins);
	const re = new Float64Array(t.size);
	const im = new Float64Array(t.size);
	let loudest = -Infinity;
	for (let f = 0; f < frames; f++) {
		loadFrame(mono, f * options.hopSize, t, re, im);
		fftInPlace(re, im, t);
		loudest = Math.max(loudest, powerColumn(re, im, bins, db, f * bins));
	}
	return { db, loudest, frames, bins, hzPerBin, maxHz };
}

function clamp01(value) {
	return Math.min(1, Math.max(0, value));
}

// a dark blue to magenta to yellow ramp for a level from 0 (quiet) to 1 (loud)
function heatColour(level) {
	const red = Math.round(255 * Math.pow(level, 0.9) * 1.25);
	const green = Math.round(255 * Math.pow(level, 2.2) * 1.1);
	const wave = Math.sin(Math.PI * Math.min(1, level * 1.2));
	const blue =
		level < 0.05 ? 20 : Math.round(70 + 140 * wave * (1 - level * 0.6));
	return [Math.min(255, red), Math.min(255, green), blue];
}

function paintSpectrogram(a) {
	const image = document.createElement("canvas");
	image.width = Math.max(1, a.frames);
	image.height = a.bins;
	const context = image.getContext("2d");
	const data = context.createImageData(image.width, image.height);
	for (let f = 0; f < a.frames; f++) {
		for (let k = 0; k < a.bins; k++) {
			const level = clamp01(
				(a.db[f * a.bins + k] - (a.loudest - dynamicRangeDb)) / dynamicRangeDb,
			);
			const p = ((a.bins - 1 - k) * image.width + f) * 4;
			const [red, green, blue] = heatColour(level);
			data.data.set([red, green, blue, 255], p);
		}
	}
	context.putImageData(data, 0, 0);
	return image;
}

function makeSpectrogram(decoded, options) {
	const analysis = analyseFrames(
		mixToMono(decoded),
		decoded.sampleRate,
		options,
	);
	return {
		image: paintSpectrogram(analysis),
		frames: analysis.frames,
		bins: analysis.bins,
		hzPerBin: analysis.hzPerBin,
		seconds: (analysis.frames * options.hopSize) / decoded.sampleRate,
		maxHz: analysis.maxHz,
	};
}

// ---- the live spectrogram: a ring of columns held on an off-screen canvas, one pixel wide each ----

function liveCapacity(options, columnSeconds) {
	const window = options.windowSeconds;
	const span =
		window > 0
			? Math.min(options.maxDuration, window * 10)
			: options.maxDuration;
	return Math.min(maxLiveColumns, Math.ceil(span / columnSeconds) + 2);
}

function liveFits(live, options, bins, columnSeconds) {
	return (
		live &&
		live.bins === bins &&
		live.columnSeconds === columnSeconds &&
		live.capacity === liveCapacity(options, columnSeconds)
	);
}

function createLive(options, bins, columnSeconds) {
	const capacity = liveCapacity(options, columnSeconds);
	const canvas = document.createElement("canvas");
	canvas.width = capacity;
	canvas.height = bins;
	return {
		bins,
		columnSeconds,
		capacity,
		maxFrequency: options.maxFrequency,
		canvas,
		context: canvas.getContext("2d"),
		count: 0,
	};
}

function liveEnd(live) {
	return live.count * live.columnSeconds;
}

function writeColumn(live, levels, offset) {
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

function plotSeconds(v) {
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
function nowSeconds(v) {
	return v.live && v.live.count > 0 ? liveEnd(v.live) : v.el.currentTime || 0;
}

// the first time shown and how many seconds are shown: all of it, or a window that slides to end at the present
// the first time shown and how many seconds are shown: all of it, or a window that slides to end at the present, either of
// which the user may have zoomed and panned
function timeRange(v) {
	const windowSeconds = v.options.windowSeconds;
	const view = v.view.time;
	if (windowSeconds > 0) {
		const span = view ? view.span : windowSeconds;
		const back = view ? view.back : 0;
		return { start: nowSeconds(v) - back - span, seconds: span };
	}
	if (view) {
		return { start: view.start, seconds: view.span };
	}
	return { start: 0, seconds: plotSeconds(v) || 1 };
}

function fullMaxHz(v) {
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

function layout(v) {
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
	return {
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
}

function fromPixel(v, e) {
	const l = layout(v);
	const rect = v.canvas.getBoundingClientRect();
	const x = e.clientX - rect.left - l.left;
	const y = e.clientY - rect.top - l.top;
	if (x < 0 || y < 0 || x > l.pw || y > l.ph) {
		return null;
	}
	return {
		seconds: l.start + (x / l.pw) * l.seconds,
		hz: l.minHz + (1 - y / l.ph) * (l.maxHz - l.minHz),
	};
}

function niceStep(span, pixels, minPixels) {
	const step = timeSteps.find((s) => (s / span) * pixels >= minPixels);
	return step ?? timeSteps[timeSteps.length - 1];
}

function decimalPlaces(step) {
	if (step >= 1) {
		return 0;
	}
	return step >= 0.1 ? 1 : 2;
}

function zeroPad(text, decimals) {
	return text.padStart(decimals === 0 ? 2 : decimals + 3, "0");
}

// seconds as a label; a step finer than a second needs decimals, and a minute or more reads as minutes and seconds
function formatTime(seconds, step) {
	const decimals = decimalPlaces(step);
	const rounded = Number(seconds.toFixed(decimals));
	if (rounded >= 60) {
		const minutes = Math.floor(rounded / 60);
		return (
			minutes +
			":" +
			zeroPad((rounded - minutes * 60).toFixed(decimals), decimals)
		);
	}
	return rounded.toFixed(decimals);
}

// A clock label for a time in seconds on the broadcast clock. wallSeconds counts from midnight of the wall-clock day the
// media started on, in the start time's own offset, so it can run past 24 hours and is wrapped for display.
function formatClock(wallSeconds, step) {
	const decimals = decimalPlaces(step);
	const day = ((wallSeconds % 86400) + 86400) % 86400;
	const h = String(Math.floor(day / 3600)).padStart(2, "0");
	const m = String(Math.floor((day % 3600) / 60)).padStart(2, "0");
	if (step >= 60) {
		return h + ":" + m;
	}
	return h + ":" + m + ":" + zeroPad((day % 60).toFixed(decimals), decimals);
}

function frequencyStep(spanHz) {
	const steps = [
		[4000, 1000],
		[1500, 500],
		[600, 200],
		[300, 100],
		[150, 50],
		[60, 20],
	];
	const found = steps.find(([from]) => spanHz >= from);
	return found ? found[1] : 10;
}

function scales(l) {
	const hzSpan = l.maxHz - l.minHz;
	return {
		x: (t) => l.left + ((t - l.start) / l.seconds) * l.pw,
		y: (hz) => l.top + l.ph - ((hz - l.minHz) / hzSpan) * l.ph,
	};
}

// ---- drawing ----

function drawBackground(ctx, v, l) {
	ctx.setTransform(l.dpr, 0, 0, l.dpr, 0, 0);
	ctx.clearRect(0, 0, l.width, l.height);
	ctx.fillStyle = "#0a0e18";
	ctx.fillRect(l.left, l.top, l.pw, l.ph);
	if (!v.options.showSpectrogram) {
		return;
	}
	if (v.spectrogram) {
		drawDecoded(ctx, v.spectrogram, l);
	}
	if (v.live && v.live.count > 0) {
		drawLiveSpectrum(ctx, v.live, l);
	}
}

// the part of the decoded spectrogram that falls inside the time range
// the part of the decoded spectrogram that falls inside the time and frequency ranges
function drawDecoded(ctx, s, l) {
	const from = Math.max(0, l.start);
	const to = Math.min(s.seconds, l.start + l.seconds);
	if (to <= from) {
		return;
	}
	const sy = Math.max(0, s.bins - 1 - l.maxHz / s.hzPerBin);
	const sh = Math.min(s.bins, s.bins - l.minHz / s.hzPerBin) - sy;
	const sx = (from / s.seconds) * s.frames;
	const sw = ((to - from) / s.seconds) * s.frames;
	const dx = l.left + ((from - l.start) / l.seconds) * l.pw;
	const dw = ((to - from) / l.seconds) * l.pw;
	ctx.imageSmoothingEnabled = true;
	ctx.drawImage(s.image, sx, sy, sw, sh, dx, l.top, dw, l.ph);
}

function drawLiveSpectrum(ctx, live, l) {
	const earliest = Math.max(0, live.count - live.capacity);
	const first = Math.max(earliest, Math.floor(l.start / live.columnSeconds));
	const last = Math.min(
		live.count,
		Math.ceil((l.start + l.seconds) / live.columnSeconds),
	);
	if (last > first) {
		drawLiveColumns(ctx, live, l, first, last);
	}
}

// draws columns first to last (exclusive), in two pieces when they wrap round the end of the ring
// draws columns first to last (exclusive), in two pieces when they wrap round the end of the ring
function drawLiveColumns(ctx, live, l, first, last) {
	const room = live.capacity - (first % live.capacity);
	if (last - first > room) {
		drawLiveColumns(ctx, live, l, first, first + room);
		drawLiveColumns(ctx, live, l, first + room, last);
		return;
	}
	const cs = live.columnSeconds;
	const dx = l.left + ((first * cs - l.start) / l.seconds) * l.pw;
	const dw = (((last - first) * cs) / l.seconds) * l.pw;
	const rows = liveRows(live, l);
	ctx.imageSmoothingEnabled = false;
	ctx.drawImage(
		live.canvas,
		first % live.capacity,
		rows.top,
		last - first,
		rows.height,
		dx,
		l.top,
		dw,
		l.ph,
	);
}

// the rows of the live image that fall inside the frequency range (row 0 is the highest frequency)
function liveRows(live, l) {
	const perHz = (live.bins - 1) / live.maxFrequency;
	const top = Math.max(0, live.bins - 1 - l.maxHz * perHz);
	const bottom = Math.min(live.bins, live.bins - l.minHz * perHz);
	return { top, height: bottom - top };
}

// the times (in seconds from the start of the media) of the ticks; on the broadcast clock they fall on round clock times
// the times (in seconds from the start of the media) of the ticks; on the broadcast clock they fall on round clock times
function timeTicks(l, clock, offset, step) {
	const from = Math.max(l.start, 0) + offset;
	const end = l.start + l.seconds + offset;
	const ticks = [];
	for (
		let k = Math.ceil(from / step - tinyAmount);
		k * step <= end + tinyAmount;
		k++
	) {
		ticks.push(k * step - offset);
	}
	return ticks;
}

function drawTimeAxis(ctx, v, l, sc) {
	const clock = Number.isFinite(v.options.startSeconds);
	const step = niceStep(l.seconds, l.pw, clock ? 70 : 60);
	const offset = clock ? v.options.startSeconds : 0;
	ctx.strokeStyle = "rgba(143,155,184,0.9)";
	ctx.fillStyle = "#8f9bb8";
	ctx.lineWidth = 1;
	ctx.font = "11px system-ui, sans-serif";
	ctx.strokeRect(l.left + 0.5, l.top + 0.5, l.pw, l.ph);
	for (const t of timeTicks(l, clock, offset, step)) {
		const px = Math.round(sc.x(t)) + 0.5;
		ctx.beginPath();
		ctx.moveTo(px, l.top + l.ph);
		ctx.lineTo(px, l.top + l.ph + 4);
		ctx.stroke();
		const text = clock ? formatClock(offset + t, step) : formatTime(t, step);
		drawEdgeAwareLabel(ctx, text, px, l);
	}
}

// keeps the first and last labels inside the plot instead of half cut off
function drawEdgeAwareLabel(ctx, text, px, l) {
	const atStart = px < l.left + 12;
	const atEnd = px > l.left + l.pw - 12;
	ctx.textAlign = atStart ? "left" : atEnd ? "right" : "center";
	const x = atStart ? l.left : atEnd ? l.left + l.pw : px;
	ctx.fillText(text, x, l.top + l.ph + 16);
}

function drawFrequencyLabel(ctx, l, text, py) {
	const labelY = py - 3 < l.top + 9 ? py + 12 : py - 3;
	ctx.lineWidth = 3;
	ctx.strokeStyle = "rgba(10,14,24,0.85)";
	ctx.strokeText(text, l.left + 4, labelY);
	ctx.lineWidth = 1;
	ctx.strokeStyle = "rgba(143,155,184,0.9)";
	ctx.fillText(text, l.left + 4, labelY);
}

function drawFrequencyAxis(ctx, l, sc) {
	const step = frequencyStep(l.maxHz - l.minHz);
	ctx.textAlign = "left";
	for (
		let k = Math.ceil(l.minHz / step - tinyAmount);
		k * step <= l.maxHz + tinyAmount;
		k++
	) {
		const hz = k * step;
		const py = Math.round(sc.y(hz)) + 0.5;
		drawFrequencyLabel(ctx, l, frequencyLabel(hz), py);
		if (hz > 0) {
			ctx.strokeStyle = "rgba(255,255,255,0.07)";
			ctx.beginPath();
			ctx.moveTo(l.left, py);
			ctx.lineTo(l.left + l.pw, py);
			ctx.stroke();
		}
	}
}

function frequencyLabel(hz) {
	return hz >= 1000 ? Number((hz / 1000).toFixed(2)) + " kHz" : hz + " Hz";
}

// one path per colour, so thousands of landmarks cost a few strokes rather than thousands
function drawLandmarks(ctx, v, sc) {
	const lines = v.landmarks;
	const byColour = new Map();
	for (let i = 0, n = 0; i + 3 < lines.length; i += 4, n++) {
		const colour = v.colours[n] || defaultLandmarkColour;
		if (!byColour.has(colour)) {
			byColour.set(colour, []);
		}
		byColour.get(colour).push(i);
	}
	ctx.lineWidth = 1;
	for (const [colour, starts] of byColour) {
		ctx.strokeStyle = colour;
		ctx.beginPath();
		for (const i of starts) {
			ctx.moveTo(sc.x(lines[i]), sc.y(lines[i + 1]));
			ctx.lineTo(sc.x(lines[i + 2]), sc.y(lines[i + 3]));
		}
		ctx.stroke();
	}
}

// one path per dot size (the weight in half-pixel steps)
function drawKeyPoints(ctx, v, sc) {
	const points = v.keyPoints;
	const bySize = new Map();
	for (let i = 0; i + 2 < points.length; i += 3) {
		const radius = Math.round((2 + points[i + 2] * 2.5) * 2) / 2;
		if (!bySize.has(radius)) {
			bySize.set(radius, []);
		}
		bySize.get(radius).push(i);
	}
	ctx.fillStyle = "#4cc2ff";
	ctx.strokeStyle = "#06202e";
	for (const [radius, starts] of bySize) {
		ctx.beginPath();
		for (const i of starts) {
			const x = sc.x(points[i]);
			ctx.moveTo(x + radius, sc.y(points[i + 1]));
			ctx.arc(x, sc.y(points[i + 1]), radius, 0, Math.PI * 2);
		}
		ctx.fill();
		ctx.stroke();
	}
}

function drawPlayhead(ctx, v, l, sc) {
	const px = sc.x(v.el.currentTime || 0);
	ctx.strokeStyle = "#ffffff";
	ctx.lineWidth = 1.5;
	ctx.beginPath();
	ctx.moveTo(px, l.top);
	ctx.lineTo(px, l.top + l.ph);
	ctx.stroke();
}

function drawOverlays(ctx, v, l, sc) {
	ctx.save();
	ctx.beginPath();
	ctx.rect(l.left, l.top, l.pw, l.ph);
	ctx.clip();
	if (v.options.showLandmarks) {
		drawLandmarks(ctx, v, sc);
	}
	if (v.options.showKeyPoints) {
		drawKeyPoints(ctx, v, sc);
	}
	if (!(v.options.windowSeconds > 0)) {
		drawPlayhead(ctx, v, l, sc);
	}
	ctx.restore();
}

function draw(v) {
	if (!v.dotNetRef) {
		return;
	}
	applyFollow(v);
	const l = layout(v);
	const ctx = v.canvas.getContext("2d");
	const sc = scales(l);
	drawBackground(ctx, v, l);
	drawTimeAxis(ctx, v, l, sc);
	if (v.options.frequencyLabels) {
		drawFrequencyAxis(ctx, l, sc);
	}
	drawOverlays(ctx, v, l, sc);
}
