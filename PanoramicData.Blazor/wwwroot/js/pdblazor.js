function hasSplitJs() {
	return typeof Split !== 'undefined';
}

function initializeSplitter(ids, options) {
	Split(ids, options);
}

function hasPopperJs() {
	return typeof Popper !== 'undefined';
}

function showMenu(menuId, x, y) {
	var menu = document.getElementById(menuId);
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
		placement: 'bottom-start'
	};
	menu.classList.add("show");

	var popper = Popper.createPopper(reference, menu, options);
	document.addEventListener("click", function (event) {
		let isClickInside = menu.contains(event.target);
		if (!isClickInside) {
			menu.classList.remove("show");
			popper.destroy();
		}
	});
}

function hideMenu(menuId) {
	var menu = document.getElementById(menuId);
	menu.classList.remove("show");
}