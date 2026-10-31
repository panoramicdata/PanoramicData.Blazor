using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor;

/// <summary>
/// A versatile development environment component with splitter-based layout.
/// </summary>
public partial class PDStudio : PDComponentBase, IDisposable
{
	private CancellationTokenSource? _cancellationTokenSource;
	private readonly EventHandler<KeyboardInfo> _onGlobalKeyDown;

	private bool _isExecuting;
	private string _executionStatus = StudioExecutionStatus.Ready.ToDisplayString();
	private string _resultsContent = string.Empty;
	private string _currentCode = string.Empty;
	private PDStudioResultSet? _resultSet;

	/// <summary>
	/// Gets the tabular results of the last execution, when the service returned them, for example to download.
	/// </summary>
	public PDStudioResultSet? ResultSet => _resultSet;

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
	/// Event callback for when the editor's code changes, whether by typing, New or loading an example.
	/// </summary>
	[Parameter] public EventCallback<string> CodeChanged { get; set; }

	/// <summary>
	/// Gets the code currently in the editor.
	/// </summary>
	public string Code => _currentCode;

	/// <summary>
	/// Event callback for when execution state changes: true when a run starts, false when it ends, however it ends.
	/// </summary>
	[Parameter] public EventCallback<bool> OnExecutionStateChanged { get; set; }

	// The state last given to OnExecutionStateChanged, so each change is reported once.
	private bool? _reportedExecuting;

