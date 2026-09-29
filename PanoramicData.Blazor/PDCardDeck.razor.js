// The document-level listeners each deck has added, keyed by the deck's element, so that they can be
// removed again when the deck is disposed.
const listenersByElement = new Map();

function addDocumentListener(element, type, handler) {
	let listeners = listenersByElement.get(element);
	if (!listeners) {
		listeners = [];
		listenersByElement.set(element, listeners);
	}

	listeners.push({ type, handler });
	document.addEventListener(type, handler);
}

export function registerValidDragOperationListeners(element, dotNetRef) {
	addDocumentListener(element, "drop", function () {
		dotNetRef.invokeMethodAsync("InitiateTransformAsync");
	});
}

export function registerInvalidDragOperationListeners(element, dotNetRef) {
	addDocumentListener(element, "dragstart", function (event) {
		if (isOutsideElement(event)) {
			dotNetRef.invokeMethodAsync("EndDragOperationAsync");
		}
	});

	addDocumentListener(element, "mouseup", function () {
		dotNetRef.invokeMethodAsync("EndDragOperationAsync");
	});

	addDocumentListener(element, "mouseleave", function () {
		dotNetRef.invokeMethodAsync("EndDragOperationAsync");
	});
}

// Removes every document-level listener registered for the given deck element.
export function unregisterListeners(element) {
	const listeners = listenersByElement.get(element);
	if (!listeners) {
		return;
	}

	for (const listener of listeners) {
		document.removeEventListener(listener.type, listener.handler);
	}

	listenersByElement.delete(element);
}

function isOutsideElement(event, element) {
	return element && !element.contains(event.target);
}
