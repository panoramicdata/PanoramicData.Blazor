using PanoramicData.Blazor.PDLogData;

namespace PanoramicData.Blazor;

public partial class PDLog : ILogger
{
	private readonly List<LogEntry> _logEntries = [];

	private ElementReference _logContainer;

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

	//[Parameter]
	//public bool Tail { get; set; } = true;

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
	[Parameter] public bool WordWrap { get; set; } = false; // Toggle for word wrapping

	/// <summary>
	/// Gets or sets whether to automatically scroll to the bottom of the log.
	/// </summary>
	[Parameter] public bool Tail { get; set; } = false; // Auto-scroll to bottom

	/// <summary>
	/// Gets or sets whether to display timestamps in local time.
	/// </summary>
	[Parameter] public bool UseLocalTime { get; set; } = false;

	/// <summary>
	/// Gets or sets whether to display log entries in reverse chronological order.
	/// </summary>
	[Parameter] public bool Reverse { get; set; } = false;

	private List<LogEntry> OrderedEntries => (Reverse ? [.. _logEntries.OrderByDescending(x => x.Timestamp)] : _logEntries);

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

	public void Clear()
	{
		_logEntries.Clear();
		StateHasChanged();
	}

	protected override void OnParametersSet()
	{
		if (Capacity < 1)
		{
			Capacity = 1;
		}

		if (Capacity < _logEntries.Count)
		{
			_logEntries.RemoveRange(0, _logEntries.Count - Capacity);
		}

		base.OnParametersSet();
	}

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;

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

		var entry = new LogEntry
		{
			LogLevel = logLevel,
			Message = message,
			Icon = GetLogLevelIcon(logLevel),
			Timestamp = DateTime.UtcNow,
			TimestampClass = GetLogLevelTimestampClass(logLevel),
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

	private static string GetLogLevelIcon(LogLevel logLevel) => logLevel switch
	{
		LogLevel.Information => "fas fa-info-circle text-info",
		LogLevel.Warning => "fas fa-exclamation-triangle text-warning",
		LogLevel.Error => "fas fa-times-circle text-danger",
		LogLevel.Critical => "fas fa-bomb text-danger",
		LogLevel.Debug => "fas fa-bug text-secondary",
		LogLevel.Trace => "fas fa-search text-primary",
		_ => "fas fa-info-circle text-muted"
	};

	// Get the timestamp color class matching the log level
	private static string GetLogLevelTimestampClass(LogLevel logLevel) => logLevel switch
	{
		LogLevel.Information => "text-info",
		LogLevel.Warning => "text-warning",
		LogLevel.Error => "text-danger",
		LogLevel.Critical => "text-danger",
		LogLevel.Debug => "text-secondary",
		LogLevel.Trace => "text-primary",
		_ => "text-muted"
	};


	private async Task ScrollToBottomAsync()
	{
		if (_commonModule is null)
		{
			return;
		}

		// TODO - Get this working
		await _commonModule.InvokeVoidAsync("scrollToBottom", _logContainer);
	}
}