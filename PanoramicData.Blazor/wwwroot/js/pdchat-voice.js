// PDChat Voice Mode: the microphone to a host websocket at 24 kHz, and spoken answers played as they arrive.

const SAMPLE_RATE = 24000;

// Beside this module; a separate file rather than a Blob so a Content Security Policy need not allow blob: scripts.
const CAPTURE_MODULE = import.meta.url.replace(
	/[^/]*$/,
	"pdchat-voice-capture.js",
);

let listening = null;
let speaking = null;

function toWebSocketUrl(url) {
	if (/^wss?:\/\//i.test(url)) {
		return url;
	}
	if (/^https?:\/\//i.test(url)) {
		return url.replace(/^http/i, "ws");
	}
	const scheme = location.protocol === "https:" ? "wss://" : "ws://";
	return scheme + location.host + (/^\//.test(url) ? url : "/" + url);
}

function onListenMessage(event, dotNet) {
	if (typeof event.data !== "string") {
		return;
	}
	const message = JSON.parse(event.data);
	if (message.type === "word") {
		dotNet.invokeMethodAsync("OnVoiceWord", message.text);
	} else if (message.type === "turn") {
		dotNet.invokeMethodAsync("OnVoiceTurn", message.text);
	} else if (message.type === "error") {
		dotNet.invokeMethodAsync(
			"OnVoiceError",
			message.text || "Voice Mode is unavailable.",
		);
	}
}

/** Starts listening. Rejects when the microphone is refused or unavailable. */
export async function start(listenUrl, dotNet) {
	stop();
	const stream = await navigator.mediaDevices.getUserMedia({
		audio: {
			channelCount: 1,
			echoCancellation: true,
			noiseSuppression: true,
			autoGainControl: true,
		},
	});
	const context = new AudioContext();
	await context.audioWorklet.addModule(CAPTURE_MODULE);

	const capture = new AudioWorkletNode(context, "pdchat-voice-capture");
	const socket = new WebSocket(toWebSocketUrl(listenUrl));
	socket.binaryType = "arraybuffer";
	const state = { stream, context, socket, paused: false };

	capture.port.onmessage = (event) => {
		if (!state.paused && socket.readyState === WebSocket.OPEN) {
			socket.send(event.data);
		}
	};
	socket.onmessage = (event) => onListenMessage(event, dotNet);
	socket.onclose = () => {
		if (listening === state) {
			listening = null;
			dotNet.invokeMethodAsync("OnVoiceClosed");
		}
	};

	context.createMediaStreamSource(stream).connect(capture);
	listening = state;
}

/** While paused the microphone is still open but nothing is sent, so the assistant does not hear itself. */
export function pause(paused) {
	if (listening) {
		listening.paused = paused;
	}
}

/** Stops listening and speaking, and releases the microphone. */
export function stop() {
	stopSpeaking();
	if (!listening) {
		return;
	}
	const state = listening;
	listening = null;
	state.socket.close();
	state.stream.getTracks().forEach((track) => track.stop());
	state.context.close();
}

/** Stops an answer part-way through. */
export function stopSpeaking() {
	if (!speaking) {
		return;
	}
	const state = speaking;
	speaking = null;
	state.socket.close();
	state.context.close();
	state.finish();
}

/** Speaks an answer, and calls OnVoiceSpoken once the last of the audio has played. */
export function speak(speakUrl, text, dotNet) {
	stopSpeaking();
	const context = new AudioContext({ sampleRate: SAMPLE_RATE });
	const socket = new WebSocket(toWebSocketUrl(speakUrl));
	socket.binaryType = "arraybuffer";
	const state = { context, socket, startAt: 0, last: null, finished: false };
	state.finish = () => {
		if (!state.finished) {
			state.finished = true;
			dotNet.invokeMethodAsync("OnVoiceSpoken");
		}
	};

	socket.onopen = () => socket.send(JSON.stringify({ text }));
	socket.onmessage = (event) => {
		if (typeof event.data === "string") {
			if (JSON.parse(event.data).type === "done") {
				finishAfterPlayback(state);
			}
			return;
		}
		queueAudio(state, new Float32Array(event.data));
	};
	socket.onerror = () => finishAfterPlayback(state);
	socket.onclose = () => finishAfterPlayback(state);
	speaking = state;
}

function queueAudio(state, samples) {
	if (samples.length === 0) {
		return;
	}
	const buffer = state.context.createBuffer(1, samples.length, SAMPLE_RATE);
	buffer.copyToChannel(samples, 0);
	const source = state.context.createBufferSource();
	source.buffer = buffer;
	source.connect(state.context.destination);
	state.startAt = Math.max(state.startAt, state.context.currentTime + 0.05);
	source.start(state.startAt);
	state.startAt += buffer.duration;
	state.last = source;
}

function finishAfterPlayback(state) {
	const done = () => {
		if (speaking === state) {
			speaking = null;
			state.context.close();
		}
		state.finish();
	};
	// A short answer may already have finished playing, and then onended will not fire again.
	if (
		state.last &&
		!state.finished &&
		state.context.currentTime < state.startAt
	) {
		state.last.onended = done;
	} else {
		done();
	}
}
