// PanoramicData.Blazor/PDComboBox.razor.js
export function blurInput(element) {
	if (element) element.blur();
}

export function scrollIntoView(element) {
	if (element) element.scrollIntoView({ block: "nearest" });
}

export function selectInputText(element) {
	if (element) {
		element.select();
	}
}