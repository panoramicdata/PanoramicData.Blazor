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
const timeSteps = [0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800, 3600];

export function visualise(el, canvas, dotNetRef, options) {
	clearVisualisation(el);
	if (!el || !canvas) {
		return;
	}
	const v = { el, canvas, dotNetRef, options, spectrogram: null, token: 0, raf: 0, listeners: [], observer: null };
	visuals.set(el, v);
	const redraw = () => draw(v);
	const startLoop = () => {
		cancelAnimationFrame(v.raf);
		const tick = () => {
			draw(v);
			v.raf = requestAnimationFrame(tick);
		};
		v.raf = requestAnimationFrame(tick);
	};
	const stopLoop = () => {
		cancelAnimationFrame(v.raf);
		v.raf = 0;
		draw(v);
	};
	const handlers = [
		["play", startLoop],
		["pause", stopLoop],
		["ended", stopLoop],
		["seeked", redraw],
		["timeupdate", redraw],
		["loadedmetadata", redraw],
		["durationchange", redraw],
	];
	for (const [type, fn] of handlers) {
		el.addEventListener(type, fn);
		v.listeners.push([type, fn]);
	}
	v.onClick = (e) => {
		const point = fromPixel(v, e);
		if (point && v.dotNetRef) {
			v.dotNetRef.invokeMethodAsync("OnVisualisationPointer", point.seconds, point.hz).catch(() => {});
		}
	};
	canvas.addEventListener("click", v.onClick);
	if (typeof ResizeObserver !== "undefined") {
		v.observer = new ResizeObserver(redraw);
		v.observer.observe(canvas);
	}
	if (!el.paused) {
		startLoop();
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
	cancelAnimationFrame(v.raf);
	for (const [type, fn] of v.listeners) {
		el.removeEventListener(type, fn);
	}
	v.canvas.removeEventListener("click", v.onClick);
	if (v.observer) {
		v.observer.disconnect();
	}
	v.token++;
	v.dotNetRef = null;
	visuals.delete(el);
}

function report(v, status) {
	if (v.dotNetRef) {
		v.dotNetRef.invokeMethodAsync("OnSpectrogramStatus", status).catch(() => {});
	}
}

async function loadSpectrogram(v) {
	const token = ++v.token;
	const src = v.el.currentSrc || v.el.src;
	if (!src) {
		draw(v);
		return;
	}
	report(v, "loading");
	try {
		const response = await fetch(src);
		const buffer = await response.arrayBuffer();
		let context;
		try {
			context = new AudioContext({ sampleRate: v.options.sampleRate });
		} catch {
			context = new AudioContext();
		}
		const decoded = await context.decodeAudioData(buffer);
		await context.close();
		if (token !== v.token) {
			return;
		}
		if (decoded.duration > v.options.maxDuration) {
			v.spectrogram = null;
			report(v, "toolong");
		} else {
			v.spectrogram = makeSpectrogram(decoded, v.options);
			report(v, "ready");
		}
	} catch {
		if (token === v.token) {
			v.spectrogram = null;
			report(v, "failed");
		}
	}
	if (token === v.token) {
		draw(v);
	}
}

// Short-time Fourier transform: Hann window, radix-2 FFT, decibels scaled over the 80 dB below the loudest cell. Column m is the
// window that starts at m * hop samples, so it is drawn at time m * hop / rate.
function makeSpectrogram(decoded, options) {
	const rate = decoded.sampleRate;
	const win = options.windowSize;
	const hop = options.hopSize;
	const length = decoded.length;
	const mono = new Float32Array(length);
	for (let c = 0; c < decoded.numberOfChannels; c++) {
		const channel = decoded.getChannelData(c);
		for (let i = 0; i < length; i++) {
			mono[i] += channel[i] / decoded.numberOfChannels;
		}
	}
	const hzPerBin = rate / win;
	const maxHz = Math.min(options.maxFrequency, rate / 2);
	const bins = Math.min(win / 2 + 1, Math.ceil(maxHz / hzPerBin) + 1);
	const frames = Math.max(0, Math.floor((length - win) / hop) + 1);
	const hann = new Float64Array(win);
	for (let i = 0; i < win; i++) {
		hann[i] = 0.5 - 0.5 * Math.cos((2 * Math.PI * i) / win);
	}
	const cos = new Float64Array(win / 2);
	const sin = new Float64Array(win / 2);
	for (let i = 0; i < win / 2; i++) {
		cos[i] = Math.cos((-2 * Math.PI * i) / win);
		sin[i] = Math.sin((-2 * Math.PI * i) / win);
	}
	const reverse = new Uint32Array(win);
	const bits = Math.log2(win);
	for (let i = 0; i < win; i++) {
		let r = 0;
		for (let b = 0; b < bits; b++) {
			r |= ((i >> b) & 1) << (bits - 1 - b);
		}
		reverse[i] = r;
	}
	const re = new Float64Array(win);
	const im = new Float64Array(win);
	const db = new Float32Array(frames * bins);
	let max = -Infinity;
	for (let f = 0; f < frames; f++) {
		for (let i = 0; i < win; i++) {
			re[reverse[i]] = mono[f * hop + i] * hann[i];
			im[reverse[i]] = 0;
		}
		for (let size = 2; size <= win; size <<= 1) {
			const half = size >> 1;
			const step = win / size;
			for (let start = 0; start < win; start += size) {
				for (let k = 0; k < half; k++) {
					const a = start + k;
					const b = a + half;
					const wr = cos[k * step];
					const wi = sin[k * step];
					const tr = re[b] * wr - im[b] * wi;
					const ti = re[b] * wi + im[b] * wr;
					re[b] = re[a] - tr;
					im[b] = im[a] - ti;
					re[a] += tr;
					im[a] += ti;
				}
			}
		}
		for (let k = 0; k < bins; k++) {
			const value = 10 * Math.log10(re[k] * re[k] + im[k] * im[k] + 1e-12);
			db[f * bins + k] = value;
			if (value > max) {
				max = value;
			}
		}
	}
	const image = document.createElement("canvas");
	image.width = Math.max(1, frames);
	image.height = bins;
	const ictx = image.getContext("2d");
	const data = ictx.createImageData(image.width, image.height);
	for (let f = 0; f < frames; f++) {
		for (let k = 0; k < bins; k++) {
			const level = Math.min(1, Math.max(0, (db[f * bins + k] - (max - 80)) / 80));
			const p = ((bins - 1 - k) * image.width + f) * 4;
			data.data[p] = Math.min(255, Math.round(255 * Math.pow(level, 0.9) * 1.25));
			data.data[p + 1] = Math.min(255, Math.round(255 * Math.pow(level, 2.2) * 1.1));
			data.data[p + 2] = level < 0.05 ? 20 : Math.round(70 + 140 * Math.sin(Math.PI * Math.min(1, level * 1.2)) * (1 - level * 0.6));
			data.data[p + 3] = 255;
		}
	}
	ictx.putImageData(data, 0, 0);
	return { image, frames, bins, hzPerBin, seconds: (frames * hop) / rate, maxHz };
}

function layout(v) {
	const dpr = window.devicePixelRatio || 1;
	const width = v.canvas.clientWidth || 600;
	const height = v.canvas.clientHeight || 200;
	if (v.canvas.width !== Math.round(width * dpr) || v.canvas.height !== Math.round(height * dpr)) {
		v.canvas.width = Math.round(width * dpr);
		v.canvas.height = Math.round(height * dpr);
	}
	const left = 0;
	const right = 0;
	const top = 8;
	const bottom = 22;
	const options = v.options;
	let seconds = v.spectrogram ? v.spectrogram.seconds : Number.isFinite(v.el.duration) ? v.el.duration : 0;
	if (!seconds) {
		for (let i = 0; i < options.keyPoints.length; i += 3) {
			seconds = Math.max(seconds, options.keyPoints[i] + 0.5);
		}
		for (let i = 0; i < options.landmarks.length; i += 4) {
			seconds = Math.max(seconds, options.landmarks[i], options.landmarks[i + 2]);
		}
	}
	return {
		dpr,
		width,
		height,
		left,
		top,
		pw: width - left - right,
		ph: height - top - bottom,
		seconds: seconds || 1,
		maxHz: v.spectrogram ? v.spectrogram.maxHz : options.maxFrequency,
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
	return { seconds: (x / l.pw) * l.seconds, hz: (1 - y / l.ph) * l.maxHz };
}

function niceStep(span, pixels, minPixels) {
	for (const step of timeSteps) {
		if ((step / span) * pixels >= minPixels) {
			return step;
		}
	}
	return timeSteps[timeSteps.length - 1];
}

function formatTime(seconds) {
	const whole = Math.round(seconds * 10) / 10;
	if (whole < 60) {
		return Number.isInteger(whole) ? String(whole) : whole.toFixed(1);
	}
	return Math.floor(whole / 60) + ":" + String(Math.round(whole % 60)).padStart(2, "0");
}

// A clock label for a time in seconds on the broadcast clock. wallSeconds counts from midnight of the wall-clock day the
// media started on, in the start time's own offset, so it can run past 24 hours and is wrapped for display.
function formatClock(wallSeconds, step) {
	const day = ((Math.round(wallSeconds) % 86400) + 86400) % 86400;
	const h = String(Math.floor(day / 3600)).padStart(2, "0");
	const m = String(Math.floor((day % 3600) / 60)).padStart(2, "0");
	if (step >= 60) {
		return h + ":" + m;
	}
	return h + ":" + m + ":" + String(day % 60).padStart(2, "0");
}

function draw(v) {
	if (!v.dotNetRef) {
		return;
	}
	const l = layout(v);
	const ctx = v.canvas.getContext("2d");
	ctx.setTransform(l.dpr, 0, 0, l.dpr, 0, 0);
	ctx.clearRect(0, 0, l.width, l.height);
	ctx.fillStyle = "#0a0e18";
	ctx.fillRect(l.left, l.top, l.pw, l.ph);
	const x = (t) => l.left + (t / l.seconds) * l.pw;
	const y = (hz) => l.top + l.ph - (hz / l.maxHz) * l.ph;

	if (v.spectrogram && v.options.showSpectrogram) {
		const s = v.spectrogram;
		const rows = Math.min(s.bins, l.maxHz / s.hzPerBin + 1);
		const sy = s.bins - rows;
		ctx.imageSmoothingEnabled = true;
		ctx.drawImage(s.image, 0, sy, s.frames, rows, l.left, l.top, (s.seconds / l.seconds) * l.pw, l.ph);
	}

	ctx.strokeStyle = "rgba(143,155,184,0.9)";
	ctx.fillStyle = "#8f9bb8";
	ctx.lineWidth = 1;
	ctx.font = "11px system-ui, sans-serif";
	ctx.strokeRect(l.left + 0.5, l.top + 0.5, l.pw, l.ph);
	const clock = Number.isFinite(v.options.startSeconds);
	const tStep = niceStep(l.seconds, l.pw, clock ? 70 : 60);
	// on the broadcast clock the ticks fall on round times (for example every 10 seconds on the :00), not on round offsets
	const offset = clock ? v.options.startSeconds : 0;
	const firstTick = clock ? Math.ceil(offset / tStep - 1e-9) * tStep - offset : 0;
	ctx.textAlign = "center";
	for (let t = firstTick; t <= l.seconds + 1e-9; t += tStep) {
		const px = Math.round(x(t)) + 0.5;
		ctx.beginPath();
		ctx.moveTo(px, l.top + l.ph);
		ctx.lineTo(px, l.top + l.ph + 4);
		ctx.stroke();
		const first = x(t) < l.left + 12;
		const last = x(t) > l.left + l.pw - 12;
		ctx.textAlign = first ? "left" : last ? "right" : "center";
		ctx.fillText(clock ? formatClock(offset + t, tStep) : formatTime(t), first ? l.left : last ? l.left + l.pw : px, l.top + l.ph + 16);
	}
	if (v.options.frequencyLabels) {
		const hzStep = l.maxHz >= 4000 ? 1000 : l.maxHz >= 1500 ? 500 : l.maxHz >= 600 ? 200 : 100;
		ctx.textAlign = "left";
		for (let hz = 0; hz <= l.maxHz + 1e-9; hz += hzStep) {
			const py = Math.round(y(hz)) + 0.5;
			const label = hz >= 1000 ? hz / 1000 + " kHz" : hz + " Hz";
			const ly = py - 3 < l.top + 9 ? py + 12 : py - 3;
			ctx.lineWidth = 3;
			ctx.strokeStyle = "rgba(10,14,24,0.85)";
			ctx.strokeText(label, l.left + 4, ly);
			ctx.lineWidth = 1;
			ctx.strokeStyle = "rgba(143,155,184,0.9)";
			ctx.fillText(label, l.left + 4, ly);
			if (hz > 0) {
				ctx.save();
				ctx.strokeStyle = "rgba(255,255,255,0.07)";
				ctx.beginPath();
				ctx.moveTo(l.left, py);
				ctx.lineTo(l.left + l.pw, py);
				ctx.stroke();
				ctx.restore();
			}
		}
	}
	ctx.textAlign = "center";
	ctx.fillStyle = "#c9d3ea";

	ctx.save();
	ctx.beginPath();
	ctx.rect(l.left, l.top, l.pw, l.ph);
	ctx.clip();
	if (v.options.showLandmarks) {
		const lines = v.options.landmarks;
		const colours = v.options.landmarkColors;
		ctx.lineWidth = 1;
		for (let i = 0, n = 0; i + 3 < lines.length; i += 4, n++) {
			ctx.strokeStyle = colours[n] || "rgba(124,240,178,0.65)";
			ctx.beginPath();
			ctx.moveTo(x(lines[i]), y(lines[i + 1]));
			ctx.lineTo(x(lines[i + 2]), y(lines[i + 3]));
			ctx.stroke();
		}
	}
	if (v.options.showKeyPoints) {
		const points = v.options.keyPoints;
		ctx.fillStyle = "#4cc2ff";
		ctx.strokeStyle = "#06202e";
		for (let i = 0; i + 2 < points.length; i += 3) {
			ctx.beginPath();
			ctx.arc(x(points[i]), y(points[i + 1]), 2 + points[i + 2] * 2.5, 0, Math.PI * 2);
			ctx.fill();
			ctx.stroke();
		}
	}
	const now = v.el.currentTime || 0;
	const px = x(Math.min(now, l.seconds));
	ctx.strokeStyle = "#ffffff";
	ctx.lineWidth = 1.5;
	ctx.beginPath();
	ctx.moveTo(px, l.top);
	ctx.lineTo(px, l.top + l.ph);
	ctx.stroke();
	ctx.restore();
}
