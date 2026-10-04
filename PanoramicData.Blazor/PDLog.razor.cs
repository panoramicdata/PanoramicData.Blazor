using PanoramicData.Blazor.PDLogData;

namespace PanoramicData.Blazor;

/// <summary>
/// In-memory log viewer component that also implements <see cref="ILogger"/>.
/// </summary>
public partial class PDLog : ILogger
{
	private readonly List<LogEntry> _logEntries = [];

	/// <summary>
	/// Gets the scrolling log container element, set by the markup.
	/// </summary>
	internal ElementReference LogContainer { get; set; }

	/// <summary>
	/// Gets or sets JavaScript runtime used for scrolling interop.
	/// </summary>
	[Inject] public IJSRuntime? JSRuntime { get; set; }

	private IJSObjectReference? _commonModule;

	/// <summary>
	/// Optional CSS class to apply to the "class" attribute on the log container.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the minimum log level to display.
	/// </summary>
	[Parameter]
	public LogLevel LogLevel { get; set; } = LogLevel.Information;

	/// <summary>
	/// Gets or sets the maximum number of log entries to keep.
	/// </summary>
	[Parameter]
	public int Capacity { get; set; } = 1000;

	/// <summary>
	/// Gets or sets the number of rows to display.
	/// </summary>
	[Parameter]
	public int Rows { get; set; } = 30;

	/// <summary>
	/// Gets or sets whether to show the timestamp for each log entry.
	/// </summary>
	[Parameter]
	public bool ShowTimestamp { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the icon for each log entry.
	/// </summary>
	[Parameter]
	public bool ShowIcon { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the exception for each log entry.
	/// </summary>
	[Parameter]
	public bool ShowException { get; set; } = true;

	/// <summary>
	/// Gets or sets the format for the UTC timestamp.
	/// </summary>
	[Parameter]
	public string UtcTimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

	/// <summary>
	/// Gets or sets whether to wrap long lines.
	/// </summary>
	[Parameter] public bool WordWrap { get; set; } // Toggle for word wrapping

	/// <summary>
	/// Gets or sets whether to automatically scroll to the bottom of the log.
	/// </summary>
	[Parameter] public bool Tail { get; set; } // Auto-scroll to bottom

	/// <summary>
	/// Gets or sets whether to display timestamps in local time.
	/// </summary>
	[Parameter] public bool UseLocalTime { get; set; }

	/// <summary>
	/// Gets or sets whether to display log entries in reverse chronological order.
	/// </summary>
	[Parameter] public bool Reverse { get; set; }

	private List<LogEntry> OrderedEntries => (Reverse ? [.. _logEntries.OrderByDescending(x => x.Timestamp)] : _logEntries);

	/// <summary>
	/// Loads JavaScript helpers after first render.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
			}
			catch
			{
				// BC-40 - fast page switching in Server Side Blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	/// <summary>
	/// Clears all buffered log entries.
	/// </summary>
	public void Clear()
	{
		_logEntries.Clear();
		StateHasChanged();
	}

	/// <summary>
	/// Normalizes parameter values and enforces log capacity limits.
	/// </summary>
	protected override void OnParametersSet()
	{
		Capacity = Math.Max(Capacity, 1);

		if (Capacity < _logEntries.Count)
		{
			_logEntries.RemoveRange(0, _logEntries.Count - Capacity);
		}

		base.OnParametersSet();
	}

	/// <summary>
	/// Begins a logging scope. This implementation does not maintain scopes.
	/// </summary>
	/// <typeparam name="TState">Scope state type.</typeparam>
	/// <param name="state">Scope state.</param>
	/// <returns>Always returns null.</returns>
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	/// <summary>
	/// Determines whether the specified level is enabled for display.
	/// </summary>
	/// <param name="logLevel">Log level to test.</param>
	/// <returns>True when enabled.</returns>
	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;

	/// <summary>
	/// Appends a log entry when the requested level is enabled.
	/// </summary>
	/// <typeparam name="TState">Structured state type.</typeparam>
	/// <param name="logLevel">Entry severity.</param>
	/// <param name="eventId">Event identifier.</param>
	/// <param name="state">Structured state payload.</param>
	/// <param name="exception">Optional exception payload.</param>
	/// <param name="formatter">Formatter used to produce message text.</param>
	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel))
		{
			return;
		}

		var message = formatter(state, exception);
		var (icon, timestampClass) = _logLevelStyles.GetValueOrDefault(logLevel, _defaultLogLevelStyle);

		var entry = new LogEntry
		{
			LogLevel = logLevel,
			Message = message,
			Icon = icon,
			Timestamp = DateTime.UtcNow,
			TimestampClass = timestampClass,
			Exception = exception
		};

		_logEntries.Add(entry);

		if (_logEntries.Count > Capacity)
		{
			_logEntries.RemoveAt(0);
		}

		if (Tail)
		{
			InvokeAsync(ScrollToBottomAsync);
		}

		InvokeAsync(StateHasChanged);
	}

	private string GetTimestamp(DateTime timestamp)
		=> (UseLocalTime ? timestamp.ToLocalTime() : timestamp).ToString(UtcTimestampFormat, CultureInfo.InvariantCulture);

	private static readonly (string Icon, string TimestampClass) _defaultLogLevelStyle = ("fas fa-info-circle text-muted", "text-muted");

	// The icon and timestamp color class matching each log level
	private static readonly Dictionary<LogLevel, (string Icon, string TimestampClass)> _logLevelStyles = new()
	{
		[LogLevel.Information] = ("fas fa-info-circle text-info", "text-info"),
		[LogLevel.Warning] = ("fas fa-exclamation-triangle text-warning", "text-warning"),
		[LogLevel.Error] = ("fas fa-times-circle text-danger", "text-danger"),
		[LogLevel.Critical] = ("fas fa-bomb text-danger", "text-danger"),
		[LogLevel.Debug] = ("fas fa-bug text-secondary", "text-secondary"),
		[LogLevel.Trace] = ("fas fa-search text-primary", "text-primary"),
	};

	private async Task ScrollToBottomAsync()
	{
		if (_commonModule is null)
		{
			return;
		}

		// TODO - Get this working
		await _commonModule.InvokeVoidAsync("scrollToBottom", LogContainer);
	}
}