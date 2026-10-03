using BlazorMonaco.Editor;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Custom languages, editor options and method completions for the PDMonacoEditor demo.
/// </summary>
public partial class PDMonaco
{
	private MethodCache? _methodCache;
	private IJSObjectReference? _module;

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
			cache.AddMethod("rmscript", new MethodCache.Method
			{
				MethodName = "List.Add",
				Description = "Adds an item to a list."
			});

		}
	}

	private static Task UpdateCacheAsync(MethodCache methodCache, string language, string methodName)
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

	private static readonly Dictionary<string, Func<MethodCache.Parameter[]>> _rmscriptMacroParameters = new()
	{
		["List.Add"] = RmscriptParameters.GetListAddParameters,
		["Color"] = RmscriptParameters.GetColorParameters,
		["String"] = RmscriptParameters.GetStringParameters
	};

	private static void AddRmscriptMacroParameters(MethodCache.Method method)
	{
		// use this static method to add parameters - ensures unspecified positions are calculated
		if (_rmscriptMacroParameters.TryGetValue(method.MethodName, out var getParameters))
		{
			MethodCache.AddMethodParameters(method, getParameters());
		}

		// common parameters
		MethodCache.AddMethodParameters(method, [.. RmscriptParameters.GetCommonRmscriptParameters(), .. RmscriptParameters.GetCommonRmscriptEvaluationParameters()]);
	}

	private static void InitializeOptions(StandaloneEditorConstructionOptions options)
	{
		// this method is called by the PDMonacoEditor when initialized to allow for default options to be applied
		// Language and Theme are already set from the supplied Parameters
		options.LineNumbers = "on";
		options.Suggest = new SuggestOptions
		{
			ShowWords = false
		};
	}

	public async Task InitializeLanguageAsync(Language language)
	{
		// this method is called by the PDMonacoEditor to allow custom languages to be configured
		// this needs to be performed in javascript
		if (_module is null && JSRuntime != null)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor.Demo/Pages/PDMonaco.razor.js").ConfigureAwait(true);
		}

		if (_module != null)
		{
			await _module.InvokeVoidAsync("configureMonaco");
		}
	}

	private static void RegisterLanguages(List<Language> languages)
	{
		// this method is called by the PDMonacoEditor to allow custom languages to be added
		languages.Add(new Language
		{
			Id = "ncalc",
			ShowCompletions = true,
			SignatureHelpTriggers = ['(', ',']
		});
		languages.Add(new Language
		{
			Id = "rmscript",
			ShowCompletions = true,
			FunctionDelimiter = ':',
			OptionalParameterPostfix = '=',
			SignatureHelpTriggers = [':', ',']
		});
	}
}
