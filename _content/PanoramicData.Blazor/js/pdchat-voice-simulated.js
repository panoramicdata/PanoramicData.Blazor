// PDChat Voice Mode, simulated: hears THAT you are talking, never WHAT, and makes the words up.
// Same exports as pdchat-voice.js. Audio never leaves the browser.

const PHRASES = [
	"I know this is not what you are actually saying, but to use real voice recognition you need to hook in a streaming speech service.",
	"blah blah blah blah blah blah",
	"This is a simulated transcript, and none of your words were sent anywhere.",
	"Pretend I just asked something really clever.",
	"Supply VoiceEndpoints from your chat service to hear the real thing.",
	"Testing, testing, one two three. Is this thing on?",
];

// Every other utterance starts with the demo's wake phrase, so a dormant microphone can be seen to wake.
const WAKE_PHRASE = "Hey DumbBot,";

const LOUD = 0.02;
const WORD_MS = 220;
const PAUSE_MS = 700;

let listening = null;
let utteranceIndex = 0;
let wordIndex = 0;

function utteranceWords(index) {
	const phrase = PHRASES[index % PHRASES.length];
	return (index % 2 === 0 ? `${WAKE_PHRASE} ${phrase}` : phrase).split(" ");
}

function nextWord() {
	const words = utteranceWords(utteranceIndex);
	const word = words[wordIndex];
	wordIndex++;
	if (wordIndex >= words.length) {
		wordIndex = 0;
		utteranceIndex = (utteranceIndex + 1) % (PHRASES.length * 2);
	}
	return word;
}

function level(analyser, samples) {
	analyser.getFloatTimeDomainData(samples);
	let sum = 0;
	for (let i = 0; i < samples.length; i++) {
		sum += samples[i] * samples[i];
	}
	return Math.sqrt(sum / samples.length);
}

function tick(state, dotNet) {
	if (state.paused) {
		return;
	}
	const now = Date.now();
	if (level(state.analyser, state.samples) > LOUD) {
		state.lastLoud = now;
		if (now - state.lastWord >= WORD_MS) {
			state.lastWord = now;
			const word = nextWord();
			state.heard.push(word);
			dotNet.invokeMethodAsync("OnVoiceWord", word);
		}
	} else if (state.heard.length > 0 && now - state.lastLoud >= PAUSE_MS) {
		const text = state.heard.join(" ");
		state.heard = [];
		dotNet.invokeMethodAsync("OnVoiceTurn", text);
	}
}

/** Starts listening. Rejects when the microphone is refused or unavailable. */
export async function start(listenUrl, dotNet) {
	stop();
	const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
	const context = new AudioContext();
	const analyser = context.createAnalyser();
	analyser.fftSize = 1024;
	context.createMediaStreamSource(stream).connect(analyser);
	const state = {
		stream,
		context,
		analyser,
		samples: new Float32Array(analyser.fftSize),
		paused: false,
		heard: [],
		lastLoud: 0,
		lastWord: 0,
	};
	state.timer = setInterval(() => tick(state, dotNet), 50);
	listening = state;
}

/** While paused nothing is "heard", so the assistant does not hear itself. */
export function pause(paused) {
	if (listening) {
		listening.paused = paused;
		listening.heard = [];
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
	clearInterval(state.timer);
	state.stream.getTracks().forEach((track) => track.stop());
	state.context.close();
}

/** Stops an answer part-way through. */
export function stopSpeaking() {
	if (globalThis.speechSynthesis) {
		globalThis.speechSynthesis.cancel();
	}
}

/** Speaks an answer with the browser's own voice, and calls OnVoiceSpoken when it ends. */
export function speak(speakUrl, text, dotNet) {
	stopSpeaking();
	let finished = false;
	const finish = () => {
		if (!finished) {
			finished = true;
			dotNet.invokeMethodAsync("OnVoiceSpoken");
		}
	};
	if (!globalThis.speechSynthesis) {
		setTimeout(finish, 60 * text.length);
		return;
	}
	const utterance = new SpeechSynthesisUtterance(text);
	utterance.onend = finish;
	utterance.onerror = finish;
	globalThis.speechSynthesis.speak(utterance);
}
