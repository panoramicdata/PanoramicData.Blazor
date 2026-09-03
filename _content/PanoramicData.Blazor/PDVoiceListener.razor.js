let listenerReference = null;
let recognition = null;
let isListening = false;
let mode = "ManualActivation";
let runInBackground = true;
let shouldRestart = false;

function getSpeechRecognitionConstructor() {
	return window.SpeechRecognition || window.webkitSpeechRecognition || null;
}

function createRecognition() {
	const SpeechRecognitionCtor = getSpeechRecognitionConstructor();
	if (!SpeechRecognitionCtor) {
		if (listenerReference) {
			listenerReference.invokeMethodAsync("OnUnsupported");
		}
		return null;
	}

	const instance = new SpeechRecognitionCtor();
	instance.continuous = true;
	instance.interimResults = false;

	instance.onstart = () => {
		isListening = true;
		if (listenerReference) {
			listenerReference.invokeMethodAsync("OnListeningStarted");
		}
	};

	instance.onresult = (event) => {
		if (!listenerReference || !event || !event.results) {
			return;
		}

		for (let i = event.resultIndex; i < event.results.length; i++) {
			const result = event.results[i];
			if (!result || !result.isFinal || !result[0] || !result[0].transcript) {
				continue;
			}

			listenerReference.invokeMethodAsync("OnRecognizedText", result[0].transcript, new Date().toISOString());
		}
	};

	instance.onerror = (event) => {
		if (!listenerReference) {
			return;
		}

		const code = event && event.error ? event.error : null;
		const message = event && event.message ? event.message : null;
		if (code === "not-allowed" || code === "service-not-allowed") {
			shouldRestart = false;
			listenerReference.invokeMethodAsync("OnPermissionDenied");
			return;
		}

		listenerReference.invokeMethodAsync("OnListenerError", code, message);
	};

	instance.onend = () => {
		isListening = false;
		if (listenerReference) {
			listenerReference.invokeMethodAsync("OnListeningStopped");
		}

		if (shouldRestart && runInBackground && mode !== "ManualActivation") {
			startListening();
		}
	};

	return instance;
}

export function initialize(ref, options) {
	listenerReference = ref;
	mode = options && options.mode ? options.mode : "ManualActivation";
	runInBackground = options && options.runInBackground !== undefined ? options.runInBackground : true;
	recognition = createRecognition();
}

export function configure(options) {
	mode = options && options.mode ? options.mode : mode;
	runInBackground = options && options.runInBackground !== undefined ? options.runInBackground : runInBackground;
}

export function startListening() {
	if (!recognition) {
		recognition = createRecognition();
		if (!recognition) {
			return;
		}
	}

	if (isListening) {
		return;
	}

	shouldRestart = true;
	try {
		recognition.start();
	} catch {
	}
}

export function stopListening() {
	shouldRestart = false;
	if (!recognition || !isListening) {
		return;
	}

	try {
		recognition.stop();
	} catch {
	}
}

export function dispose() {
	shouldRestart = false;
	if (recognition) {
		try {
			recognition.onstart = null;
			recognition.onresult = null;
			recognition.onerror = null;
			recognition.onend = null;
			recognition.abort();
		} catch {
		}
	}

	recognition = null;
	isListening = false;
	listenerReference = null;
}
