namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDAudioButtonDemo
{
	private double _buttonValue1 = 0;
	private double _buttonValue2 = 0;
	private double _buttonValue3 = 0;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void OnButton1ValueChanged(double value)
	{
		_buttonValue1 = value;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Mute", value.ToString("F2"))));
	}

	private void OnButton2ValueChanged(double value)
	{
		_buttonValue2 = value;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Solo", value.ToString("F2"))));
	}

	private void OnButton3ValueChanged(double value)
	{
		_buttonValue3 = value;
		EventManager?.Add(new Event("ValueChanged", new EventArgument("Phantom", value.ToString("F2"))));
	}
}