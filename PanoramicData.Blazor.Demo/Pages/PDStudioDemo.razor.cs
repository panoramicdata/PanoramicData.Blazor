using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Demo.Services;

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
		ExecutionTimeoutSeconds = 5 // Set shorter timeout for demo/testing
	};

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

	private async Task OnToggleConsoleClick()
	{
		StudioOptions.IsLoggingVisible = !StudioOptions.IsLoggingVisible;
		AddEvent("Settings", $"Console {(StudioOptions.IsLoggingVisible ? "shown" : "hidden")}");
		StateHasChanged();
		await Task.CompletedTask;
	}

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