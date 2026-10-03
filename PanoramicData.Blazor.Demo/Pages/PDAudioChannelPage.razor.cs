namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDAudioChannelPage
{
	// Interactive Controls section
	protected double FaderValue { get; set; } = 0.7;
	protected double GainValue { get; set; } = 0.6;
	protected double CompValue { get; set; } = 0.5;
	protected double EqHighValue { get; set; } = 0.5;
	protected double EqMidValue { get; set; } = 0.5;
	protected double EqLowValue { get; set; } = 0.5;
	protected double DspValue { get; set; } = 0.5;
	protected double PanValue { get; set; } = 0.5;
	protected double PflValue { get; set; }
	protected double MuteValue { get; set; }

	// Custom Colors section
	protected double GuitarFader { get; set; } = 0.8;
	protected double GuitarGain { get; set; } = 0.5;
	protected double GuitarComp { get; set; } = 0.5;
	protected double GuitarPfl { get; set; }
	protected double GuitarMute { get; set; }
	protected double BassFader { get; set; } = 0.6;
	protected double BassGain { get; set; } = 0.5;
	protected double BassPfl { get; set; }
	protected double BassMute { get; set; }
	protected double DrumsFader { get; set; } = 0.9;
	protected double DrumsGain { get; set; } = 0.5;
	protected double DrumsPfl { get; set; }
	protected double DrumsMute { get; set; }

	// Multiple Channels section - ALL controls for each channel
	protected double Multi1Fader { get; set; } = 0.75;
	protected double Multi1Gain { get; set; } = 0.5;
	protected double Multi1Comp { get; set; } = 0.5;
	protected double Multi1EqHigh { get; set; } = 0.5;
	protected double Multi1EqMid { get; set; } = 0.5;
	protected double Multi1EqLow { get; set; } = 0.5;
	protected double Multi1Dsp { get; set; } = 0.5;
	protected double Multi1Pan { get; set; } = 0.5;
	protected double Multi1Pfl { get; set; }
	protected double Multi1Mute { get; set; }

	protected double Multi2Fader { get; set; } = 0.60;
	protected double Multi2Gain { get; set; } = 0.7;
	protected double Multi2Comp { get; set; } = 0.5;
	protected double Multi2EqHigh { get; set; } = 0.5;
	protected double Multi2EqMid { get; set; } = 0.5;
	protected double Multi2EqLow { get; set; } = 0.5;
	protected double Multi2Dsp { get; set; } = 0.5;
	protected double Multi2Pan { get; set; } = 0.5;
	protected double Multi2Pfl { get; set; }
	protected double Multi2Mute { get; set; }

	protected double Multi3Fader { get; set; } = 0.85;
	protected double Multi3Gain { get; set; } = 0.5;
	protected double Multi3Comp { get; set; } = 0.5;
	protected double Multi3EqHigh { get; set; } = 0.5;
	protected double Multi3EqMid { get; set; } = 0.5;
	protected double Multi3EqLow { get; set; } = 0.5;
	protected double Multi3Dsp { get; set; } = 0.5;
	protected double Multi3Pan { get; set; } = 0.3;
	protected double Multi3Pfl { get; set; }
	protected double Multi3Mute { get; set; }

	protected double Multi4Fader { get; set; } = 0.50;
	protected double Multi4Gain { get; set; } = 0.5;
	protected double Multi4Comp { get; set; } = 0.5;
	protected double Multi4EqHigh { get; set; } = 0.5;
	protected double Multi4EqMid { get; set; } = 0.5;
	protected double Multi4EqLow { get; set; } = 0.5;
	protected double Multi4Dsp { get; set; } = 0.5;
	protected double Multi4Pan { get; set; } = 0.7;
	protected double Multi4Pfl { get; set; }
	protected double Multi4Mute { get; set; }

	protected double Multi5Fader { get; set; } = 0.65;
	protected double Multi5Gain { get; set; } = 0.5;
	protected double Multi5Comp { get; set; } = 0.5;
	protected double Multi5EqHigh { get; set; } = 0.5;
	protected double Multi5EqMid { get; set; } = 0.5;
	protected double Multi5EqLow { get; set; } = 0.5;
	protected double Multi5Dsp { get; set; } = 0.5;
	protected double Multi5Pan { get; set; } = 0.5;
	protected double Multi5Pfl { get; set; }
	protected double Multi5Mute { get; set; } = 1;

	// Separate event managers for each section
	private readonly EventManager _colorEventManager = new();
	private readonly EventManager _interactiveEventManager = new();
	private readonly EventManager _multiEventManager = new();

	private double OnValueChanged(string control, double value)
	{
		var evt = new Event($"{control}Changed", new EventArgument("Value", value.ToString("F3")));
		_interactiveEventManager.Add(evt);
		return value;
	}

	private double OnColorDemoValueChanged(string channel, string control, double value)
	{
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		_colorEventManager.Add(evt);
		return value;
	}

	private double OnMultiChannelValueChanged(string channel, string control, double value)
	{
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		_multiEventManager.Add(evt);
		return value;
	}
}
