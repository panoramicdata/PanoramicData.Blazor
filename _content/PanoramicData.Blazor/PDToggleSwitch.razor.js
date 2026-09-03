export function measureText(pText, pFontSize, pStyle) {
	var lDiv = document.createElement('div');

	document.body.appendChild(lDiv);

	if (pStyle != null) {
		lDiv.style = pStyle;
	}
	lDiv.style.fontSize = "" + pFontSize;
	lDiv.style.position = "absolute";
	lDiv.style.left = -1000;
	lDiv.style.top = -1000;

	lDiv.textContent = pText;

	var lResult = {
		width: lDiv.clientWidth,
		height: lDiv.clientHeight
	};

	document.body.removeChild(lDiv);
	lDiv = null;

	return lResult.width;
}