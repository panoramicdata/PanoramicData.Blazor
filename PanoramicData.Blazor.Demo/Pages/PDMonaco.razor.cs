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
	private MethodCache _methodCache;

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
		// this example show suporting two diffewrent language, the first is NCalc
		// and uses helper methods to derive methods info using reflection, the
		// second is rmscript which is an example of a propriety language where
		// the parameters of the methods has to be fetch asynchronously

		// store reference to cache as when switch language need to change options
		_methodCache = cache;

		if (!cache.Contains("ncalc"))
		{
			cache.AddPublicStaticTypeMethods("ncalc", typeof(Math), new DefaultDescriptionProvider());
			cache.AddTypeMethods("ncalc", typeof(NCalcExtensions.Extensions.IFunctionPrototypes));
		}

		if (!cache.Contains("rmscript"))
		{
			cache.AddMethod("rmscript", new MethodCache.Method
			{
				MethodName = "Color",
				Description = "Outputs a hex-encoded colour string based on the percentage difference and specified intensity of an input colour. " +
								"Input colours can be specified by name (e.g. white) or hex-encoded (e.g. #FFFFFF, also white)"
			});
			cache.AddMethod("rmscript", new MethodCache.Method
			{
				MethodName = "String",
				Description = "Constructs a string."
			});
		}
	}

	private Task UpdateCacheAsync(MethodCache methodCache, string language, string methodName)
	{
		// use this function to fetch parameter information for the given method
		// useful when thousands of parameters in total and to load upfront
		// along with methods would be too expensive
		if (language == "rmscript")
		{
			// add parameters to method - if not already fetched
			var method = methodCache.FindMethod(language, methodName).FirstOrDefault();
			if (method != null && method.State is null)
			{
				AddRmscriptMacroParameters(method);
				method.State = true;  // use the state property to fetch only once
			}
		}
		return Task.CompletedTask;
	}

	private void AddRmscriptMacroParameters(MethodCache.Method method)
	{
		if (method.MethodName == "Color")
		{
			// use this static method to add parameters - ensures unspecified positions are calculated
			MethodCache.AddMethodParameters(method, new[]
			{
				new MethodCache.Parameter {
					Name = "value",
					Description = "The colour to use to increase intensity of the input colour.",
					Type = typeof(string),
				},
				new MethodCache.Parameter {
					Name = "intensifyColor",
					Description = "The colour to use to increase intensity of the input colour.",
					Type = typeof(string),
				},
				new MethodCache.Parameter {
					Name = "intensifyPercent",
					Description = "The percentage intensity to apply.",
					Type = typeof(double),
				}
			});
		}

		if (method.MethodName == "String")
		{
			// use this static method to add parameters - ensures unspecified positions are calculated
			MethodCache.AddMethodParameters(method, new[]
			{
				new MethodCache.Parameter {
					Name = "value",
					Description = "The string value.",
					Type = typeof(string),
				},
				new MethodCache.Parameter {
					Name = "selectDistinct",
					Description = "Whether to select distinct values in a string list.",
					IsOptional = true
				},
				new MethodCache.Parameter {
					Name = "find",
					Description = "The string value(s) to find in the value."
				},
				new MethodCache.Parameter {
					Name = "replaceWith",
					Description = "The string value(s) to use to replace the string specified in the find parameter."
				},
				new MethodCache.Parameter {
					Name = "regexFind",
					Description = "The Regex pattern(s) to find in the value."
				},
				new MethodCache.Parameter {
					Name = "regexReplaceWith",
					Description = "The Regex string value(s) to use to replace the string specified in the regexFind parameter."
				}
			});
		}

		// common parameters
		// use this static method to add parameters - ensures unspecified positions are calculated
		MethodCache.AddMethodParameters(method, new[]
		{
			new MethodCache.Parameter {
				Name = "comment",
				Description = "Add a comment to make your document template more readable. The comment is discarded in the output document",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "failureText",
				Description = "The text to display should the macro fail to execute. Note that a poorly-specified macro (e.g. omitting mandatory parameters) will still result in an error message.",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "warning",
				Description = "If specified, adds a warning message for this macro. This is processed as an NCalc, and the warning message will ALWAYS be present and will be the value of the evaluated NCalc expression.",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "obfuscation",
				Description = "Obfuscation type. Use obfuscation to write reports where sensitive data is hidden.",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "mode",
				Description = "The mode in which variables are stored. In the legacy mode (default for Schedules), the variable created is a string and formatted.",
				IsOptional = true,
				Type = typeof(string),
			},
			new MethodCache.Parameter {
				Name = "errorOnOverflow",
				Description = "Should NCalc expression evaluation throw error on Overflow.",
				IsOptional = true,
				Type = typeof(bool),
			},
			new MethodCache.Parameter {
				Name = "if",
				Description = "The condition that must be true in order for the macro to be executed/evaluated.",
				IsOptional = true,
				Type = typeof(bool),
			}
		});
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
			"rmscript" => "[Color: value='#1a1a1a', intensifyColor='#ffffff', intensifyPercent=50,  storeAs='MyVar']",
			"javascript" => "if(Math.PI() > 3) {\n   this.setError(\"Invalid Function\");\n}",
			_ => "SELECT 10 * 10\n  FROM [Temp]"
		};
		_methodCache.Options.HideDataTypes = language == "rmscript";
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

	private void RegisterLanguages(List<Language> languages)
	{
		// this method is called by the PDMonacoEditor to allow custom languages to be added
		languages.Add(new Language
		{
			Id = "ncalc",
			ShowCompletions = true,
			SignatureHelpTriggers = new[] { '(', ',' }
		});
		languages.Add(new Language
		{
			Id = "rmscript",
			ShowCompletions = true,
			FunctionDelimiter = ':',
			OptionalParameterPostfix = '=',
			SignatureHelpTriggers = new[] { ':', ',' }
		});
	}
}
