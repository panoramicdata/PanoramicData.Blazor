var _recognition = null;
var _ref = null;

export function abortListenForSpeech() {
	if (_recognition) {
		_recognition.abort();
	}
}

export function initSpeech(lang) {
	if (!_recognition) {
		if (window.SpeechRecognition) {
			_recognition = new window.SpeechRecognition();
		} else if (window.webkitSpeechRecognition) {
			_recognition = new window.webkitSpeechRecognition();
		}
		if (_recognition) {
			if (lang && lang != "") {
				_recognition.lang = lang; // "en-GB"
			}
			_recognition.addEventListener("result", onSpeechResult);
			_recognition.addEventListener("audiostart", onAudioStart);
			_recognition.addEventListener("audioend", onAudioEnd);
		}
	}
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

function onAudioEnd() {
	if (_ref) {
		_ref.invokeMethodAsync("OnListeningStopped");
	}
}

function onAudioStart() {
	if (_ref) {
		_ref.invokeMethodAsync("OnListeningStarted");
	}
}

// Returns the transcript of the first alternative of the first result, if any
function getFirstTranscript(evt) {
	if (!evt || !evt.results || !(evt.results.length > 0)) {
		return null;
	}
	var results = evt.results[0];
	return results.length > 0 ? results[0].transcript : null;
}

function onSpeechResult(evt) {
	if (!_ref) {
		return;
	}
	var transcript = getFirstTranscript(evt);
	if (transcript) {
		_ref.invokeMethodAsync("OnSpeechResult", transcript);
	}
}
