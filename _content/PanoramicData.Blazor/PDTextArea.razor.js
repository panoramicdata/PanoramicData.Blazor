export function initTextArea(id, ref) {
	const textarea = document.getElementById(id);
	if (textarea) {
		textarea.ref = ref;
		textarea.addEventListener('input', checkSelection);
		textarea.addEventListener('mouseup', checkSelection);
		textarea.addEventListener('keyup', checkSelection);
	}
}

export function termTextArea(id) {
	const textarea = document.getElementById(id);
	if (textarea) {
		textarea.removeEventListener('input', checkSelection);
		textarea.removeEventListener('mouseup', checkSelection);
		textarea.removeEventListener('keyup', checkSelection);
		delete textarea.ref;
	}
}

function checkSelection(evt) {
	var textarea = evt.srcElement;
	if (textarea) {
		const selectionStart = textarea.selectionStart;
		const selectionEnd = textarea.selectionEnd;
		if (selectionStart == selectionEnd) {
			if (textarea.ref) {
				textarea.ref.invokeMethodAsync("OnSelectionChanged", selectionStart, selectionEnd, "");
			}
		} else {
			var value = textarea.value.substring(selectionStart, selectionEnd);
			if (textarea.ref) {
				textarea.ref.invokeMethodAsync("OnSelectionChanged", selectionStart, selectionEnd, value);
			}
		}
	}
}

export function setSelection(id, start, end) {
	const textarea = document.getElementById(id);
	if (textarea) {
		textarea.selectionStart = start;
		textarea.selectionEnd = end;
		textarea.focus();
		var value = textarea.value.substring(start, end);
		if (textarea.ref) {
			textarea.ref.invokeMethodAsync("OnSelectionChanged", start, end, value);
		}
	}
}