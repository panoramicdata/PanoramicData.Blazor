export function addClass (id, cls) {
	var el = document.getElementById(id);
	if (el)
		el.classList.add(cls);
}

export function click(id) {
	var el = document.getElementById(id);
	if (el && el.click) {
		el.click();
	}
}

export function debounce (func, wait) {
	let timeout;
	return function executedFunction(...args) {
		const later = () => {
			timeout = null;
			func(...args);
		};
		clearTimeout(timeout);
		timeout = setTimeout(later, wait);
	};
}

export function focus(id) {
	var node = document.getElementById(id);
	if (node && node.focus) {
		node.focus();
		return true;
	}
	return false;
}

export function getElementAtPoint (x, y) {
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

export function getElementInfo(el) {
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

export function getFocusedElementId() {
	return document.activeElement.id;
}

export function isTouchDevice () {
	return (('ontouchstart' in window) ||
		(navigator.maxTouchPoints > 0) ||
		(navigator.msMaxTouchPoints > 0));
}

export function removeClass(id, cls) {
	var el = document.getElementById(id);
	if (el)
		el.classList.remove(cls);
}

export function scrollIntoView(id, alignTop) {
	var el = document.getElementById(id);
	if (el)
		el.scrollIntoView(alignTop);
}

export function selectText(id, start, end) {
	var node = document.getElementById(id);
	if (!node) return;
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