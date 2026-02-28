// Use a WeakMap to track listeners per dotNetRef instance
const listenerMap = new WeakMap();

export function registerAudioControlEvents(dotNetRef) {
    // Clean up any existing listeners for this specific control
    const existingListeners = listenerMap.get(dotNetRef);
    if (existingListeners) {
        document.removeEventListener("pointermove", existingListeners.onPointerMove);
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
