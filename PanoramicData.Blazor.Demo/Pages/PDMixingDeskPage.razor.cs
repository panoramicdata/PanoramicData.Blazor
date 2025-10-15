namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMixingDeskPage
{
	// Basic Mixing Desk section - ALL controls
	private double _basic1Fader = 0.8;
	private double _basic1Gain = 0.6;
	private double _basic1Comp = 0.5;
	private double _basic1EqHigh = 0.5;
	private double _basic1EqMid = 0.5;
	private double _basic1EqLow = 0.5;
	private double _basic1Dsp = 0.5;
	private double _basic1Pan = 0.5;
	private double _basic1Pfl = 0;
	private double _basic1Mute = 0;

	private double _basic2Fader = 0.5;
	private double _basic2Gain = 0.5;
	private double _basic2Comp = 0.5;
	private double _basic2EqHigh = 0.5;
	private double _basic2EqMid = 0.5;
	private double _basic2EqLow = 0.5;
	private double _basic2Dsp = 0.5;
	private double _basic2Pan = 0.5;
	private double _basic2Pfl = 0;
	private double _basic2Mute = 0;

	private double _basic3Fader = 0.7;
	private double _basic3Gain = 0.5;
	private double _basic3Comp = 0.5;
	private double _basic3EqHigh = 0.5;
	private double _basic3EqMid = 0.5;
	private double _basic3EqLow = 0.5;
	private double _basic3Dsp = 0.5;
	private double _basic3Pan = 0.3;
	private double _basic3Pfl = 0;
	private double _basic3Mute = 0;

	private double _basic4Fader = 0.6;
	private double _basic4Gain = 0.5;
	private double _basic4Comp = 0.5;
	private double _basic4EqHigh = 0.5;
	private double _basic4EqMid = 0.5;
	private double _basic4EqLow = 0.5;
	private double _basic4Dsp = 0.5;
	private double _basic4Pan = 0.7;
	private double _basic4Pfl = 0;
	private double _basic4Mute = 0;

	private double _basic5Fader = 0.9;
	private double _basic5Gain = 0.5;
	private double _basic5Comp = 0.5;
	private double _basic5EqHigh = 0.5;
	private double _basic5EqMid = 0.5;
	private double _basic5EqLow = 0.5;
	private double _basic5Dsp = 0.5;
	private double _basic5Pan = 0.5;
	private double _basic5Mute = 0;
	private double _basic5Pfl = 0;

	// Custom Colors section - ALL controls
	private double _color1Fader = 0.7;
	private double _color1Gain = 0.5;
	private double _color1Comp = 0.5;
	private double _color1EqHigh = 0.5;
	private double _color1EqMid = 0.5;
	private double _color1EqLow = 0.5;
	private double _color1Dsp = 0.5;
	private double _color1Pan = 0.5;
	private double _color1Pfl = 0;
	private double _color1Mute = 0;

	private double _color2Fader = 0.8;
	private double _color2Gain = 0.5;
	private double _color2Comp = 0.5;
	private double _color2EqHigh = 0.5;
	private double _color2EqMid = 0.5;
	private double _color2EqLow = 0.5;
	private double _color2Dsp = 0.5;
	private double _color2Pan = 0.5;
	private double _color2Pfl = 0;
	private double _color2Mute = 0;

	private double _color3Fader = 0.9;
	private double _color3Gain = 0.5;
	private double _color3Comp = 0.5;
	private double _color3EqHigh = 0.5;
	private double _color3EqMid = 0.5;
	private double _color3EqLow = 0.5;
	private double _color3Dsp = 0.5;
	private double _color3Pan = 0.5;
	private double _color3Pfl = 0;
	private double _color3Mute = 0;

	// Interactive section - ALL controls for each channel
	private double _channel1Fader = 0.75;
	private double _channel1Gain = 0.7;
	private double _channel1Comp = 0.5;
	private double _channel1EqHigh = 0.5;
	private double _channel1EqMid = 0.5;
	private double _channel1EqLow = 0.5;
	private double _channel1Dsp = 0.5;
	private double _channel1Pan = 0.5;
	private double _channel1Mute = 0;
	private double _channel1Pfl = 0;

	private double _channel2Fader = 0.6;
	private double _channel2Gain = 0.5;
	private double _channel2Comp = 0.5;
	private double _channel2EqHigh = 0.5;
	private double _channel2EqMid = 0.5;
	private double _channel2EqLow = 0.5;
	private double _channel2Dsp = 0.5;
	private double _channel2Pan = 0.5;
	private double _channel2Mute = 0;
	private double _channel2Pfl = 0;

	private double _channel3Fader = 0.85;
	private double _channel3Gain = 0.5;
	private double _channel3Comp = 0.5;
	private double _channel3EqHigh = 0.5;
	private double _channel3EqMid = 0.5;
	private double _channel3EqLow = 0.5;
	private double _channel3Dsp = 0.5;
	private double _channel3Pan = 0.5;
	private double _channel3Mute = 0;
	private double _channel3Pfl = 0;

	private double _channel4Fader = 0.5;
	private double _channel4Gain = 0.5;
	private double _channel4Comp = 0.5;
	private double _channel4EqHigh = 0.5;
	private double _channel4EqMid = 0.5;
	private double _channel4EqLow = 0.5;
	private double _channel4Dsp = 0.5;
	private double _channel4Pan = 0.5;
	private double _channel4Mute = 0;
	private double _channel4Pfl = 0;

	// Separate event managers for each section
	private readonly EventManager _basicEventManager = new();
	private readonly EventManager _colorEventManager = new();
	private readonly EventManager _interactiveEventManager = new();

	private void OnBasicDeskValueChanged(string channel, string control, ref double field, double value)
	{
		field = value;
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		
		_basicEventManager.Add(evt);
	}

	private void OnColorDeskValueChanged(string channel, string control, ref double field, double value)
	{
		field = value;
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		
		_colorEventManager.Add(evt);
	}

	private void OnChannelValueChanged(string channel, string control, ref double field, double value)
	{
		field = value;
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		
		_interactiveEventManager.Add(evt);
	}
}
