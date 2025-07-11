export function registerValidDragOperationListeners(element, dotNetRef) {
	document.addEventListener('drop',
		function (_) { dotNetRef.invokeMethodAsync("InitiateTransformAsync") });
}

export function registerInvalidDragOperationListeners(element, dotNetRef) {
	document.addEventListener('dragstart', function (event) {
		if (isOutsideElement(event)) {
			dotNetRef.invokeMethodAsync('EndDragOperation');
		}
	});

	document.addEventListener('mouseup',
		function (_) { dotNetRef.invokeMethodAsync("EndDragOperation") });

	document.addEventListener('mouseleave',
		function (_) { dotNetRef.invokeMethodAsync("EndDragOperation") });
}

function isOutsideElement(event, element) {
	return element && !element.contains(event.target);
}