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
	document.addEventListener("mousedown", function (event) {
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

function focus(id) {
	var node = document.getElementById(id);
	if (node && node.focus) {
		node.focus();
	}
}

function selectText(id, start, end) {
	var node = document.getElementById(id);
	if (!start) start = 0;
	if (!end) end = node.value.length;
	if (node.createTextRange) {
		var selRange = node.createTextRange();
		selRange.collapse(true);
		selRange.moveStart('character', start);
		selRange.moveEnd('character', end);
		selRange.select();
		node.focus();
	} else if (node.setSelectionRange) {
		node.focus();
		node.setSelectionRange(start, end);
	} else if (typeof node.selectionStart != 'undefined') {
		node.selectionStart = start;
		node.selectionEnd = end;
		node.focus();
	}
}

function getFocusedElementId() {
	return document.activeElement.id;
}