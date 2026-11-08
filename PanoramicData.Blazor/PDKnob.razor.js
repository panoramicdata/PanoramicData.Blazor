// Use a WeakMap to track listeners per dotNetRef instance
const listenerMap = new WeakMap();

export function registerAudioControlEvents(dotNetRef) {
	// Clean up any existing listeners for this specific control
	const existingListeners = listenerMap.get(dotNetRef);
	if (existingListeners) {
		document.removeEventListener(
			"pointermove",
			existingListeners.onPointerMove,
		);
		document.removeEventListener("pointerup", existingListeners.onPointerUp);
	}

	const onPointerMove = (e) => {
		dotNetRef.invokeMethodAsync("OnPointerMove", e.clientY);
	};

	const onPointerUp = (e) => {
		document.removeEventListener("pointermove", onPointerMove);
		document.removeEventListener("pointerup", onPointerUp);
		listenerMap.delete(dotNetRef);
		dotNetRef.invokeMethodAsync("OnPointerUp", e.clientY);
	};

	// Store reference to listeners for this control instance
	listenerMap.set(dotNetRef, { onPointerMove, onPointerUp });

	document.addEventListener("pointermove", onPointerMove);
	document.addEventListener("pointerup", onPointerUp);
}

// Angle mode: reports pointer offsets from the dial's centre, with Shift to move freely.
const angleListenerMap = new WeakMap();

export function registerAngleEvents(dotNetRef, element, clientX, clientY) {
	const existing = angleListenerMap.get(dotNetRef);
	if (existing) {
		document.removeEventListener("pointermove", existing.onPointerMove);
		document.removeEventListener("pointerup", existing.onPointerUp);
	}

	const offset = (x, y) => {
		const rect = element.getBoundingClientRect();
		return [x - (rect.left + rect.width / 2), y - (rect.top + rect.height / 2)];
	};

	const onPointerMove = (e) => {
		const [x, y] = offset(e.clientX, e.clientY);
		dotNetRef.invokeMethodAsync("OnAnglePointerMove", x, y, e.shiftKey);
	};

	const onPointerUp = () => {
		document.removeEventListener("pointermove", onPointerMove);
		document.removeEventListener("pointerup", onPointerUp);
		angleListenerMap.delete(dotNetRef);
		dotNetRef.invokeMethodAsync("OnAnglePointerUp");
	};

	angleListenerMap.set(dotNetRef, { onPointerMove, onPointerUp });
	document.addEventListener("pointermove", onPointerMove);
	document.addEventListener("pointerup", onPointerUp);
	return offset(clientX, clientY);
}
