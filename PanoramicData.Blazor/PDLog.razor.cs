namespace PanoramicData.Blazor;

public partial class PDLog : ILogger
{
	[Parameter]
	public LogLevel LogLevel { get; set; } = LogLevel.Information;

	string _result = string.Empty;
	LogLevel _worstLogLevel = LogLevel.None;

	public void Clear()
	{
		_result = string.Empty;
		_worstLogLevel = LogLevel.None;
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
			LogLevel.Error => "☒ ",
			LogLevel.Warning => "⚠ ",
			LogLevel.Information => "• ",
			_ => "  "
		};
		_result += formatter.ToString() + "\n";

		StateHasChanged();
	}
}