var _recognition = null;
var _ref = null;

export function abortListenForSpeech() {
	if (_recognition) {
		_recognition.abort();
	}
}

export function initSpeech(lang) {
	if (_recognition) {
		return;
	}
	const Recognition =
		window.SpeechRecognition || window.webkitSpeechRecognition;
	if (!Recognition) {
		return;
	}
	_recognition = new Recognition();
	if (lang) {
		_recognition.lang = lang; // "en-GB"
	}
	_recognition.addEventListener("result", onSpeechResult);
	_recognition.addEventListener("audiostart", onAudioStart);
	_recognition.addEventListener("audioend", onAudioEnd);
}

export function startListenForSpeech(ref) {
	if (_recognition) {
		try {
			_ref = ref;
			_recognition.start();
		} catch {
			// start() throws if recognition is already running; nothing more to do
		}
	}
}

export function termSpeech() {
	if (_recognition) {
		_recognition.abort();
		_recognition.removeEventListener("result", onSpeechResult);
		_recognition.removeEventListener("audiostart", onAudioStart);
		_recognition.removeEventListener("audioend", onAudioEnd);
	}
}

// Calls the .NET text box that is listening, if any
function invokeListener(method, ...args) {
	if (_ref) {
		_ref.invokeMethodAsync(method, ...args);
	}
}

function onAudioEnd() {
	invokeListener("OnListeningStopped");
}

function onAudioStart() {
	invokeListener("OnListeningStarted");
}

// Returns the transcript of the first alternative of the first result, if any
function getFirstTranscript(evt) {
	const results = evt?.results?.[0];
	return results?.length > 0 ? results[0].transcript : null;
}

function onSpeechResult(evt) {
	var transcript = getFirstTranscript(evt);
	if (transcript) {
		invokeListener("OnSpeechResult", transcript);
	}
}
