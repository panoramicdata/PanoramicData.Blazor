using BlazorMonaco.Editor;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMonaco : IAsyncDisposable
{
	private string _theme = "vs";
	private string _themePreference;
	private string _language = "sql";
	private IJSObjectReference? _module;
	private StandaloneCodeEditor? _monacoEditor;

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	private StandaloneEditorConstructionOptions _monacoOptions = new StandaloneEditorConstructionOptions
	{
		AutomaticLayout = true,
		Language = "SQL"
	};


	private StandaloneEditorConstructionOptions GetOptions(StandaloneCodeEditor editor)
	{
		_monacoOptions.Language = _language;
		_monacoOptions.Theme = _theme;
		return _monacoOptions;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			if (JSRuntime != null)
			{
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor.Demo/Pages/PDMonaco.razor.js");
				if (_module != null)
				{
					await _module.InvokeVoidAsync("configureMonaco");
				}
			}
		}
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
			var value = _language switch
			{
				"ncalc" => "10 * 10",
				"rmscript" => "[DateTime: value={QueryEndDate}, addmonths=-12, format=yyyy-MM-15, storeAsHidden=QueryStartDate]",
				"javascript" => "if(Math.PI() > 3) {\n   this.setError(\"Invalid Function\");\n}",
				_ => "SELECT 10 * 10\n  FROM [Temp]"
			};
			await model.SetValue(value);
		}
	}

	private void OnSetLanguage(string language)
	{
		_language = language;
		StateHasChanged();
	}

	private async Task OnSetThemeAsync(string themePreference)
	{
		if (_monacoEditor != null)
		{
			_theme = _language switch
			{
				"rmscript" => themePreference == "light" ? "rm-light" : "rm-dark",
				_ => themePreference == "light" ? "vs" : "vs-dark"
			};
			_themePreference = themePreference;
			await _monacoEditor.UpdateOptions(new EditorUpdateOptions { Theme = _theme });
		}
	}

	#region IAsyncDisposable

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				await _module.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
		}
	}

	#endregion
}
