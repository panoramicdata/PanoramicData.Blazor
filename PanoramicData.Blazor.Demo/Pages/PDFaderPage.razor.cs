using Microsoft.AspNetCore.Components;
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

	// Map internal 0.0-1.0 value to -10 to +10 display range
	private double GetDisplayValue()
	{
		return (_faderValue * 20.0) - 10.0;
	}

	// Get the step value for the display input based on snap setting
	private double GetDisplayStep()
	{
		return _snap ? 1.0 : 0.1;
	}

	// Handle changes from the display value input (converts -10 to +10 back to 0.0-1.0)
	private void OnDisplayValueChanged(ChangeEventArgs e)
	{
		if (e.Value != null && double.TryParse(e.Value.ToString(), out var displayValue))
		{
			// Clamp to -10 to +10 range
			displayValue = Math.Clamp(displayValue, -10, 10);
			// Convert to 0.0-1.0 range
			_faderValue = (displayValue + 10.0) / 20.0;
		}
	}
}
