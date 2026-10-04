using BlazorMonaco.Editor;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMonaco : IAsyncDisposable
{
	private string _theme = "vs";
	protected PDMonacoEditor? Editor { get; set; }
	private string _language = "sql";
	protected bool ShowSuggestions { get; set; } = true;
	private string _themePreference = "light";
	private string _selectionText = string.Empty;
	private string _value = "SELECT 10 * 10\n  FROM [Temp]";

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

	[CascadingParameter]
	protected EventManager? EventManager { get; set; }

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
			// Nothing to do...
		}
	}

	#endregion

	private async Task OnGetSelection()
	{
		_selectionText = string.Empty;
		if (Editor != null)
		{
			var s = await Editor.GetSelection();
			if (s != null)
			{
				var r = new BlazorMonaco.Range(s.StartLineNumber, s.StartColumn, s.EndLineNumber, s.EndColumn);
				if (r != null)
				{
					var v = await Editor.GetMonacoValueAsync(r, EndOfLinePreference.TextDefined);
					_selectionText = $"{v} ({s.StartLineNumber},{s.StartColumn} - {s.EndLineNumber},{s.EndColumn})";
				}
			}
		}
	}

	private void OnSelectionChanged(Selection selection)
	{
		EventManager?.Add(new Event("SelectionChanged",
			new EventArgument("Start Line", selection.StartLineNumber),
			new EventArgument("Start Column", selection.StartColumn),
			new EventArgument("End Line", selection.EndLineNumber),
			new EventArgument("End Column", selection.EndColumn)));
	}

	private async Task OnSetSelection()
	{
		if (Editor != null)
		{
			var selection = new Selection
			{
				SelectionStartLineNumber = 2,
				SelectionStartColumn = 8,
				PositionColumn = 14,
				PositionLineNumber = 2,
			};
			await Editor.SetSelectionAsync(selection);
		}
	}

	private void OnSetLanguage(string language)
	{
		_language = language;
		_value = _language switch
		{
			"ncalc" => "10 * -3.14 + Sqrt(9)",
			"rmscript" => "[Color: value='#1a1a1a', intensifyColor='#ffffff', intensifyPercent=50,  storeAs='MyVar']",
			"javascript" => "if(Math.PI() > 3) {\n   this.setError(\"Invalid Function\");\n}",
			_ => "SELECT 10 * 10\n  FROM [Temp]"
		};
		if (_methodCache != null)
		{
			_methodCache.Options.HideDataTypes = language == "rmscript";
		}

		StateHasChanged();
	}

	private void OnSetTheme(string themePreference)
	{
		_theme = _language switch
		{
			"ncalc" => themePreference == "light" ? "ncalc-light" : "ncalc-dark",
			"rmscript" => themePreference == "light" ? "rm-light" : "rm-dark",
			_ => themePreference == "light" ? "vs" : "vs-dark"
		};
		_themePreference = themePreference;
	}
}
