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
	/// Called when New is clicked, before anything is cleared: return false to keep the code and results,
	/// for example when the user chooses to keep unsaved changes.
	/// </summary>
	[Parameter] public Func<Task<bool>>? ConfirmNewAsync { get; set; }

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

	// The service whose running state and status the studio last took.
	private IPDStudioService? _stateTakenFrom;

	/// <summary>
	/// Event callback for when logging visibility changes.
	/// </summary>
	[Parameter] public EventCallback<bool> OnLoggingVisibilityChanged { get; set; }

	/// <summary>
	/// Event callback for when the user clicks a result grid row, or presses Enter on one. While it is bound,
	/// rows show a pointer and a hover highlight and can be focused.
	/// </summary>
	[Parameter] public EventCallback<PDStudioResultRowClickEventArgs> OnResultRowClick { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="PDStudio"/> class.
	/// </summary>
	public PDStudio()
	{
		// Which object raised the key press is of no interest, only which keys it was.
		_onGlobalKeyDown = (_, keyboardInfo) => OnGlobalKeyDown(keyboardInfo);
	}

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
	/// Custom content for the editor toolbar.
	/// </summary>
	[Parameter] public RenderFragment? EditorToolbarContent { get; set; }

	/// <summary>
	/// Gets or sets content shown in place of the code editor, such as another kind of editor; null shows the code
	/// editor. The code editor stays in the page, hidden, so its code and undo history are still there when this is
	/// cleared. Give it as an attribute, as content written between the tags is never null.
	/// </summary>
	[Parameter] public RenderFragment? EditorContent { get; set; }

	/// <summary>
	/// Gets or sets what Execute runs while <see cref="EditorContent"/> shows, such as the text of the host's own editor;
	/// null runs the code editor's code, as it does while the code editor shows.
	/// </summary>
	[Parameter] public Func<string>? EditorContentCode { get; set; }

	// What Execute runs: the host's editor's text while it shows in the code editor's place, otherwise the code editor's
	private string CodeToRun => EditorContent is not null && EditorContentCode is not null ? EditorContentCode() : _currentCode;

	// Whether the last render showed EditorContent, so the code editor can be laid out again once it is back
	private bool _editorContentShown;

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

			// Ctrl+Enter runs the code, so the editor's own (Insert Line Below) is turned off; the editor applies it
			// once Monaco has created it
			await RunOnEditorAsync(e => e.DisableKeyBindingAsync(13, ctrlKey: true), "disable Ctrl+Enter");
			await LeaveEditorKeysAloneAsync();
		}

		await RelayOutEditorIfBackAsync();
		await ReportActiveTabAsync();
		await InitializeLeftPanelHandleAsync();

		// Logging integration is handled through direct method calls
		await base.OnAfterRenderAsync(firstRender);
	}

	// Hidden, the code editor had no size to lay out to
	private async Task RelayOutEditorIfBackAsync()
	{
		if (_editorContentShown && EditorContent is null)
		{
			await RefreshEditorLayoutAsync();
		}

		_editorContentShown = EditorContent is not null;
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		// A service's state is taken once, when it is given: taking it on every render would let a host re-rendering
		// after a run replace the studio's own status, such as Cancelled, with the service's.
		if (StudioService != null && !ReferenceEquals(StudioService, _stateTakenFrom))
		{
			_stateTakenFrom = StudioService;
			_isExecuting = StudioService.IsExecuting;
			_executionStatus = StudioService.CurrentStatus.ToDisplayString();
		}

		TakeLeftPanelWidth();
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
		var code = CodeToRun;
		if (StudioService == null || string.IsNullOrWhiteSpace(code))
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

			await RunCodeAsync(StudioService, code, _cancellationTokenSource.Token);
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
	private async Task RunCodeAsync(IPDStudioService studioService, string code, CancellationToken cancellationToken)
	{
		await ClearResultsAsync();

		if (studioService is IPDStudioResultSetService tabular
			&& await tabular.ExecuteResultSetAsync(code, Options.Language, Options.ExecutionTimeoutSeconds, cancellationToken) is { } resultSet)
		{
			_resultSet = resultSet;
			_executionStatus = StudioExecutionStatus.Complete.ToDisplayString();
			await NotifyCodeExecutedAsync(code);
			LogSuccess(resultSet.DurationText is { } duration
				? $"{resultSet.RowCountText} returned in {duration}"
				: $"{resultSet.RowCountText} returned");
			return;
		}

		var result = await studioService.ExecuteCodeAsync(
			code,
			Options.Language,
			null,
			Options.ExecutionTimeoutSeconds,
			cancellationToken);

		await ShowResultsAsync(result);
		await NotifyCodeExecutedAsync(code);

		LogSuccess("Code execution completed successfully");
	}

	private async Task NotifyCodeExecutedAsync(string code)
	{
		if (OnCodeExecuted.HasDelegate)
		{
			await OnCodeExecuted.InvokeAsync(code);
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

	/// <summary>
	/// Does what the New button does: asks <see cref="ConfirmNewAsync"/> first, then clears the code and results.
	/// For a host that offers New in its own menu, with <see cref="PDStudioOptions.ShowNewButton"/> off.
	/// </summary>
	public async Task NewAsync()
	{
		if (ConfirmNewAsync is not null && !await ConfirmNewAsync())
		{
			return;
		}

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
	/// Changes the code editor's options while the studio is open, such as its tab size or whether it suggests as you
	/// type. Options given before the editor has started are applied once it has.
	/// </summary>
	public Task UpdateEditorOptionsAsync(EditorUpdateOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		return RunOnEditorAsync(e => e.UpdateOptions(options), "update its options");
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

		DisposeLeftPanelInterop();
		GC.SuppressFinalize(this);
	}
}
