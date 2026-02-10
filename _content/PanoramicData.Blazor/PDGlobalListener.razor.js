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

export function isShortcutKeyMatch(keyInfo) {
	var match = shortcutKeys.find((v) => v.altKey == keyInfo.altKey &&
		v.ctrlKey == keyInfo.ctrlKey &&
		v.shiftKey == keyInfo.shiftKey &&
		((v.key.toLowerCase() == keyInfo.key.toLowerCase()) || (v.code.toLowerCase() == keyInfo.code.toLowerCase())));
	return match ? true : false;
}

function onKeyDown(e) {
	if (globalListenerReference) {
		var keyInfo = getKeyArgs(e);
		if (isShortcutKeyMatch(keyInfo)) {
			e.stopPropagation();
			e.preventDefault();
		}
		try {
			globalListenerReference.invokeMethodAsync("OnKeyDown", keyInfo);
		} catch {
			// BC-85: Circuit may be disconnected
		}
	}
}

function onKeyUp(e) {
	if (globalListenerReference) {
		var keyInfo = getKeyArgs(e);
		if (isShortcutKeyMatch(keyInfo)) {
			e.stopPropagation();
			e.preventDefault();
		}
		try {
			globalListenerReference.invokeMethodAsync("OnKeyUp", keyInfo);
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