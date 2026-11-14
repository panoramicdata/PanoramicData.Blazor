var globalListenerReference = null;
var shortcutKeys = [];

// The physical keys held down, so Ctrl+Alt that Windows reports as AltGr can be told from the AltGr key itself
const heldCodes = new Set();

export function initialize(ref) {
	globalListenerReference = ref;
	window.addEventListener("keydown", onKeyDown);
	window.addEventListener("keyup", onKeyUp);
	window.addEventListener("blur", onBlur);
}

export function dispose() {
	window.removeEventListener("keydown", onKeyDown);
	window.removeEventListener("keyup", onKeyUp);
	window.removeEventListener("blur", onBlur);
	heldCodes.clear();
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
	heldCodes.add(e.code);
	forwardKeyEvent(e, "OnKeyDown");
}

function onKeyUp(e) {
	forwardKeyEvent(e, "OnKeyUp");
	heldCodes.delete(e.code);
}

// Keys released while the window is not focused never send their key-up
function onBlur() {
	heldCodes.clear();
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

// Notes the held keys too, so keys forwarded from where the window listener cannot hear them, such as an editor in an
// iframe, are told apart from AltGr the same way
export function getKeyArgs(e) {
	if (e.type === "keydown") {
		heldCodes.add(e.code);
	}

	var ctrlAlt = isCtrlAltReportedAsAltGr(e);
	var obj = {};
	obj.key = e.key;
	obj.code = e.code;
	obj.keyCode = e.keyCode;
	obj.altKey = e.altKey || ctrlAlt;
	obj.ctrlKey = e.ctrlKey || ctrlAlt;
	obj.shiftKey = e.shiftKey;
	if (e.type === "keyup") {
		heldCodes.delete(e.code);
	}

	return obj;
}

// On Windows, Ctrl+Alt with a key that types a character on the keyboard layout, such as Ctrl+Alt+O giving "ó", arrives
// as AltGr with Ctrl and Alt cleared. With the left Alt held it is the shortcut that was pressed; the AltGr key itself
// is the right Alt, and that is typing, so it is left as it is.
function isCtrlAltReportedAsAltGr(e) {
	return typeof e.getModifierState === "function" &&
		e.getModifierState("AltGraph") &&
		(heldCodes.has("ControlLeft") || heldCodes.has("ControlRight")) &&
		heldCodes.has("AltLeft") &&
		!heldCodes.has("AltRight");
}
