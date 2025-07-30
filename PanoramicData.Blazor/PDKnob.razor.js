// PanoramicData.Blazor/PDKnob.razor.js

export function registerKnobEvents(dotNetRef) {
	function moveHandler(e) {
		dotNetRef.invokeMethodAsync('OnPointerMove', e.clientY);
	}
	function upHandler(e) {
		dotNetRef.invokeMethodAsync('OnPointerUp', e.clientY);
		window.removeEventListener('pointermove', moveHandler);
		window.removeEventListener('pointerup', upHandler);
	}
	window.addEventListener('pointermove', moveHandler);
	window.addEventListener('pointerup', upHandler);
}