// PDMediaPlayer: turns decoded audio into a spectrogram image (short-time Fourier transform and colour ramp).

const silenceFloor = Math.pow(10, -12);
const dynamicRangeDb = 80;

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
export function heatColour(level) {
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

export function makeSpectrogram(decoded, options) {
	const analysis = analyseFrames(
		mixToMono(decoded),
		decoded.sampleRate,
		options,
	);
	const result = {
		image: paintSpectrogram(analysis),
		frames: analysis.frames,
		bins: analysis.bins,
		hzPerBin: analysis.hzPerBin,
		seconds: (analysis.frames * options.hopSize) / decoded.sampleRate,
		maxHz: analysis.maxHz,
	};
	return result;
}
