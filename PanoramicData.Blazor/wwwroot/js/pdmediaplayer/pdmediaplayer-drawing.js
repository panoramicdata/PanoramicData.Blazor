// PDMediaPlayer: drawing the background, the axes and the overlays of a visualisation onto its canvas.

import {
	formatClock,
	formatTime,
	frequencyStep,
	niceStep,
} from "./pdmediaplayer-axes.js";

const tinyAmount = Math.pow(10, -9);
const defaultLandmarkColour = "rgba(124,240,178,0.65)";

export function drawBackground(ctx, v, l) {
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

export function drawTimeAxis(ctx, v, l, sc) {
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

export function drawFrequencyAxis(ctx, l, sc) {
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

export function drawOverlays(ctx, v, l, sc) {
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
