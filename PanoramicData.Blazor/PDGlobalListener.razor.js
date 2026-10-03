var globalListenerReference = null;
var shortcutKeys = [];

export function initialize(ref) {
	globalListenerReference = ref;
	window.addEventListener("keydown", onKeyDown);
	window.addEventListener("keyup", onKeyUp);
}

export function dispose() {
	window.removeEventListener("keydown", onKeyDown);
	window.removeEventListener("keyup", onKeyUp);
	globalListenerReference = null;
}

export function registerShortcutKeys(shortcuts) {
	shortcutKeys = shortcuts || [];
}

const modifierKeys = ["altKey", "ctrlKey", "shiftKey"];

export function isShortcutKeyMatch(keyInfo) {
	return shortcutKeys.some(
		(v) =>
			modifierKeys.every((m) => v[m] == keyInfo[m]) &&
			(v.key.toLowerCase() == keyInfo.key.toLowerCase() ||
				v.code.toLowerCase() == keyInfo.code.toLowerCase()),
	);
}

function onKeyDown(e) {
	forwardKeyEvent(e, "OnKeyDown");
}

function onKeyUp(e) {
	forwardKeyEvent(e, "OnKeyUp");
}

function forwardKeyEvent(e, methodName) {
	if (globalListenerReference) {
		var keyInfo = getKeyArgs(e);
		if (isShortcutKeyMatch(keyInfo)) {
			e.stopPropagation();
			e.preventDefault();
		}
		try {
			// MS-24862: invokeMethodAsync returns a Promise that rejects asynchronously (e.g.
			// "Cannot send data if the connection is not in the 'Connected' State" once the
			// circuit has disconnected) - a synchronous try/catch alone does not observe that
			// rejection, so it must be handled on the returned promise too, or it surfaces as an
			// uncaught (in promise) error.
			globalListenerReference
				.invokeMethodAsync(methodName, keyInfo)
				.catch(() => {});
		} catch {
			// BC-85: Circuit may be disconnected
		}
	}
}

export function getKeyArgs(e) {
	var obj = {};
	obj.key = e.key;
	obj.code = e.code;
	obj.keyCode = e.keyCode;
	obj.altKey = e.altKey;
	obj.ctrlKey = e.ctrlKey;
	obj.shiftKey = e.shiftKey;
	return obj;
}
