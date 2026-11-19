// PDMediaPlayer: forwards media element events to .NET and applies the commands .NET sends.
// Every decision (what to show, where to seek, how to describe an error) is made in C#; this file only
// reports what the element did and does what it is told.

import { makeSpectrogram } from "./js/pdmediaplayer/pdmediaplayer-spectrogram.js";
import {
	draw,
	onWheel,
	overviewDown,
	overviewMove,
	overviewWheel,
	panFrequency,
	panTime,
	zoomFrequency,
	zoomTime,
} from "./js/pdmediaplayer/pdmediaplayer-view.js";
import {
	createLive,
	fromPixel,
	layout,
	liveFits,
	writeColumn,
} from "./js/pdmediaplayer/pdmediaplayer-layout.js";
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
const redrawIntervalMs = 40;

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
		root: el.closest(".pd-media-player"),
		drag: null,
		published: "",
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
	const host = v.canvas.parentElement;
	if (host) {
		host.removeEventListener("pointerdown", v.onOverviewDown);
		host.removeEventListener("pointermove", v.onOverviewMove);
		host.removeEventListener("pointerup", v.onOverviewUp);
		host.removeEventListener("pointercancel", v.onOverviewUp);
		host.removeEventListener("wheel", v.onOverviewWheel);
	}
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
	v.onOverviewDown = (e) => overviewDown(v, e);
	v.onOverviewMove = (e) => overviewMove(v, e);
	v.onOverviewUp = () => (v.drag = null);
	v.onOverviewWheel = (e) => overviewWheel(v, e);
	const host = v.canvas.parentElement;
	host.addEventListener("pointerdown", v.onOverviewDown);
	host.addEventListener("pointermove", v.onOverviewMove);
	host.addEventListener("pointerup", v.onOverviewUp);
	host.addEventListener("pointercancel", v.onOverviewUp);
	host.addEventListener("wheel", v.onOverviewWheel, { passive: false });
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

// ---- drawing ----
