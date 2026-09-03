var unloadListener = false;
var unloadListenerIds = {};

export function beforeUnloadListener(event) {
	event.preventDefault();
	return event.returnValue = "Exit and lose changes?";
}

export function removeUnloadListener () {
	if (unloadListener) {
		removeEventListener("beforeunload", beforeUnloadListener, { capture: true });
		unloadListener = false;
	}
	unloadListenerIds = {};
}

export function setUnloadListener (id, changesMade) {

	// update dictionary
	if (changesMade) {
		unloadListenerIds[id] = true;
	} else {
		delete unloadListenerIds[id];
	}

	// add or remove unload listener
	var listenerCount = Object.keys(unloadListenerIds).length;
	if (listenerCount > 0 && !unloadListener) {
		addEventListener("beforeunload", beforeUnloadListener, { capture: true });
		unloadListener = true;
	}
	else if (listenerCount === 0 && unloadListener) {
		removeEventListener("beforeunload", beforeUnloadListener, { capture: true });
		unloadListener = false;
	}
}