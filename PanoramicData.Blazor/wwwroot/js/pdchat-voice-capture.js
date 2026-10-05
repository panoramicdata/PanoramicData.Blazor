// PDChat Voice Mode's capture worklet: the microphone, resampled from the device's own rate to 24 kHz, in 80 ms
// frames. A 24 kHz AudioContext cannot be connected to a microphone in every browser, so it is done here.

const TARGET_RATE = 24000;
const FRAME_SAMPLES = 1920;

class PdChatVoiceCapture extends AudioWorkletProcessor {
	constructor() {
		super();
		this.step = sampleRate / TARGET_RATE;
		this.position = 0;
		this.frame = new Float32Array(FRAME_SAMPLES);
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
			this.frame[this.length++] =
				input[index] + (input[next] - input[index]) * fraction;
			if (this.length === FRAME_SAMPLES) {
				this.port.postMessage(this.frame.buffer, [this.frame.buffer]);
				this.frame = new Float32Array(FRAME_SAMPLES);
				this.length = 0;
			}
			this.position += this.step;
		}
		this.position -= input.length;
		return true;
	}
}

registerProcessor("pdchat-voice-capture", PdChatVoiceCapture);
