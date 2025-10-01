export function clickClosest(id, cls)
{
	var el = document.getElementById(id);
	if (el)
	{
		var closest = el.closest(cls);
		if (closest)
		{
			closest.click();
		}
	}
}

export function addClass(id, cls)
{
	var el = document.getElementById(id);
	if (el) {
		el.classList.add(cls);
	}
}

export function click(id) {
	var el = document.getElementById(id);
	if (el && el.click) {
		el.click();
	}
}

export function confirm (msg) {
	return window.confirm(msg);
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

export function debounceInput (id, wait, objRef) {
	var el = document.getElementById(id);
	if (el) {
		var debouncedFunction = debounce(function (ev) {
			objRef.invokeMethodAsync("OnDebouncedInput", ev.srcElement.value)
		}, wait);
		el.addEventListener("input", debouncedFunction);
	}
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
		id: el.id || "",
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

export function getHeight(el) {
	var rect = el.getBoundingClientRect();
	return rect.height || 0;
}

export function getValue(id) {
	var node = document.getElementById(id);
	if (node) {
		return node.value;
	}
	return null;
}

export function getWidth(el) {
	var rect = el.getBoundingClientRect();
	return rect.width || 0;
}

export function getX(el) {
	var rect = el.getBoundingClientRect();
	return rect.left || 0;
}

export function getY(el) {
	var rect = el.getBoundingClientRect();
	return rect.top || 0;
}

export function isTouchDevice () {
	return (("ontouchstart" in window) ||
		(navigator.maxTouchPoints > 0) ||
		(navigator.msMaxTouchPoints > 0));
}

export function openUrl(url, target) {
	window.open(url, target);
}

export function removeClass(id, cls) {
	var el = document.getElementById(id);
	if (el) {
		el.classList.remove(cls);
	}
}

export function scrollIntoView(id, alignTop) {
	var el = document.getElementById(id);
	if (el) {
		el.scrollIntoView(alignTop);
	}
}

export function scrollIntoViewEx(selector, behaviour, block, inline) {
	const el = document.querySelector(selector);
	if (el) {
		el.scrollIntoView({
			behavior: 'smooth', // or 'auto' for immediate scrolling
			block: 'nearest',   // aligns vertically (not usually needed for columns)
			inline: 'center'    // aligns horizontally
		});
	}
}

export function selectText(id, start, end) {
	var node = document.getElementById(id);
	if (!node) {
		return;
	}
	if (!start) {
		start = 0;
	}
	if (!end) {
		end = node.value.length;
	}
	if (node.createTextRange) {
		var selRange = node.createTextRange();
		selRange.collapse(true);
		selRange.moveStart("character", start);
		selRange.moveEnd("character", end);
		selRange.select();
		node.focus();
	} else if (node.setSelectionRange) {
		node.focus();
		node.setSelectionRange(start, end);
	} else if (typeof node.selectionStart != "undefined") {
		node.selectionStart = start;
		node.selectionEnd = end;
		node.focus();
	}
}

export function setPointerCapture(id, el) {
	el.setPointerCapture(id);
}

export function setValue (id, value) {
	var node = document.getElementById(id);
	if (node) {
		node.value = value;
	}
}

export function onTableDragStart(id) {
	var node = document.getElementById(id);
	if (node) {
		node.addEventListener("dragstart", onDragStart);
	}
}

export function clearInlineStyle(element) {
    if (element && element.style) {
        element.style.cssText = '';
    }
}

function scrollToBottom(element) {
	element.scrollTop = element.scrollHeight;
}

function onDragStart(evt) {
	var url = evt.target.getAttribute('data-downloadurl');
	if (url) {
		evt.dataTransfer.setData("DownloadURL", url);
	}
}