using Microsoft.Extensions.Logging;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDLogPage
{
	private PDLog? _log;

	private int _rows = 10;
	private bool _tail = false;
	private bool _logTimestamp = true;
	private string _logTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

	private void Log(LogLevel logLevel)
		=> _log?.Log(
			logLevel,
			"Some {LogLevel} text",
			logLevel);

	private void Clear()
		=> _log?.Clear();

	private void AddRow(int n)
	{
		_rows += n;
		if (_rows < 1)
		{
			_rows = 1;
		}

		StateHasChanged();
	}
}