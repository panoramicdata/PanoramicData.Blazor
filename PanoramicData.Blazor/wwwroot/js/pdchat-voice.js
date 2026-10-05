// PDChat Voice Mode: the microphone to a host websocket at 24 kHz, and spoken answers played as they arrive.

const SAMPLE_RATE = 24000;
const FRAME_SAMPLES = 1920;

// Resampled in the worklet from the device's own rate: a 24 kHz AudioContext cannot be connected to a
// microphone in every browser.
const CAPTURE_WORKLET = `
class PdChatVoiceCapture extends AudioWorkletProcessor {
	constructor() {
		super();
		this.step = sampleRate / ${SAMPLE_RATE};
		this.position = 0;
		this.frame = new Float32Array(${FRAME_SAMPLES});
		this.length = 0;
	}
	process(inputs) {
		const input = inputs[0] && inputs[0][0];
		if (!input) {
			return true;
		}
		while (this.position < input.length) {
			const index = Math.floor(this.position);
			const next = Math.min(index + 1, input.length - 1);
			const fraction = this.position - index;
			this.frame[this.length++] = input[index] + (input[next] - input[index]) * fraction;
			if (this.length === ${FRAME_SAMPLES}) {
				this.port.postMessage(this.frame.buffer, [this.frame.buffer]);
				this.frame = new Float32Array(${FRAME_SAMPLES});
				this.length = 0;
			}
			this.position += this.step;
		}
		this.position -= input.length;
		return true;
	}
}
registerProcessor("pdchat-voice-capture", PdChatVoiceCapture);
`;

let listening = null;
let speaking = null;

function toWebSocketUrl(url) {
	const absolute = new URL(url, document.baseURI);
	absolute.protocol = absolute.protocol === "https:" ? "wss:" : "ws:";
	return absolute.toString();
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
	const workletUrl = URL.createObjectURL(
		new Blob([CAPTURE_WORKLET], { type: "text/javascript" }),
	);
	await context.audioWorklet.addModule(workletUrl);
	URL.revokeObjectURL(workletUrl);

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

/** While paused the microphone is still open but nothing is sent, so Merlin does not hear itself. */
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

/** Speaks an answer, resolving once the last of the audio has played. */
export function speak(speakUrl, text) {
	stopSpeaking();
	return new Promise((resolve) => {
		const context = new AudioContext({ sampleRate: SAMPLE_RATE });
		const socket = new WebSocket(toWebSocketUrl(speakUrl));
		socket.binaryType = "arraybuffer";
		const state = { context, socket, startAt: 0, last: null, finished: false };
		state.finish = () => {
			if (!state.finished) {
				state.finished = true;
				resolve();
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
	});
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
