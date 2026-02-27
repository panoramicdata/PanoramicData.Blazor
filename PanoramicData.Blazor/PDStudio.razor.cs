using System.Globalization;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor;

/// <summary>
/// A versatile development environment component with splitter-based layout.
/// </summary>
public partial class PDStudio : PDComponentBase, IDisposable
{
	private PDMonacoEditor? EditorRef;
	private PDStudioResults? ResultsRef;
	private PDLog? LogRef;
	private CancellationTokenSource? _cancellationTokenSource;

	private bool _isExecuting;
	private string _executionStatus = StudioExecutionStatus.Ready.ToDisplayString();
	private string _resultsContent = string.Empty;
	private string _currentCode = string.Empty;

	// Shortcut key for execute functionality
	private readonly ShortcutKey _executeShortcut = ShortcutKey.Create("ctrl-enter");

	[Inject] private ILogger<PDStudio> Logger { get; set; } = null!;
	[Inject] private IGlobalEventService GlobalEventService { get; set; } = null!;

	/// <summary>
	/// Gets or sets the studio service for code execution.
	/// </summary>
	[Parameter] public IPDStudioService? StudioService { get; set; }

	/// <summary>
	/// Gets or sets the configuration options.
	/// </summary>
	[Parameter] public PDStudioOptions Options { get; set; } = new();

	/// <summary>
	/// Event callback for when code is executed.
	/// </summary>
	[Parameter] public EventCallback<string> OnCodeExecuted { get; set; }

	/// <summary>
	/// Event callback for when execution state changes.
	/// </summary>
	[Parameter] public EventCallback<bool> OnExecutionStateChanged { get; set; }

	/// <summary>
	/// Event callback for when logging visibility changes.
	/// </summary>
	[Parameter] public EventCallback<bool> OnLoggingVisibilityChanged { get; set; }

	/// <summary>
	/// Gets the PDLog component reference for logging integration.
	/// </summary>
	public PDLog? LogComponent => LogRef;

	/// <summary>
	/// Gets or sets the data provider for the graph data.
	/// </summary>
	[Parameter]
	public IDataProviderService<GraphData>? DataProvider { get; set; }

	/// <summary>
	/// Custom content for the editor toolbar.
	/// </summary>
	[Parameter] public RenderFragment? EditorToolbarContent { get; set; }

	/// <summary>
	/// Gets or sets a callback to initialize Monaco editor options.
	/// </summary>
	[Parameter] public Action<StandaloneEditorConstructionOptions>? InitializeMonacoOptions { get; set; }

	/// <summary>
	/// Gets or sets a callback to initialize the method cache for language completions.
	/// </summary>
	[Parameter] public Action<MethodCache>? InitializeMethodCache { get; set; }

	/// <summary>
	/// Gets or sets a callback to register custom languages for Monaco editor.
	/// </summary>
	[Parameter] public Action<List<Language>>? RegisterLanguages { get; set; }

	/// <summary>
	/// Gets or sets a callback to initialize custom language configurations.
	/// </summary>
	[Parameter] public Func<Language, Task>? InitializeLanguageAsync { get; set; }

	/// <summary>
	/// Gets or sets a callback to update method cache asynchronously.
	/// </summary>
	[Parameter] public Func<MethodCache, string, string, Task>? UpdateMethodCacheAsync { get; set; }

	/// <summary>
	/// Gets a Monaco initializer function that combines default and custom options.
	/// </summary>
	private void GetMonacoInitializer(StandaloneEditorConstructionOptions options)
	{
		// Apply default PDStudio Monaco configuration
		ConfigureMonacoOptions(options);

		// Apply custom configuration if provided
		InitializeMonacoOptions?.Invoke(options);
	}

	/// <summary>
	/// Configures Monaco editor options to ensure proper sizing within PDStudio.
	/// </summary>
	private static void ConfigureMonacoOptions(StandaloneEditorConstructionOptions options)
	{
		// Ensure the editor uses automatic layout and fills its container
		options.AutomaticLayout = true;
		options.ScrollBeyondLastLine = false;
		options.WordWrap = "on";
		options.Minimap = new EditorMinimapOptions { Enabled = false };

		// Enable IntelliSense and syntax highlighting features by default
		options.LineNumbers = "on";
		options.Suggest = new SuggestOptions
		{
			ShowWords = false // Don't show generic word suggestions, prefer language-specific ones
		};

		// Enable folding and bracket matching for better code editing experience
		options.Folding = true;
		options.MatchBrackets = "always";

		// Configure scrolling behavior for better user experience
		options.SmoothScrolling = true;
		options.MouseWheelScrollSensitivity = 1;
	}

