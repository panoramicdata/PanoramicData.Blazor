using BlazorMonaco.Editor;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMonaco
{
	private StandaloneCodeEditor? _monacoEditor;

	private StandaloneEditorConstructionOptions _monacoOptions = new StandaloneEditorConstructionOptions
	{
		AutomaticLayout = true,
		Language = "SQL"
	};

	private StandaloneEditorConstructionOptions GetOptions(StandaloneCodeEditor editor)
	{
		return _monacoOptions;
	}

	private async Task OnMonacoEditorBlurAsync()
	{
		if (_monacoEditor != null)
		{
			var model = await _monacoEditor.GetModel();
			var value = await model.GetValue(EndOfLinePreference.CRLF, true);
		}
	}

	private async Task OnMonacoInitAsync()
	{
		if (_monacoEditor != null)
		{
			var model = await _monacoEditor.GetModel();
			var value = "SELECT 10 * 10 FROM [Temp]";
			await model.SetValue(value);
		}
	}

	private void OnHostResize()
	{
		_monacoEditor?.Layout();
	}

}
