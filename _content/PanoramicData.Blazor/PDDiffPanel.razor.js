// Detaches the models from a BlazorMonaco diff editor, disposes the editor and removes its holders from
// BlazorMonaco's registry (createDiffEditor registers three: id, id_original and id_modified, and BlazorMonaco
// never removes them), so the models can be disposed afterwards without Monaco logging
// "TextModel got disposed before DiffEditorWidget model got reset" and the registry does not grow.
export function releaseEditor(id) {
	const editors = window.blazorMonaco && window.blazorMonaco.editors;
	if (!editors) {
		return;
	}

	const holder = editors.find((h) => h.id === id);
	if (holder) {
		holder.editor.setModel(null);
		holder.editor.dispose();
	}

	for (const holderId of [id, id + "_original", id + "_modified"]) {
		const index = editors.findIndex((h) => h.id === holderId);
		if (index >= 0) {
			editors.splice(index, 1);
		}
	}
}

// Replaces the text of the original and/or modified model (null leaves a side unchanged). Model.setValue resets the
// editors' scroll position, so the modified editor's scroll offsets are put back afterwards; the original side
// follows the modified side in a diff editor.
export function setTexts(id, original, modified) {
	const editors = window.blazorMonaco && window.blazorMonaco.editors;
	const holder = editors && editors.find((h) => h.id === id);
	if (!holder) {
		return;
	}

	const modifiedEditor = holder.editor.getModifiedEditor();
	const top = modifiedEditor.getScrollTop();
	const left = modifiedEditor.getScrollLeft();
	if (original !== null) {
		holder.editor.getOriginalEditor().getModel().setValue(original);
	}

	if (modified !== null) {
		modifiedEditor.getModel().setValue(modified);
	}

	modifiedEditor.setScrollTop(top);
	modifiedEditor.setScrollLeft(left);
}
