export function registerEndDragOperation(element, dotNetRef) {
	function isOutsideElement(event) {
		return element && !element.contains(event.target);
	}

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

export function registerDragEnterOperation(element, dotNetRef) {
	if (!element) return;

	element.addEventListener('dragenter', function (event) {
		event.preventDefault();
		dotNetRef.invokeMethodAsync('RegisterDestination');
  });
}