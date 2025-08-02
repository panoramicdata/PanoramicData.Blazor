namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDAudioPadDemo
{
	private double _padValue1 = 0;
	private double _padValue2 = 0;
	private double _padValue3 = 0;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnPad1ValueChanged(double value)
	{
		_padValue1 = value;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Cue", value.ToString("F2"))));
	}

	private void OnPad2ValueChanged(double value)
	{
		_padValue2 = value;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Talkback", value.ToString("F2"))));
	}

	private void OnPad3ValueChanged(double value)
	{
		_padValue3 = value;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Record", value.ToString("F2"))));
	}
}