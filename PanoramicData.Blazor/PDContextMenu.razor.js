var menuEl = null;
var popper = null;

export function hasPopperJs() {
	return typeof Popper !== "undefined";
}

export function showMenu(menuId, x, y) {
	menuEl = document.getElementById(menuId);
	if (menuEl) {
		var reference = {
			getBoundingClientRect() {
				return {
					width: 0,
					height: 0,
					top: y,
					bottom: y,
					left: x,
					right: x,
				};
			},
		};
		// Fixed, so the menu opens at the pointer wherever it sits: under CSS zoom or a scaled parent, and in a panel that
		// scrolls or clips. "positionFixed" was popper 1's option, which popper 2 ignores
		var options = {
			placement: "bottom-start",
			strategy: "fixed",
		};
		menuEl.classList.add("show");
		popper = window.Popper.createPopper(reference, menuEl, options); // this is popper v2.4.4 syntax
		document.addEventListener("mousedown", documentMouseDown);
	}
}

export function hideMenu() {
	if (menuEl) {
		menuEl.classList.remove("show");
		document.removeEventListener("mousedown", documentMouseDown);
		if (popper) {
			popper.destroy();
		}
	}
}

export function documentMouseDown(event) {
	if (menuEl && popper) {
		let isClickInside = menuEl.contains(event.target);
		if (!isClickInside) {
			hideMenu();
		}
	}
}