	protected override void OnInitialized()
	{
		// Subscribe to service events if available
		if (StudioService != null)
		{
			StudioService.ExecutionEvent += OnStudioExecutionEvent;
		}

		// Subscribe to global keyboard events
		GlobalEventService.KeyDownEvent += OnGlobalKeyDown;

		base.OnInitialized();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			// Register the Ctrl+Enter shortcut for execution
			GlobalEventService.RegisterShortcutKey(_executeShortcut);

			// Disable Ctrl+Enter in Monaco editor to prevent conflicts (with delay to ensure initialization)
			if (EditorRef != null)
			{
				_ = Task.Run(async () =>
				{
					await Task.Delay(500); // Wait for Monaco to fully initialize
					await EditorRef.DisableKeyBindingAsync(13, ctrlKey: true);
				});
			}
		}

		// Logging integration is handled through direct method calls
		await base.OnAfterRenderAsync(firstRender);
	}

	protected override void OnParametersSet()
	{
		// Update execution state if service is available
		if (StudioService != null)
		{
			_isExecuting = StudioService.IsExecuting;
			_executionStatus = StudioService.CurrentStatus.ToDisplayString();
		}

		base.OnParametersSet();
	}

	private void OnGlobalKeyDown(object? sender, KeyboardInfo keyboardInfo)
	{
		// Check if the pressed key combination matches our execute shortcut
		if (_executeShortcut.IsMatch(keyboardInfo.Key, keyboardInfo.Code, keyboardInfo.AltKey, keyboardInfo.CtrlKey, keyboardInfo.ShiftKey))
		{
			// Trigger execution asynchronously
			InvokeAsync(async () => await OnPlayCancelClick());
		}
	}

	private void OnStudioExecutionEvent(object? sender, StudioExecutionEventArgs e)
	{
		InvokeAsync(async () =>
		{
			switch (e.EventType)
			{
				case StudioExecutionEventType.Started:
					_isExecuting = true;
					_executionStatus = e.Status;
					LogInformation("Execution started: {0}", e.Status);
					break;

				case StudioExecutionEventType.UpdateOutput:
					_resultsContent = e.Output;
					if (ResultsRef != null)
					{
						await ResultsRef.UpdateResults(e.Output);
					}
					// If we have a specific status (like timeout), keep it instead of overriding
					if (!string.IsNullOrWhiteSpace(e.Status))
					{
						_executionStatus = e.Status;
					}

					break;

				case StudioExecutionEventType.Progress:
					_executionStatus = e.Status;
					break;

				case StudioExecutionEventType.Log:
					LogToComponent(e.LogLevel, e.Status);
					break;

				case StudioExecutionEventType.Error:
					_isExecuting = false; // End execution on error
										  // For timeout errors, use the specific timeout message
					if (e.Status.Contains("Timed out after"))
					{
						_executionStatus = e.Status;
					}
					else
					{
						_executionStatus = $"Error: {e.Status}";
					}

					LogError("Execution error: {0}", e.Status);

					if (OnExecutionStateChanged.HasDelegate)
					{
						await OnExecutionStateChanged.InvokeAsync(_isExecuting);
					}

					break;

				case StudioExecutionEventType.Completed:
				case StudioExecutionEventType.Cancelled:
					_isExecuting = false;
					_executionStatus = e.EventType == StudioExecutionEventType.Completed ?
						StudioExecutionStatus.Complete.ToDisplayString() :
						StudioExecutionStatus.Cancelled.ToDisplayString();
					LogInformation("Execution {0}: {1}", e.EventType, e.Status);

					if (OnExecutionStateChanged.HasDelegate)
					{
						await OnExecutionStateChanged.InvokeAsync(_isExecuting);
					}

					break;
			}

			StateHasChanged();
		});
	}

	private async Task OnPlayCancelClick()
	{
		if (_isExecuting)
		{
			await CancelExecution();
		}
		else
		{
			await ExecuteCode();
		}
	}

	private async Task ExecuteCode()
	{
		if (StudioService == null || string.IsNullOrWhiteSpace(_currentCode))
		{
			return;
		}

		try
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_isExecuting = true;
			_executionStatus = StudioExecutionStatus.Starting.ToDisplayString();

			if (OnExecutionStateChanged.HasDelegate)
			{
				await OnExecutionStateChanged.InvokeAsync(_isExecuting);
			}

			StateHasChanged();

			// Clear previous results
			_resultsContent = string.Empty;
			if (ResultsRef != null)
			{
				await ResultsRef.ClearResults();
			}

			// Execute code with timeout from options
			var result = await StudioService.ExecuteCodeAsync(
				_currentCode,
				Options.Language,
				null,
				Options.ExecutionTimeoutSeconds,
				_cancellationTokenSource.Token);

			_resultsContent = result;
			if (ResultsRef != null)
			{
				await ResultsRef.UpdateResults(result);
			}

			if (OnCodeExecuted.HasDelegate)
			{
				await OnCodeExecuted.InvokeAsync(_currentCode);
			}

			LogInformation("Code execution completed successfully");
		}
		catch (OperationCanceledException)
		{
			_executionStatus = StudioExecutionStatus.Cancelled.ToDisplayString();
			LogInformation("Code execution was cancelled");
		}
		catch (Exception ex)
		{
			_executionStatus = $"Error: {ex.Message}";
			LogError("Error executing code: {0}", ex.Message);
		}
		finally
		{
			_isExecuting = false;
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;
			StateHasChanged();
		}
	}

	private async Task CancelExecution()
	{
		if (_cancellationTokenSource != null)
		{
			_cancellationTokenSource.Cancel();
			_executionStatus = StudioExecutionStatus.Cancelling.ToDisplayString();
			StateHasChanged();
			await Task.Delay(100); // Brief delay to show cancelling status
		}
	}

	private async Task OnToggleConsoleClick()
	{
		Options.IsLoggingVisible = !Options.IsLoggingVisible;

		LogInformation("Console {0}", Options.IsLoggingVisible ? "shown" : "hidden");

		if (OnLoggingVisibilityChanged.HasDelegate)
		{
			await OnLoggingVisibilityChanged.InvokeAsync(Options.IsLoggingVisible);
		}

		StateHasChanged();
	}

	private void OnCodeChanged(string code)
	{
		_currentCode = code;
	}

	// Menu event handlers
	private async Task OnNewClick()
	{
		_currentCode = string.Empty;
		if (EditorRef != null)
		{
			await EditorRef.SetMonacoValueAsync(string.Empty);
		}

		_resultsContent = string.Empty;
		if (ResultsRef != null)
		{
			await ResultsRef.ClearResults();
		}

		LogInformation("New document created");
	}

	private async Task OnLoadExample1()
	{
		var example = GetExample1();
		_currentCode = example;
		if (EditorRef != null)
		{
			await EditorRef.SetMonacoValueAsync(example);
		}

		LogInformation("Example 1 loaded");
	}

	private async Task OnLoadExample2()
	{
		var example = GetExample2();
		_currentCode = example;
		if (EditorRef != null)
		{
			await EditorRef.SetMonacoValueAsync(example);
		}

		LogInformation("Example 2 loaded");
	}

	private async Task OnOpenClick()
	{
		// Future implementation
		LogInformation("Open clicked (not implemented)");
		await Task.CompletedTask;
	}

	private async Task OnSaveClick()
	{
		// Future implementation
		LogInformation("Save clicked (not implemented)");
		await Task.CompletedTask;
	}

	private async Task SetLogLevel(LogLevel logLevel)
	{
		Options.DefaultLogLevel = logLevel;
		LogInformation("Log level changed to {0}", logLevel);
		StateHasChanged();
		await Task.CompletedTask;
	}

	private async Task OnFileMenuClick(string key)
	{
		switch (key)
		{
			case "New":
				await OnNewClick();
				break;
			case "LoadExample1":
				await OnLoadExample1();
				break;
			case "LoadExample2":
				await OnLoadExample2();
				break;
			case "Open":
				await OnOpenClick();
				break;
			case "Save":
				await OnSaveClick();
				break;
		}
	}

	private async Task OnLoggingMenuClick(string key)
	{
		LogLevel logLevel = key switch
		{
			"Debug" => LogLevel.Debug,
			"Info" => LogLevel.Information,
			"Warning" => LogLevel.Warning,
			"Error" => LogLevel.Error,
			_ => LogLevel.Information
		};

		await SetLogLevel(logLevel);
	}

	private string GetExample1()
	{
		return Options.Language.ToLowerInvariant() switch
		{
			"ncalc" => @"// Basic NCalc Expression Examples

// Mathematical Operations
2 + 3 * 4

// Using Built-in Variables
Pi * 2

// Trigonometric Functions
Sin(Pi / 4) + Cos(Pi / 4)

// Conditional Logic
if(x > y, 'X is greater than Y', 'Y is greater than or equal to X')

// Square Root with Variables
Sqrt(x * y)",

			"sql" or "mssql" => "SELECT * FROM customers WHERE city = 'London'",
			"html" => "<h1>Hello World</h1>\n<p>This is a sample HTML document.</p>",
			"javascript" => "console.log('Hello World!');\nfor(let i = 0; i < 5; i++) {\n  console.log('Count: ' + i);\n}",
			_ => $"<h2>Example 1 for {Options.Language}</h2>\n<p>This is a sample {Options.Language} example.</p>"
		};
	}

	private string GetExample2()
	{
		return Options.Language.ToLowerInvariant() switch
		{
			"ncalc" => @"// Test Error Scenarios (Choose one to test)

// 1. TIMEOUT TEST - Will timeout after 5 seconds
// sleep(10)

// 2. INVALID CODE TEST - Syntax error
// 1 +

// 3. RUNTIME ERROR TEST - Division by zero
// 10 / 0

// 4. VALID COMPLEX EXPRESSION - Advanced example
if(Hour(Now) < 12,
   'Good Morning! Pi = ' + Pi,
   if(Hour(Now) < 18,
      'Good Afternoon! E = ' + E,
      'Good Evening! Today is ' + Today))

// Uncomment one of the test cases above to try different error scenarios",

			"sql" or "mssql" => @"SELECT c.name, SUM(o.total) as total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
GROUP BY c.id, c.name
ORDER BY total_spent DESC",
			"html" => @"<div class=""container"">
  <h1>Sample Table</h1>
  <table border=""1"">
    <tr><th>Name</th><th>Value</th></tr>
    <tr><td>Item 1</td><td>100</td></tr>
    <tr><td>Item 2</td><td>200</td></tr>
  </table>
</div>",
			"javascript" => @"function fibonacci(n) {
  if (n <= 1) return n;
  return fibonacci(n - 1) + fibonacci(n - 2);
}

for(let i = 0; i < 10; i++) {
  console.log(`fibonacci(${i}) = ${fibonacci(i)}`);
}",
			_ => $"<h2>Example 2 for {Options.Language}</h2>\n<p>This is a more complex {Options.Language} example.</p>\n<pre>Sample code block</pre>"
		};
	}

	/// <summary>
	/// Logs a message to both the standard logger and the PDLog component.
	/// </summary>
	private void LogToComponent(LogLevel level, string message, params object[] args)
	{
		// Log to standard logger
#pragma warning disable CA2254 // Logging wrapper intentionally forwards variable message templates
		Logger.Log(level, message, args);
#pragma warning restore CA2254

		// Also log to PDLog component if available
		LogRef?.Log(level, default, message, null, (msg, ex) => string.Format(CultureInfo.InvariantCulture, msg, args));
	}

	/// <summary>
	/// Logs an information message to both loggers.
	/// </summary>
	private void LogInformation(string message, params object[] args)
	{
		LogToComponent(LogLevel.Information, message, args);
	}

	/// <summary>
	/// Logs a warning message to both loggers.
	/// </summary>
	private void LogWarning(string message, params object[] args)
	{
		LogToComponent(LogLevel.Warning, message, args);
	}

	/// <summary>
	/// Logs an error message to both loggers.
	/// </summary>
	private void LogError(string message, params object[] args)
	{
		LogToComponent(LogLevel.Error, message, args);
	}

	/// <summary>
	/// Forces the Monaco editor to refresh its layout.
	/// This can be called when the container size changes.
	/// </summary>
	public async Task RefreshEditorLayoutAsync()
	{
		if (EditorRef != null)
		{
			await EditorRef.ForceLayoutUpdateAsync();
		}
	}

	public void Dispose()
	{
		if (StudioService != null)
		{
			StudioService.ExecutionEvent -= OnStudioExecutionEvent;
		}

		// Unsubscribe from global keyboard events
		GlobalEventService.KeyDownEvent -= OnGlobalKeyDown;

		// Unregister the shortcut key
		GlobalEventService.UnregisterShortcutKey(_executeShortcut);

		_cancellationTokenSource?.Cancel();
		_cancellationTokenSource?.Dispose();

		GC.SuppressFinalize(this);
	}
}