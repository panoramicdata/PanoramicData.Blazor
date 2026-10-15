// Detaches the models from a BlazorMonaco diff editor, then disposes the editor, so that the models can
// be disposed afterwards without Monaco logging "TextModel got disposed before DiffEditorWidget model got reset".
export function releaseEditor(id) {
	const holder = window.blazorMonaco && window.blazorMonaco.editors.find((h) => h.id === id);
	if (!holder) {
		return;
	}

	holder.editor.setModel(null);
	holder.editor.dispose();
}
