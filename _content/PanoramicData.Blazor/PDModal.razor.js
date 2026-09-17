export function initialize(id, opt, ref) {
	var el = document.getElementById(id);
	if (el) {

		el.addEventListener("shown.bs.modal", function () {
			if (ref) {
				ref.invokeMethodAsync("OnModalShown");
			}
		});

		el.addEventListener("hidden.bs.modal", function () {
			if (ref) {
				ref.invokeMethodAsync("OnModalHidden");
			}
		});

		return new bootstrap.Modal(el, opt);
	}
}

export function cleanupBackdrops() {
	// Bootstrap removes the .modal-backdrop only on its animated 'hidden.bs.modal' event, which can lose
	// the race against a page teardown - orphaning the backdrop on <body> so it blocks the whole page.
	// Sweep any stray backdrops and restore <body> synchronously so hide-on-navigation is deterministic.
	try {
		document.querySelectorAll("body > .modal-backdrop").forEach(function (el) { el.remove(); });
		document.body.classList.remove("modal-open");
		document.body.style.removeProperty("overflow");
		document.body.style.removeProperty("padding-right");
	} catch (e) {
		// Never let backdrop cleanup throw.
	}
}