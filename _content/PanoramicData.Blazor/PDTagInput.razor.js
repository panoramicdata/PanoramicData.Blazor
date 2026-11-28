// Escape closes an open suggestion list and goes no further, so a dialog around the box stays open.
// Blazor handles events at the document, after a dialog's own listener has already seen them, so this stops it first.
export function initialize(root, dotNetRef) {
	if (!root || root._pdTagInputEscape) {
		return;
	}

	root._pdTagInputEscape = (event) => {
		if (event.key === "Escape" && root.querySelector(".pd-taginput-dropdown")) {
			event.stopPropagation();
			dotNetRef.invokeMethodAsync("CloseSuggestions");
		}
	};
	root.addEventListener("keydown", root._pdTagInputEscape);
}
