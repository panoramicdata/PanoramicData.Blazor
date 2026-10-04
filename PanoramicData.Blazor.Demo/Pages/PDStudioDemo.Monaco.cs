using BlazorMonaco.Editor;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Monaco editor themes, completions and language configuration for the PDStudio demo.
/// </summary>
public partial class PDStudioDemo
{
	// Dark and light Monaco theme of each language with its own themes; others use the built-in vs themes
	private static readonly Dictionary<string, (string Dark, string Light)> _languageThemes = new()
	{
		["ncalc"] = ("ncalc-dark", "ncalc-light"),
		["sql"] = ("sql-dark", "sql-light")
	};

	private void UpdateThemeBasedOnPreference(bool isDark)
	{
		var language = StudioOptions.Language.ToLowerInvariant();
		var (dark, light) = _languageThemes.GetValueOrDefault(language, ("vs-dark", "vs"));
		StudioOptions.Theme = isDark ? dark : light;

		// Trigger re-render to propagate theme change to PDMonaco
		StateHasChanged();
	}

	/// <summary>
	/// Initializes Monaco editor options for better PDStudio experience.
	/// </summary>
	private static void InitializeMonacoOptions(StandaloneEditorConstructionOptions options)
	{
		// Enhanced options for PDStudio demo
		options.LineNumbers = "on";
		options.Minimap = new EditorMinimapOptions { Enabled = true };
		options.Folding = true;
		options.MatchBrackets = "always";

		// Enable advanced IntelliSense features
		options.Suggest = new SuggestOptions
		{
			ShowWords = false, // Don't show generic words
			ShowSnippets = true
		};

		// Smooth scrolling for better UX
		options.SmoothScrolling = true;
	}

	/// <summary>
	/// Initializes method cache for language completions and IntelliSense.
	/// </summary>
	private static void InitializeMethodCache(MethodCache cache)
	{
		// Add NCalc mathematical functions
		if (!cache.Contains("ncalc"))
		{
			// Add built-in Math functions using reflection
			cache.AddPublicStaticTypeMethods("ncalc", typeof(Math), new DefaultDescriptionProvider());

			// Add custom NCalc functions
			AddNCalcLogicalMethods(cache);

			// Add date/time functions
			AddNCalcDateMethods(cache);
		}

		// Add SQL completions for demo
		if (!cache.Contains("sql"))
		{
			AddSqlMethods(cache);
		}
	}

	private static void AddNCalcLogicalMethods(MethodCache cache)
	{
		cache.AddMethod("ncalc", new MethodCache.Method
		{
			MethodName = "if",
			Description = "Evaluates a condition and returns one of two values.",
			Parameters = [
				new MethodCache.Parameter { Name = "condition", Description = "Boolean condition to evaluate", Type = typeof(bool) },
				new MethodCache.Parameter { Name = "trueValue", Description = "Value returned if condition is true", Type = typeof(object) },
				new MethodCache.Parameter { Name = "falseValue", Description = "Value returned if condition is false", Type = typeof(object) }
			]
		});

		cache.AddMethod("ncalc", new MethodCache.Method
		{
			MethodName = "in",
			Description = "Checks if a value exists in a list of values.",
			Parameters = [
				new MethodCache.Parameter { Name = "value", Description = "Value to search for", Type = typeof(object) },
				new MethodCache.Parameter { Name = "values", Description = "List of values to search in", Type = typeof(object) }
			]
		});

		cache.AddMethod("ncalc", new MethodCache.Method
		{
			MethodName = "isnull",
			Description = "Returns true if the value is null, false otherwise.",
			Parameters = [
				new MethodCache.Parameter { Name = "value", Description = "Value to check for null", Type = typeof(object) }
			]
		});
	}

	private static void AddNCalcDateMethods(MethodCache cache)
	{
		cache.AddMethod("ncalc", new MethodCache.Method
		{
			MethodName = "AddDays",
			Description = "Adds a specified number of days to a date.",
			Parameters = [
				new MethodCache.Parameter { Name = "date", Description = "The base date", Type = typeof(DateTime) },
				new MethodCache.Parameter { Name = "days", Description = "Number of days to add", Type = typeof(double) }
			]
		});

		cache.AddMethod("ncalc", new MethodCache.Method
		{
			MethodName = "AddHours",
			Description = "Adds a specified number of hours to a date.",
			Parameters = [
				new MethodCache.Parameter { Name = "date", Description = "The base date", Type = typeof(DateTime) },
				new MethodCache.Parameter { Name = "hours", Description = "Number of hours to add", Type = typeof(double) }
			]
		});

		cache.AddMethod("ncalc", new MethodCache.Method
		{
			MethodName = "Hour",
			Description = "Gets the hour component of a date/time value.",
			Parameters = [
				new MethodCache.Parameter { Name = "date", Description = "The date/time value", Type = typeof(DateTime) }
			]
		});
	}

	private static void AddSqlMethods(MethodCache cache)
	{
		cache.AddMethod("sql", new MethodCache.Method
		{
			MethodName = "SELECT",
			Description = "Retrieves rows from a database table.",
			Parameters = [
				new MethodCache.Parameter { Name = "columns", Description = "Columns to select", Type = typeof(string) }
			]
		});

		cache.AddMethod("sql", new MethodCache.Method
		{
			MethodName = "FROM",
			Description = "Specifies the table to select from.",
			Parameters = [
				new MethodCache.Parameter { Name = "table", Description = "Table name", Type = typeof(string) }
			]
		});

		cache.AddMethod("sql", new MethodCache.Method
		{
			MethodName = "WHERE",
			Description = "Filters rows based on a condition.",
			Parameters = [
				new MethodCache.Parameter { Name = "condition", Description = "Filter condition", Type = typeof(string) }
			]
		});
	}

	/// <summary>
	/// Registers custom languages for Monaco editor.
	/// </summary>
	private static void RegisterLanguages(List<Language> languages)
	{
		// Register NCalc language with advanced features
		languages.Add(new Language
		{
			Id = "ncalc",
			ShowCompletions = true,
			FunctionDelimiter = '(',
			SignatureHelpTriggers = ['(', ',']
		});

		// Register SQL with basic completions
		languages.Add(new Language
		{
			Id = "sql",
			ShowCompletions = true,
			FunctionDelimiter = ' ',
			SignatureHelpTriggers = [' ', '(', ',']
		});
	}

	/// <summary>
	/// Initializes language-specific configurations (called for each registered language).
	/// </summary>
	private async Task InitializeLanguageAsync(Language language)
	{
		try
		{
			// Load JavaScript module for Monaco language configuration
			_jsModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import",
					"./_content/PanoramicData.Blazor.Demo/Pages/PDStudioDemo.razor.js");

			// Configure Monaco with custom language features
			if (_jsModule != null)
			{
				await _jsModule.InvokeVoidAsync("configurePDStudioMonaco");
			}
		}
		catch (Exception ex)
		{
			// Log the error but don't crash the component
			Logger?.LogWarning(ex, "Failed to initialize Monaco language configuration for {Language}", language.Id);
		}
	}

	/// <summary>
	/// Updates method cache asynchronously (useful for dynamic completions).
	/// </summary>
	private static Task UpdateMethodCacheAsync()
	{
		// This could be used to fetch additional parameter information dynamically
		// For the demo, all completions are loaded upfront in InitializeMethodCache
		return Task.CompletedTask;
	}
}