	/// <summary>
	/// Event callback for when logging visibility changes.
	/// </summary>
	[Parameter] public EventCallback<bool> OnLoggingVisibilityChanged { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PDStudio"/> class.
	/// </summary>
	public PDStudio()
	{
		// Which object raised the key press is of no interest, only which keys it was.
		_onGlobalKeyDown = (_, keyboardInfo) => OnGlobalKeyDown(keyboardInfo);
	}

	private const string _resultsTab = "results";
	private const string _messagesTab = "messages";

	private readonly List<PDStudioMessage> _messages = [];
	private string _activeTab = _resultsTab;
	private LogLevel? _messageLevel;
	private string MessageSearch { get; set; } = string.Empty;

	/// <summary>
	/// Gets the messages recorded so far, oldest first, whether or not the Messages tab is shown.
	/// </summary>
	public IReadOnlyList<PDStudioMessage> Messages => _messages;

	// The filter starts at the options' level; Debug and below show everything.
	private LogLevel MessageLevel => _messageLevel ?? (Options.DefaultLogLevel <= LogLevel.Debug ? LogLevel.Trace : Options.DefaultLogLevel);

	private string MessageLevelValue => MessageLevel.ToString();

	private IEnumerable<PDStudioMessage> FilteredMessages => _messages.Where(m =>
		m.Level >= MessageLevel
		&& (MessageSearch.Length == 0 || m.Text.Contains(MessageSearch, StringComparison.OrdinalIgnoreCase)));

	private string MessagesBadgeCssClass => _messages.Any(m => m.Level >= LogLevel.Error)
		? "pd-studio-tab-badge pd-studio-tab-badge-error"
		: "pd-studio-tab-badge";

	private string MessagesBadgeText
	{
		get
		{
			var errors = _messages.Count(m => m.Level >= LogLevel.Error);
			return errors > 0
				? string.Format(CultureInfo.InvariantCulture, errors == 1 ? "{0} error" : "{0} errors", errors)
				: _messages.Count.ToString(CultureInfo.InvariantCulture);
		}
	}

	// A hidden Messages tab cannot stay selected.
	// A tab that is hidden or no longer offered cannot stay selected.
	private string ActiveTab
		=> (_activeTab == _messagesTab && Options.IsLoggingVisible)
			|| OutputTabs.Any(t => t.Key == _activeTab)
				? _activeTab
				: _resultsTab;

	private bool IsTabActive(string tab) => tab == ActiveTab;

	private string TabCssClass(string tab) => IsTabActive(tab) ? "pd-studio-tab active" : "pd-studio-tab";

	/// <summary>
	/// Shows a tab: "results", "messages", or the key of one of the <see cref="OutputTabs"/>.
	/// </summary>
	public void SelectTab(string key)
	{
		_activeTab = key;
		StateHasChanged();
	}

	/// <summary>
	/// Gets or sets tabs shown after Results and Messages, such as a query plan or a chart.
	/// </summary>
	[Parameter] public IReadOnlyList<PDStudioOutputTab> OutputTabs { get; set; } = [];

	/// <summary>
	/// Gets or sets content shown in a panel to the left of the editor and its tabs, such as a schema browser.
	/// </summary>
	[Parameter] public RenderFragment? LeftPanelContent { get; set; }

	/// <summary>
	/// Gets or sets content placed at the right-hand end of the toolbar.
	/// </summary>
	[Parameter] public RenderFragment? ToolbarEndContent { get; set; }

	/// <summary>
	/// Gets or sets content placed at the start of the toolbar, before Execute, such as a File menu.
	/// </summary>
	[Parameter] public RenderFragment? ToolbarStartContent { get; set; }

	/// <summary>
	/// Gets or sets toolbar items grouped with Execute, after it and before Cancel when
	/// <see cref="PDStudioOptions.ShowCancelButton"/> is set, such as an Explain button.
	/// </summary>
	[Parameter] public RenderFragment? ExecuteGroupContent { get; set; }

	/// <summary>
	/// Gets or sets toolbar items grouped with New and Examples, after them, such as a Restore button.
	/// </summary>
	[Parameter] public RenderFragment? MenuGroupContent { get; set; }

	// The menu group only when something is in it.
	private bool HasMenuButtons
		=> (Options.ShowMenu && (Options.ShowNewButton || CurrentExamples.Count > 0)) || MenuGroupContent != null;

	// Without a Cancel button of its own, Execute turns into Cancel while code runs.
	private bool IsPlayCancel => _isExecuting && !Options.ShowCancelButton;

	// One width for Execute and Cancel only matters when the one button is both.
	private string PlayCssClass => Options.ShowCancelButton
		? Options.ExecuteButtonCssClass
		: $"pd-studio-execute {(_isExecuting ? Options.CancelButtonCssClass : Options.ExecuteButtonCssClass)}";

	/// <summary>
	/// Gets or sets content shown in a bar along the bottom of the studio, such as the cursor position.
	/// </summary>
	[Parameter] public RenderFragment? FooterContent { get; set; }

	private void OnMessageLevelChanged(ChangeEventArgs args)
		=> _messageLevel = Enum.TryParse<LogLevel>(args.Value?.ToString(), out var level) ? level : LogLevel.Trace;

	/// <summary>
	/// Removes every recorded message.
	/// </summary>
	public void ClearMessages() => _messages.Clear();

	private static string LevelCssClass(PDStudioMessage message) => message.Level switch
	{
		>= LogLevel.Error => "level-error",
		LogLevel.Warning => "level-warning",
		LogLevel.Information => message.IsSuccess ? "level-success" : "level-information",
		_ => "level-debug"
	};

	private static string LevelIcon(PDStudioMessage message) => message.Level switch
	{
		>= LogLevel.Error => "fas fa-times-circle",
		LogLevel.Warning => "fas fa-exclamation-triangle",
		LogLevel.Information => message.IsSuccess ? "fas fa-check-circle" : "fas fa-info-circle",
		_ => "fas fa-bug"
	};

	private static string LevelName(PDStudioMessage message) => message.Level switch
	{
		LogLevel.Critical => "Critical",
		LogLevel.Error => "Error",
		LogLevel.Warning => "Warning",
		LogLevel.Information => message.IsSuccess ? "Success" : "Info",
		_ => "Debug"
	};

	/// <summary>Gets or sets the code editor; set by the markup's <c>@ref</c>.</summary>
	internal PDMonacoEditor? EditorRef { get; set; }

	/// <summary>Gets or sets the results pane; set by the markup's <c>@ref</c>.</summary>
	internal PDStudioResults? ResultsRef { get; set; }

	/// <summary>
	/// Gets or sets the data provider for the graph data.
	/// </summary>
	[Parameter]
	public IDataProviderService<GraphData>? DataProvider { get; set; }

	/// <summary>
	/// Gets or sets the examples offered on the Examples menu; those for another language are left out.
	/// </summary>
	[Parameter] public IReadOnlyList<PDStudioExample> Examples { get; set; } = [];

	private const string _examplePrefix = "example-";

	private List<PDStudioExample> CurrentExamples => [.. Examples.Where(e => e.IsFor(Options.Language))];

	private static string ExampleKey(int index) => _examplePrefix + index.ToString(CultureInfo.InvariantCulture);

	// Built afresh each render: a child PDMenuItem registers with its dropdown only once, so would not follow a language change.
	private List<MenuItem> ExampleMenuItems()
		=> [.. CurrentExamples.Select((e, i) => new MenuItem { Text = e.Name, Key = ExampleKey(i) })];

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

		// Room between the line under the toolbar and the first line of code
		options.Padding = new EditorPaddingOptions { Top = 6 };

		// Enable IntelliSense and syntax highlighting features by default
		options.LineNumbers = "on";
		options.Suggest = new SuggestOptions
		{
			ShowWords = false // Don't show generic word suggestions, prefer language-specific ones
		};

		// Enable folding and bracket matching for better code editing experience
		options.Folding = true;
		options.MatchBrackets = "always";

		// Configure scrolling behaviour for better user experience
		options.SmoothScrolling = true;
		options.MouseWheelScrollSensitivity = 1;
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		// Subscribe to service events if available
		if (StudioService != null)
		{
			StudioService.ExecutionEvent += OnStudioExecutionEvent;
		}

		// Subscribe to global keyboard events
		GlobalEventService.KeyDownEvent += _onGlobalKeyDown;

		base.OnInitialized();
	}

