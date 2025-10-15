namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDAudioChannelPage
{
	// Interactive Controls section
	private double _faderValue = 0.7;
	private double _gainValue = 0.6;
	private double _compValue = 0.5;
	private double _eqHighValue = 0.5;
	private double _eqMidValue = 0.5;
	private double _eqLowValue = 0.5;
	private double _dspValue = 0.5;
	private double _panValue = 0.5;
	private double _pflValue = 0;
	private double _muteValue = 0;

	// Custom Colors section
	private double _guitarFader = 0.8;
	private double _guitarGain = 0.5;
	private double _guitarComp = 0.5;
	private double _guitarPfl = 0;
	private double _guitarMute = 0;
	private double _bassFader = 0.6;
	private double _bassGain = 0.5;
	private double _bassPfl = 0;
	private double _bassMute = 0;
	private double _drumsFader = 0.9;
	private double _drumsGain = 0.5;
	private double _drumsPfl = 0;
	private double _drumsMute = 0;

	// Multiple Channels section - ALL controls for each channel
	private double _multi1Fader = 0.75;
	private double _multi1Gain = 0.5;
	private double _multi1Comp = 0.5;
	private double _multi1EqHigh = 0.5;
	private double _multi1EqMid = 0.5;
	private double _multi1EqLow = 0.5;
	private double _multi1Dsp = 0.5;
	private double _multi1Pan = 0.5;
	private double _multi1Pfl = 0;
	private double _multi1Mute = 0;

	private double _multi2Fader = 0.60;
	private double _multi2Gain = 0.7;
	private double _multi2Comp = 0.5;
	private double _multi2EqHigh = 0.5;
	private double _multi2EqMid = 0.5;
	private double _multi2EqLow = 0.5;
	private double _multi2Dsp = 0.5;
	private double _multi2Pan = 0.5;
	private double _multi2Pfl = 0;
	private double _multi2Mute = 0;

	private double _multi3Fader = 0.85;
	private double _multi3Gain = 0.5;
	private double _multi3Comp = 0.5;
	private double _multi3EqHigh = 0.5;
	private double _multi3EqMid = 0.5;
	private double _multi3EqLow = 0.5;
	private double _multi3Dsp = 0.5;
	private double _multi3Pan = 0.3;
	private double _multi3Pfl = 0;
	private double _multi3Mute = 0;

	private double _multi4Fader = 0.50;
	private double _multi4Gain = 0.5;
	private double _multi4Comp = 0.5;
	private double _multi4EqHigh = 0.5;
	private double _multi4EqMid = 0.5;
	private double _multi4EqLow = 0.5;
	private double _multi4Dsp = 0.5;
	private double _multi4Pan = 0.7;
	private double _multi4Pfl = 0;
	private double _multi4Mute = 0;

	private double _multi5Fader = 0.65;
	private double _multi5Gain = 0.5;
	private double _multi5Comp = 0.5;
	private double _multi5EqHigh = 0.5;
	private double _multi5EqMid = 0.5;
	private double _multi5EqLow = 0.5;
	private double _multi5Dsp = 0.5;
	private double _multi5Pan = 0.5;
	private double _multi5Pfl = 0;
	private double _multi5Mute = 1;

	// Separate event managers for each section
	private readonly EventManager _colorEventManager = new();
	private readonly EventManager _interactiveEventManager = new();
	private readonly EventManager _multiEventManager = new();

	private void OnValueChanged(string control, ref double field, double value)
	{
		field = value;
		var evt = new Event($"{control}Changed", new EventArgument("Value", value.ToString("F3")));
		_interactiveEventManager.Add(evt);
	}

	private void OnColorDemoValueChanged(string channel, string control, ref double field, double value)
	{
		field = value;
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		_colorEventManager.Add(evt);
	}

	private void OnMultiChannelValueChanged(string channel, string control, ref double field, double value)
	{
		field = value;
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		_multiEventManager.Add(evt);
	}
}
