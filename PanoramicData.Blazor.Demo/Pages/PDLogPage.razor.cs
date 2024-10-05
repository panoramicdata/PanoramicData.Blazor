using Microsoft.Extensions.Logging;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDLogPage
{
	private PDLog? _log;
	private string _rows = "10";
	private string _capacity = "20";
	private bool _wordWrap = true;
	private bool _reverse = true;
	private bool _tail = false;
	private bool _useLocalTime = false;
	private bool _showTimestamp = true;
	private bool _showException = true;
	private bool _showIcon = true;
	private LogLevel _logLevel = LogLevel.Information;

	private List<MenuItem> LogLevelMenuItems => Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>().Select(ll => new MenuItem { Key = ll.ToString(), Text = ll.ToString() }).ToList();

	private string LogTimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

	private void Log(LogLevel logLevel, bool isLongEntry = false, bool isException = false)
	{
		if (isException)
		{
			_log?.Log(
					logLevel,
					new InvalidOperationException("Example Invalid Operation Exception"),
					isLongEntry
						? "Some {LogLevel} text\nLorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum."
						: "Some {LogLevel} text",
					logLevel);
			return;
		}

		_log?.Log(
				logLevel,
				isLongEntry
					? "Some {LogLevel} text\nLorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum."
					: "Some {LogLevel} text",
				logLevel);
	}

	private void OnLogLevelClick(string key)
	{
		_logLevel = Enum.Parse<LogLevel>(key);
		StateHasChanged();
	}

	private void Clear()
		=> _log?.Clear();
}