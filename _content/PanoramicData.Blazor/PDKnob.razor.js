export { registerAudioControlEvents } from "./js/pdaudio/pdaudio-pointer-drag.js";

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
