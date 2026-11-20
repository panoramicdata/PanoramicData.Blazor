// PDMediaPlayer: choosing and formatting the ticks of the time and frequency axes.

const timeSteps = [
	0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800,
	3600,
];

export function niceStep(span, pixels, minPixels) {
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
export function formatTime(seconds, step) {
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
export function formatClock(wallSeconds, step) {
	const decimals = decimalPlaces(step);
	const day = ((wallSeconds % 86400) + 86400) % 86400;
	const h = String(Math.floor(day / 3600)).padStart(2, "0");
	const m = String(Math.floor((day % 3600) / 60)).padStart(2, "0");
	if (step >= 60) {
		return h + ":" + m;
	}
	return h + ":" + m + ":" + zeroPad((day % 60).toFixed(decimals), decimals);
}

export function frequencyStep(spanHz) {
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

export function scales(l) {
	const hzSpan = l.maxHz - l.minHz;
	const result = {
		x: (t) => l.left + ((t - l.start) / l.seconds) * l.pw,
		y: (hz) => l.top + l.ph - ((hz - l.minHz) / hzSpan) * l.ph,
	};
	return result;
}
