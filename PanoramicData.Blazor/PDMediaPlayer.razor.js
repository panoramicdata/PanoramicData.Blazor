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
