using Microsoft.Extensions.Logging;

namespace PanoramicData.Blazor.Demo.Pages;
public partial class PDFaderPage
{
	private double _faderValue = 0.5;
	private string _faderColor = "#888";
	private bool _snap = true;
	private PDLog _log = null!;

	private void OnValueChanged(double value)
	{
		_log.Log(
			LogLevel.Information,
			new EventId(0, "ValueChanged"),
			"Value changed to {Value}",
			value);
	}
}
