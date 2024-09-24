namespace PanoramicData.Blazor;

public partial class PDLog : ILogger
{
	[Parameter]
	public LogLevel LogLevel { get; set; } = LogLevel.Information;

	//[Parameter]
	//public bool Tail { get; set; } = true;

	[Parameter]
	public int Rows { get; set; } = 30;

	[Parameter]
	public bool UtcTimestamp { get; set; } = true;

	[Parameter]
	public string UtcTimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

	private string _result = string.Empty;
	private LogLevel _worstLogLevel = LogLevel.None;
	private PDTextArea? _textArea;

	private string Fill => _worstLogLevel switch
	{
		LogLevel.None => "transparent",
		LogLevel.Trace => "gray",
		LogLevel.Debug => "lightblue",
		LogLevel.Information => "green",
		LogLevel.Warning => "orange",
		_ => "red"
	};

	public void Clear()
	{
		_result = string.Empty;
		_worstLogLevel = LogLevel.None;
		StateHasChanged();
	}

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => throw new NotSupportedException();

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		if (logLevel > _worstLogLevel)
		{
			_worstLogLevel = logLevel;
		}

		_result += logLevel switch
		{
			LogLevel.Debug => "⚪ ",
			LogLevel.Information => "ⓘ ",
			LogLevel.Warning => "⚠ ",
			LogLevel.Error => "☒ ",
			LogLevel.Critical => "☠ ",
			_ => "⚫ ",
		};

		if (UtcTimestamp)
		{
			_result += DateTimeOffset.UtcNow.ToString(UtcTimestampFormat, CultureInfo.InvariantCulture) + " ";
		}

		_result += formatter(state, exception) + Environment.NewLine;

		//if (_textArea is not null && Tail)
		//{
		//	_ = _textArea.ScrollToEndAsync().ConfigureAwait(true);
		//}

		StateHasChanged();
	}
}