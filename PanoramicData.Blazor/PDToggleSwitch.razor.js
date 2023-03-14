export function measureText(id, onText, offText, className) {
	if ((!onText || onText.length === 0) && (!offtext || offText.length == 0)) {
		return 0;
	}
	return 0;
	var el = document.getElementById(id);
	if (el) {
		var svgEl = document.createElement('svg');
		if (className) {
			svgEl.attr('class', className);
		}

		var textEl = document.createElement('text', { x: -1000, y: -1000, text: onText});

		//textEl.attr({ x: -1000, y: -1000 }).text(onText);
		//document.getElementById('yourTextId').getComputedTextLength();

		svgEl.appendChild(textEl);
		el.appendChild(svgEl);
		var len1 = textEl.getComputedTextLength();

		textEl.text(offText);
		var len2 = textEl.getComputedTextLength();

		textEl.remove();
		svgEl.remove();

		return Math.max(len1, len2);
	}
	return 0;
}