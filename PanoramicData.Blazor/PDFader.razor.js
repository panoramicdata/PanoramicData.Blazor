export function registerAudioControlEvents(dotNetRef) {
    const onPointerMove = (e) => {
        dotNetRef.invokeMethodAsync("OnPointerMove", e.clientY);
    };

    const onPointerUp = (e) => {
        document.removeEventListener("pointermove", onPointerMove);
        document.removeEventListener("pointerup", onPointerUp);
        dotNetRef.invokeMethodAsync("OnPointerUp", e.clientY);
    };

    document.addEventListener("pointermove", onPointerMove);
    document.addEventListener("pointerup", onPointerUp);
}
