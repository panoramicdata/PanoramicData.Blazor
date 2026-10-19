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
const timeSteps = [0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800, 3600];
const tinyAmount = Math.pow(10, -9);
const silenceFloor = Math.pow(10, -12);
const redrawIntervalMs = 40;
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
	for (const [type, fn] of v.listeners) {
		el.removeEventListener(type, fn);
	}
	v.canvas.removeEventListener("click", v.onClick);
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
	if (typeof ResizeObserver !== "undefined") {
		v.observer = new ResizeObserver(redraw);
		v.observer.observe(v.canvas);
	}
}

// swaps the options (the points and lines, mostly) without attaching again
export function updateVisualisation(el, options) {
	const v = el ? visuals.get(el) : null;
	if (v) {
		v.options = options;
		draw(v);
	}
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
	draw(v);
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
	const src = v.el.currentSrc || v.el.src;
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
	const span =
		options.windowSeconds > 0 ? options.windowSeconds : options.maxDuration;
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

function dataSeconds(options) {
	let seconds = 0;
	for (let i = 0; i < options.keyPoints.length; i += 3) {
		seconds = Math.max(seconds, options.keyPoints[i] + 0.5);
	}
	for (let i = 0; i < options.landmarks.length; i += 4) {
		seconds = Math.max(seconds, options.landmarks[i], options.landmarks[i + 2]);
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
	return Number.isFinite(duration) && duration > 0
		? duration
		: dataSeconds(v.options);
}

// the time the plot ends at: the end of the live data, or the playhead
function nowSeconds(v) {
	return v.live && v.live.count > 0 ? liveEnd(v.live) : v.el.currentTime || 0;
}

// the first time shown and how many seconds are shown: all of it, or a window that slides to end at the present
function timeRange(v) {
	const windowSeconds = v.options.windowSeconds;
	if (windowSeconds > 0) {
		return { start: nowSeconds(v) - windowSeconds, seconds: windowSeconds };
	}
	return { start: 0, seconds: plotSeconds(v) || 1 };
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
		maxHz: v.spectrogram ? v.spectrogram.maxHz : v.options.maxFrequency,
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
		hz: (1 - y / l.ph) * l.maxHz,
	};
}

function niceStep(span, pixels, minPixels) {
	const step = timeSteps.find((s) => (s / span) * pixels >= minPixels);
	return step ?? timeSteps[timeSteps.length - 1];
}

function formatTime(seconds) {
	const whole = Math.round(seconds * 10) / 10;
	if (whole < 60) {
		return Number.isInteger(whole) ? String(whole) : whole.toFixed(1);
	}
	const minutes = Math.floor(whole / 60);
	return minutes + ":" + String(Math.round(whole % 60)).padStart(2, "0");
}

// A clock label for a time in seconds on the broadcast clock. wallSeconds counts from midnight of the wall-clock day the
// media started on, in the start time's own offset, so it can run past 24 hours and is wrapped for display.
function formatClock(wallSeconds, step) {
	const day = ((Math.round(wallSeconds) % 86400) + 86400) % 86400;
	const h = String(Math.floor(day / 3600)).padStart(2, "0");
	const m = String(Math.floor((day % 3600) / 60)).padStart(2, "0");
	return step >= 60
		? h + ":" + m
		: h + ":" + m + ":" + String(day % 60).padStart(2, "0");
}

function frequencyStep(maxHz) {
	if (maxHz >= 4000) {
		return 1000;
	}
	if (maxHz >= 1500) {
		return 500;
	}
	return maxHz >= 600 ? 200 : 100;
}

function scales(l) {
	return {
		x: (t) => l.left + ((t - l.start) / l.seconds) * l.pw,
		y: (hz) => l.top + l.ph - (hz / l.maxHz) * l.ph,
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
function drawDecoded(ctx, s, l) {
	const from = Math.max(0, l.start);
	const to = Math.min(s.seconds, l.start + l.seconds);
	if (to <= from) {
		return;
	}
	const rows = Math.min(s.bins, l.maxHz / s.hzPerBin + 1);
	const sx = (from / s.seconds) * s.frames;
	const sw = ((to - from) / s.seconds) * s.frames;
	const dx = l.left + ((from - l.start) / l.seconds) * l.pw;
	const dw = ((to - from) / l.seconds) * l.pw;
	ctx.imageSmoothingEnabled = true;
	ctx.drawImage(s.image, sx, s.bins - rows, sw, rows, dx, l.top, dw, l.ph);
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
	ctx.imageSmoothingEnabled = false;
	ctx.drawImage(
		live.canvas,
		first % live.capacity,
		0,
		last - first,
		live.bins,
		dx,
		l.top,
		dw,
		l.ph,
	);
}

// the times (in seconds from the start of the media) of the ticks; on the broadcast clock they fall on round clock times
function timeTicks(l, clock, offset, step) {
	const from = Math.max(l.start, 0) + offset;
	const first = Math.ceil(from / step - tinyAmount) * step - offset;
	const ticks = [];
	for (let t = first; t <= l.start + l.seconds + tinyAmount; t += step) {
		ticks.push(t);
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
		const text = clock ? formatClock(offset + t, step) : formatTime(t);
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
	const step = frequencyStep(l.maxHz);
	ctx.textAlign = "left";
	for (let hz = 0; hz <= l.maxHz + tinyAmount; hz += step) {
		const py = Math.round(sc.y(hz)) + 0.5;
		drawFrequencyLabel(
			ctx,
			l,
			hz >= 1000 ? hz / 1000 + " kHz" : hz + " Hz",
			py,
		);
		if (hz > 0) {
			ctx.strokeStyle = "rgba(255,255,255,0.07)";
			ctx.beginPath();
			ctx.moveTo(l.left, py);
			ctx.lineTo(l.left + l.pw, py);
			ctx.stroke();
		}
	}
}

function drawLandmarks(ctx, v, sc) {
	const lines = v.options.landmarks;
	const colours = v.options.landmarkColors;
	ctx.lineWidth = 1;
	for (let i = 0, n = 0; i + 3 < lines.length; i += 4, n++) {
		ctx.strokeStyle = colours[n] || "rgba(124,240,178,0.65)";
		ctx.beginPath();
		ctx.moveTo(sc.x(lines[i]), sc.y(lines[i + 1]));
		ctx.lineTo(sc.x(lines[i + 2]), sc.y(lines[i + 3]));
		ctx.stroke();
	}
}

function drawKeyPoints(ctx, v, sc) {
	const points = v.options.keyPoints;
	ctx.fillStyle = "#4cc2ff";
	ctx.strokeStyle = "#06202e";
	for (let i = 0; i + 2 < points.length; i += 3) {
		ctx.beginPath();
		ctx.arc(
			sc.x(points[i]),
			sc.y(points[i + 1]),
			2 + points[i + 2] * 2.5,
			0,
			Math.PI * 2,
		);
		ctx.fill();
		ctx.stroke();
	}
}

function drawPlayhead(ctx, v, l, sc) {
	const px = sc.x(Math.min(v.el.currentTime || 0, l.seconds));
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
