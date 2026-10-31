using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Demo.Services;
using System.Globalization;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDStudioDemo : IDisposable
{
	private DemoStudioService? _studioService;
	private readonly List<DemoEvent> _events = [];
	private IJSObjectReference? _jsModule;

	protected IPDStudioService StudioService => _studioService ??= new DemoStudioService(ServiceProvider.GetRequiredService<ILogger<DemoStudioService>>());

	protected PDStudioOptions StudioOptions { get; set; } = new()
	{
		Language = "ncalc",
		Theme = "vs-dark", // Will be overridden by system preference detection
		IsLoggingVisible = true,
		DefaultLogLevel = LogLevel.Information,
		TopSplitSizes = [50.0, 50.0],
		MainSplitSizes = [75.0, 25.0],
		IsEditingEnabledDuringExecution = true,
		ExecutionTimeoutSeconds = 5, // Set shorter timeout for demo/testing
		ResultsTabIconCssClass = "fas fa-table",
		MessagesTabIconCssClass = "fas fa-list",
		ResultsTabToolTip = "What the code returned",
		MessagesTabToolTip = "What happened while it ran"
	};

	protected static IReadOnlyList<PDStudioExample> Examples { get; } =
	[
		new("Basic expressions", """
			// Mathematical Operations
			2 + 3 * 4

			// Trigonometric Functions
			Sin(Pi / 4) + Cos(Pi / 4)

			// Conditional Logic
			if(x > y, 'X is greater than Y', 'Y is greater than or equal to X')
			""", "ncalc"),
		new("Timeout (sleeps 10 seconds)", "sleep(10)", "ncalc"),
		new("Syntax error", "1 +", "ncalc"),
		new("Division by zero", "10 / 0", "ncalc"),
		new("Customers in London", "SELECT * FROM customers WHERE city = 'London'", "sql"),
		new("Spend by customer", """
			SELECT c.name, SUM(o.total) AS total_spent
			FROM customers c
			JOIN orders o ON c.id = o.customer_id
			GROUP BY c.id, c.name
			ORDER BY total_spent DESC
			""", "sql"),
		new("Heading and paragraph", "<h1>Hello World</h1>\n<p>This is a sample HTML document.</p>", "html"),
		new("Counting loop", "for (let i = 0; i < 5; i++) {\n  console.log('Count: ' + i);\n}", "javascript")
	];

	protected static IReadOnlyList<PDStudioOutputTab> OutputTabs { get; } =
	[
		new("about", "About", builder => builder.AddMarkupContent(0, "<p class='p-3 mb-0'>A tab added by the host page through OutputTabs.</p>"), "fas fa-info-circle", "A tab the host page adds")
	];

	[Inject] private ILogger<PDStudioDemo> Logger { get; set; } = null!;
	[Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
	[Inject] private IJSRuntime JSRuntime { get; set; } = null!;

	protected override async Task OnInitializedAsync()
	{
		// Detect system theme preference and set Monaco theme accordingly
		try
		{
			_jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import",
				"./_content/PanoramicData.Blazor.Demo/Pages/PDStudioDemo.razor.js");
			
			if (_jsModule != null)
			{
				var isDark = await _jsModule.InvokeAsync<bool>("isSystemDarkMode");
				UpdateThemeBasedOnPreference(isDark);
			}
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Failed to detect system theme preference, using default");
		}
		
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
		
		// Update theme based on language and current system preference
		if (_jsModule != null)
		{
			var isDark = await _jsModule.InvokeAsync<bool>("isSystemDarkMode");
			UpdateThemeBasedOnPreference(isDark);
		}
		
		AddEvent("Settings", $"Language changed to {StudioOptions.Language.ToUpper()}");
		
		// StateHasChanged() is now called in UpdateThemeBasedOnPreference,
		// but call it again to ensure all changes are propagated
		StateHasChanged();
		await Task.CompletedTask;
	}

	// The demo's Options card: each switches one PDStudio option or host slot.
	private bool ShowFileMenu { get; set; } = true;
	private bool ShowEndContent { get; set; } = true;
	private bool ShowLeftPanel { get; set; } = true;
	private bool ShowFooter { get; set; } = true;
	private bool ShowAboutTab { get; set; } = true;
	private bool ShowExecuteGroupButton { get; set; } = true;
	private bool ShowMenuGroupButton { get; set; } = true;
	private bool UseCustomColours { get; set; }
	private string SeparatorColour { get; set; } = "#6c757d";
	private string BorderColour { get; set; } = "#444444";
	private string AccentColour { get; set; } = "#e83e8c";

	private bool DarkEditor
	{
		get => StudioOptions.Theme.Contains("dark", StringComparison.OrdinalIgnoreCase);
		set => UpdateThemeBasedOnPreference(value);
	}

	private static List<MenuItem> FileMenuItems { get; } =
	[
		new() { Key = "sample", Text = "Load the first example", IconCssClass = "fas fa-fw fa-folder-open" },
		new() { Key = "time", Text = "Insert the time at the cursor", IconCssClass = "fas fa-fw fa-clock" }
	];

	private PDStudio? _studio;

	// SetCodeAsync replaces the code, as opening a file would; InsertTextAsync adds at the cursor.
	private async Task OnFileMenuClick(string key)
	{
		if (_studio is null)
		{
			return;
		}

		if (key == "sample")
		{
			await _studio.SetCodeAsync(Examples.FirstOrDefault(e => e.IsFor(StudioOptions.Language))?.Code ?? string.Empty);
		}
		else if (key == "time")
		{
			await _studio.InsertTextAsync(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
		}
	}

	private Task InsertExampleAsync(PDStudioExample example)
		=> _studio?.InsertTextAsync(example.Code) ?? Task.CompletedTask;

	private void AddEvent(string eventType, string description)
	{
		_events.Add(new DemoEvent
		{
			Timestamp = DateTime.Now,
			EventType = eventType,
			Description = description
		});

		// Keep only last 20 events
		while (_events.Count > 20)
		{
			_events.RemoveAt(0);
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

		GC.SuppressFinalize(this);
	}

	private class DemoEvent
	{
		public DateTime Timestamp { get; set; }
		public string EventType { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}
}