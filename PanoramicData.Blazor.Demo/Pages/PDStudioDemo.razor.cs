using BlazorMonaco.Editor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Demo.Services;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDStudioDemo : IDisposable
{
	private DemoStudioService? _studioService;
	private readonly List<DemoEvent> Events = new();
	private IJSObjectReference? _jsModule;

	protected IPDStudioService StudioService => _studioService ??= new DemoStudioService(ServiceProvider.GetRequiredService<ILogger<DemoStudioService>>());

	protected PDStudioOptions StudioOptions { get; set; } = new()
	{
		Language = "ncalc",
		Theme = "ncalc-light", // Use custom NCalc theme by default
		IsLoggingVisible = true,
		DefaultLogLevel = LogLevel.Information,
		TopSplitSizes = new[] { 50.0, 50.0 },
		MainSplitSizes = new[] { 75.0, 25.0 },
		IsEditingEnabledDuringExecution = true,
		ExecutionTimeoutSeconds = 5 // Set shorter timeout for demo/testing
	};

	[Inject] private ILogger<PDStudioDemo> Logger { get; set; } = null!;
	[Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;

	protected override void OnInitialized()
	{
		// Add initial example content
		AddEvent("Demo", "PDStudio demo initialized with NCalc support (5s timeout)");
	}

	private async Task OnCodeExecuted(string code)
	{
		AddEvent("Execution", $"Expression executed ({code.Length} characters)");
		await Task.CompletedTask;
	}

	private async Task OnExecutionStateChanged(bool isExecuting)
	{
		AddEvent("State", isExecuting ? "Expression evaluation started" : "Expression evaluation completed");
		StateHasChanged();
		await Task.CompletedTask;
	}

	private async Task OnLoggingVisibilityChanged(bool isVisible)
	{
		AddEvent("UI", isVisible ? "Console shown" : "Console hidden");
		StateHasChanged();
		await Task.CompletedTask;
	}

	private async Task OnChangeLanguage()
	{
		var languages = new[] { "ncalc", "html", "sql", "javascript", "json" };
		var currentIndex = Array.IndexOf(languages, StudioOptions.Language.ToLowerInvariant());
		var nextIndex = (currentIndex + 1) % languages.Length;

		StudioOptions.Language = languages[nextIndex];
		AddEvent("Settings", $"Language changed to {StudioOptions.Language.ToUpper()}");
		StateHasChanged();
		await Task.CompletedTask;
	}

	private async Task OnToggleTheme()
	{
		var currentTheme = StudioOptions.Theme;
		var language = StudioOptions.Language.ToLowerInvariant();

		// Use language-specific themes when available
		StudioOptions.Theme = language switch
		{
			"ncalc" => currentTheme.Contains("dark") ? "ncalc-light" : "ncalc-dark",
			"sql" => currentTheme.Contains("dark") ? "sql-light" : "sql-dark",
			_ => currentTheme.Contains("dark") ? "vs-light" : "vs-dark"
		};

		AddEvent("Settings", $"Theme changed to {(StudioOptions.Theme.Contains("dark") ? "Dark" : "Light")} for {language.ToUpper()}");
		StateHasChanged();
		await Task.CompletedTask;
	}

	private async Task OnToggleConsoleClick()
	{
		StudioOptions.IsLoggingVisible = !StudioOptions.IsLoggingVisible;
		AddEvent("Settings", $"Console {(StudioOptions.IsLoggingVisible ? "shown" : "hidden")}");
		StateHasChanged();
		await Task.CompletedTask;
	}

	private void AddEvent(string eventType, string description)
	{
		Events.Add(new DemoEvent
		{
			Timestamp = DateTime.Now,
			EventType = eventType,
			Description = description
		});

		// Keep only last 20 events
		while (Events.Count > 20)
		{
			Events.RemoveAt(0);
		}
	}

	public void Dispose()
	{
		_studioService = null;
		// Properly dispose of the JavaScript module
		if (_jsModule != null)
		{
			// Don't await in Dispose - use fire-and-forget
			_ = Task.Run(async () =>
			{
				try
				{
					await _jsModule.DisposeAsync();
				}
				catch
				{
					// Ignore disposal errors
				}
			});
			_jsModule = null;
		}
	}

	#region Monaco Editor Enhancement Methods

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

			// Add date/time functions
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

		// Add SQL completions for demo
		if (!cache.Contains("sql"))
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
			if (_jsModule == null)
			{
				_jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
					"./_content/PanoramicData.Blazor.Demo/Pages/PDStudioDemo.razor.js");
			}

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
	private async Task UpdateMethodCacheAsync(MethodCache methodCache, string language, string methodName)
	{
		// This could be used to fetch additional parameter information dynamically
		// For the demo, all completions are loaded upfront in InitializeMethodCache
		await Task.CompletedTask;
	}

	#endregion

	private class DemoEvent
	{
		public DateTime Timestamp { get; set; }
		public string EventType { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}
}