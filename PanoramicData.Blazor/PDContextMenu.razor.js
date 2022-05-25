var menuEl = null;
var popper = null;

export function hasPopperJs() {
	return typeof Popper !== 'undefined';
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
					right: x
				};
			}
		};
		var options = {
			placement: 'bottom-start',
			positionFixed: true
		};
		menuEl.classList.add("show");
		popper = Popper.createPopper(reference, menuEl, options); // this is popper v2.4.4 syntax
		document.addEventListener("mousedown", documentMouseDown);
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

export function getElementAtPoint(x, y) {
	var el = document.elementFromPoint(x, y);
	if (el) {
		var baseInfo = getElementInfo(el);
		var info = baseInfo;
		el = el.parentElement;
		while (el) {
			info.parent = getElementInfo(el);
			info = info.parent;
			el = el.parentElement;
		}
		return baseInfo;
	}
	return null;
}

function getElementInfo(el) {
	var info = {
		id: el.id || '',
		tag: el.tagName,
		classList: []
	}
	for (var i = 0; i < el.classList.length; i++) {
		info.classList.push(el.classList[i]);
	}
	return info;
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