using BlazorMonaco.Editor;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMonaco : IAsyncDisposable
{
	private string _theme = "vs";
	private string _themePreference = "light";
	private string _language = "sql";
	private string _value = "SELECT 10 * 10\n  FROM [Temp]";
	private IJSObjectReference? _module;

	[Inject]
	public IJSRuntime? JSRuntime { get; set; }

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

	private void InitializeCache(MethodCache cache)
	{
		// this method allows  method signatures to be registered for a language
		if (!cache.Contains("ncalc"))
		{
			cache.AddPublicStaticTypeMethods("ncalc", typeof(Math), new DefaultDescriptionProvider());
			cache.AddTypeMethods("ncalc", typeof(NCalcExtensions.Extensions.IFunctionPrototypes));
		}
	}

	private void InitializeOptions(StandaloneEditorConstructionOptions options)
	{
		// this method is called by the PDMonacoEditor when initialized to allow for default options to be applied
		// Language and Theme are already set from the supplied Parameters
		options.LineNumbers = "on";
	}

	public async Task InitializeLanguageAsync(Language language)
	{
		// this method is called by the PDMonacoEditor to allow custom languages to be configured
		// this needs to be performed in javascript
		if (_module is null && JSRuntime != null)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor.Demo/Pages/PDMonaco.razor.js").ConfigureAwait(true); ;
		}
		if (_module != null)
		{
			await _module.InvokeVoidAsync("configureMonaco");
		}
	}

	private void OnSetLanguage(string language)
	{
		_language = language;
		_value = _language switch
		{
			"ncalc" => "10 * -3.14 + Sqrt(9)",
			"javascript" => "if(Math.PI() > 3) {\n   this.setError(\"Invalid Function\");\n}",
			_ => "SELECT 10 * 10\n  FROM [Temp]"
		};
		StateHasChanged();
	}

	private void OnSetTheme(string themePreference)
	{
		_theme = _language switch
		{
			"ncalc" => themePreference == "light" ? "ncalc-light" : "ncalc-dark",
			_ => themePreference == "light" ? "vs" : "vs-dark"
		};
		_themePreference = themePreference;
	}

	private void RegisterLanguages(List<Language> languages)
	{
		// this method is called by the PDMonacoEditor to allow custom languages to be added
		languages.Add(new Language
		{
			Id = "ncalc",
			ShowCompletions = true,
			SignatureHelpTriggers = new[] { '(', ',' }
		});
	}
}