	/// <inheritdoc />
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
					await RunOnEditorAsync(e => e.DisableKeyBindingAsync(13, ctrlKey: true), "disable Ctrl+Enter");
				});
			}
		}

		// Logging integration is handled through direct method calls
		await base.OnAfterRenderAsync(firstRender);
	}

	/// <inheritdoc />
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

	private void OnGlobalKeyDown(KeyboardInfo keyboardInfo)
	{
		// Check if the pressed key combination matches our execute shortcut
		if (_executeShortcut.IsMatch(keyboardInfo.Key, keyboardInfo.Code, keyboardInfo.AltKey, keyboardInfo.CtrlKey, keyboardInfo.ShiftKey))
		{
			// Trigger execution asynchronously
			InvokeAsync(() => RunSafelyAsync(OnPlayCancelClick, "run the code"));
		}
	}

	private void OnStudioExecutionEvent(object? sender, StudioExecutionEventArgs e)
	{
		InvokeAsync(() => RunSafelyAsync(async () =>
		{
			await ApplyExecutionEventAsync(e);
			StateHasChanged();
		}, "show the service's progress"));
	}

	// Event handlers run outside any caller's try/catch, so a failure is logged rather than allowed to take the host down.
	private async Task RunSafelyAsync(Func<Task> action, string what)
	{
		try
		{
			await action();
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "PDStudio could not {What}", what);
		}
	}

	// Editor calls go through JavaScript, which fails once the browser has gone.
	private async Task RunOnEditorAsync(Func<PDMonacoEditor, Task> action, string what)
	{
		if (EditorRef is null)
		{
			return;
		}

		try
		{
			await action(EditorRef);
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "PDStudio could not {What} in the editor", what);
		}
	}

	private async Task ApplyExecutionEventAsync(StudioExecutionEventArgs e)
	{
		switch (e.EventType)
		{
			case StudioExecutionEventType.Started:
				_isExecuting = true;
				_executionStatus = e.Status;
				LogInformation("Execution started: {0}", e.Status);
				break;

			case StudioExecutionEventType.UpdateOutput:
				await ShowOutputAsync(e);
				break;

			case StudioExecutionEventType.Progress:
				_executionStatus = e.Status;
				break;

			case StudioExecutionEventType.Log:
				LogToComponent(e.LogLevel, e.Status);
				break;

			case StudioExecutionEventType.Error:
				await EndExecutionWithErrorAsync(e);
				break;

			case StudioExecutionEventType.Completed:
			case StudioExecutionEventType.Cancelled:
				await EndExecutionAsync(e);
				break;

			default:
				// OutputComplete changes nothing here: the output has already been shown as it arrived.
				break;
		}
	}

	private async Task ShowOutputAsync(StudioExecutionEventArgs e)
	{
		await ShowResultsAsync(e.Output);

		// If we have a specific status (like timeout), keep it instead of overriding
		if (!string.IsNullOrWhiteSpace(e.Status))
		{
			_executionStatus = e.Status;
		}
	}

	private async Task EndExecutionWithErrorAsync(StudioExecutionEventArgs e)
	{
		_isExecuting = false; // End execution on error

		// For timeout errors, use the specific timeout message
		_executionStatus = e.Status.Contains("Timed out after") ? e.Status : $"Error: {e.Status}";

		LogError("Execution error: {0}", e.Status);

		await NotifyExecutionStateChangedAsync();
	}

	private async Task EndExecutionAsync(StudioExecutionEventArgs e)
	{
		_isExecuting = false;
		_executionStatus = e.EventType == StudioExecutionEventType.Completed ?
			StudioExecutionStatus.Complete.ToDisplayString() :
			StudioExecutionStatus.Cancelled.ToDisplayString();
		if (e.EventType == StudioExecutionEventType.Completed)
		{
			LogSuccess($"Execution Completed: {e.Status}");
		}
		else
		{
			LogInformation("Execution {0}: {1}", e.EventType, e.Status);
		}

		await NotifyExecutionStateChangedAsync();
	}

	// A run can end through a service event and then through ExecuteCode's finally; both report, so only a change counts.
	private async Task NotifyExecutionStateChangedAsync()
	{
		if (_reportedExecuting == _isExecuting)
		{
			return;
		}

		_reportedExecuting = _isExecuting;
		if (OnExecutionStateChanged.HasDelegate)
		{
			await OnExecutionStateChanged.InvokeAsync(_isExecuting);
		}
	}

	// Shows output in the results pane, replacing whatever it held.
	private async Task ShowResultsAsync(string output)
	{
		_resultsContent = output;
		_resultSet = null;
		if (ResultsRef != null)
		{
			await ResultsRef.UpdateResults(output);
		}
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

			await NotifyExecutionStateChangedAsync();

			StateHasChanged();

			await RunCodeAsync(StudioService, _cancellationTokenSource.Token);
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
			await NotifyExecutionStateChangedAsync();
			StateHasChanged();
		}
	}

	// Clears the previous results, executes the code with the timeout from the options, and shows what it returns.
	private async Task RunCodeAsync(IPDStudioService studioService, CancellationToken cancellationToken)
	{
		await ClearResultsAsync();

		if (studioService is IPDStudioResultSetService tabular
			&& await tabular.ExecuteResultSetAsync(_currentCode, Options.Language, Options.ExecutionTimeoutSeconds, cancellationToken) is { } resultSet)
		{
			_resultSet = resultSet;
			_executionStatus = StudioExecutionStatus.Complete.ToDisplayString();
			await NotifyCodeExecutedAsync();
			LogSuccess(resultSet.DurationText is { } duration
				? $"{resultSet.RowCountText} returned in {duration}"
				: $"{resultSet.RowCountText} returned");
			return;
		}

		var result = await studioService.ExecuteCodeAsync(
			_currentCode,
			Options.Language,
			null,
			Options.ExecutionTimeoutSeconds,
			cancellationToken);

		await ShowResultsAsync(result);
		await NotifyCodeExecutedAsync();

		LogSuccess("Code execution completed successfully");
	}

	private async Task NotifyCodeExecutedAsync()
	{
		if (OnCodeExecuted.HasDelegate)
		{
			await OnCodeExecuted.InvokeAsync(_currentCode);
		}
	}

	private async Task ClearResultsAsync()
	{
		_resultsContent = string.Empty;
		_resultSet = null;
		if (ResultsRef != null)
		{
			await ResultsRef.ClearResults();
		}
	}

	private async Task CancelExecution()
	{
		if (_cancellationTokenSource != null)
		{
			// show "Cancelling..." before cancelling: the cancellation can complete the execution, which sets
			// "Cancelled", and writing "Cancelling..." afterwards would overwrite it (issue #176)
			_executionStatus = StudioExecutionStatus.Cancelling.ToDisplayString();
			StateHasChanged();
			await _cancellationTokenSource.CancelAsync();
			await Task.Delay(100); // Brief delay to show cancelling status
		}
	}
	private async Task OnCodeChanged(string code)
	{
		_currentCode = code;
		await CodeChanged.InvokeAsync(code);
	}

	/// <summary>
	/// Replaces the code in the editor, for example with a file the host has opened.
	/// </summary>
	public async Task SetCodeAsync(string code)
	{
		_currentCode = code;
		await CodeChanged.InvokeAsync(code);
		await RunOnEditorAsync(e => e.SetMonacoValueAsync(code), "set the code");
		StateHasChanged();
	}

	/// <summary>
	/// Inserts text at the editor's cursor, replacing any selection, for example a table name the user picked.
	/// The editor then reports the new code through <see cref="CodeChanged"/>.
	/// </summary>
	public Task InsertTextAsync(string text)
		=> RunOnEditorAsync(e => e.InsertTextAsync(text), "insert text");

	// Menu event handlers
	private async Task OnNewClick()
	{
		await SetCodeAsync(string.Empty);
		await ClearResultsAsync();

		LogInformation("New document created");
	}

	private async Task OnExampleClick(string key)
	{
		var examples = CurrentExamples;
		if (!key.StartsWith(_examplePrefix, StringComparison.Ordinal)
			|| !int.TryParse(key.AsSpan(_examplePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
			|| index >= examples.Count)
		{
			return;
		}

		var example = examples[index];
		await SetCodeAsync(example.Code);

		LogInformation("Example loaded: {0}", example.Name);
	}

	/// <summary>
	/// Logs a message to the standard logger and the Messages tab.
	/// </summary>
	private void LogToComponent(LogLevel level, string message, params object[] args)
	{
		// Without arguments the message is plain text, such as a service's status, and may contain braces.
		AddMessage(level, args.Length == 0 ? message : string.Format(CultureInfo.InvariantCulture, message, args), false);
	}

	// A successful outcome: Information to the logger and the level filter, styled as a success on the tab.
	private void LogSuccess(string text) => AddMessage(LogLevel.Information, text, true);

	private void AddMessage(LogLevel level, string text, bool isSuccess)
	{
		if (Logger.IsEnabled(level))
		{
			Logger.Log(level, "{Message}", text);
		}

		while (_messages.Count >= Math.Max(1, Options.MaxMessages))
		{
			_messages.RemoveAt(0);
		}

		_messages.Add(new PDStudioMessage(DateTime.UtcNow, level, text) { IsSuccess = isSuccess });
	}

	/// <summary>
	/// Logs an information message to both loggers.
	/// </summary>
	private void LogInformation(string message, params object[] args)
	{
		LogToComponent(LogLevel.Information, message, args);
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
		await RunOnEditorAsync(e => e.ForceLayoutUpdateAsync(), "refresh the layout");
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (StudioService != null)
		{
			StudioService.ExecutionEvent -= OnStudioExecutionEvent;
		}

		// Unsubscribe from global keyboard events
		GlobalEventService.KeyDownEvent -= _onGlobalKeyDown;

		// Unregister the shortcut key
		GlobalEventService.UnregisterShortcutKey(_executeShortcut);

		try
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
		}
		catch (Exception ex)
		{
			// Cancelling runs the execution's own callbacks, and disposal must not fail because of them.
			Logger.LogWarning(ex, "PDStudio could not cancel its execution while being disposed");
		}

		GC.SuppressFinalize(this);
	}
}
