export function registerValidDragOperationListeners(element, dotNetRef) {
	document.addEventListener('drop',
		function (_) { dotNetRef.invokeMethodAsync("InitiateTransformAsync") });
}

export function registerInvalidDragOperationListeners(element, dotNetRef) {
	document.addEventListener('dragstart', function (event) {
		if (isOutsideElement(event)) {
			dotNetRef.invokeMethodAsync('EndDragOperationAsync');
		}
	});

	document.addEventListener('mouseup',
		function (_) { dotNetRef.invokeMethodAsync("EndDragOperationAsync") });

	document.addEventListener('mouseleave',
		function (_) { dotNetRef.invokeMethodAsync("EndDragOperationAsync") });
}

function isOutsideElement(event, element) {
	return element && !element.contains(event.target);
}