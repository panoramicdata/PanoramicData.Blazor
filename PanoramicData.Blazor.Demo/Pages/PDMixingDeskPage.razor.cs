namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDMixingDeskPage
{
	// Basic Mixing Desk section - ALL controls
	protected double Basic1Fader { get; set; } = 0.8;
	protected double Basic1Gain { get; set; } = 0.6;
	protected double Basic1Comp { get; set; } = 0.5;
	protected double Basic1EqHigh { get; set; } = 0.5;
	protected double Basic1EqMid { get; set; } = 0.5;
	protected double Basic1EqLow { get; set; } = 0.5;
	protected double Basic1Dsp { get; set; } = 0.5;
	protected double Basic1Pan { get; set; } = 0.5;
	protected double Basic1Pfl { get; set; }
	protected double Basic1Mute { get; set; }

	protected double Basic2Fader { get; set; } = 0.5;
	protected double Basic2Gain { get; set; } = 0.5;
	protected double Basic2Comp { get; set; } = 0.5;
	protected double Basic2EqHigh { get; set; } = 0.5;
	protected double Basic2EqMid { get; set; } = 0.5;
	protected double Basic2EqLow { get; set; } = 0.5;
	protected double Basic2Dsp { get; set; } = 0.5;
	protected double Basic2Pan { get; set; } = 0.5;
	protected double Basic2Pfl { get; set; }
	protected double Basic2Mute { get; set; }

	protected double Basic3Fader { get; set; } = 0.7;
	protected double Basic3Gain { get; set; } = 0.5;
	protected double Basic3Comp { get; set; } = 0.5;
	protected double Basic3EqHigh { get; set; } = 0.5;
	protected double Basic3EqMid { get; set; } = 0.5;
	protected double Basic3EqLow { get; set; } = 0.5;
	protected double Basic3Dsp { get; set; } = 0.5;
	protected double Basic3Pan { get; set; } = 0.3;
	protected double Basic3Pfl { get; set; }
	protected double Basic3Mute { get; set; }

	protected double Basic4Fader { get; set; } = 0.6;
	protected double Basic4Gain { get; set; } = 0.5;
	protected double Basic4Comp { get; set; } = 0.5;
	protected double Basic4EqHigh { get; set; } = 0.5;
	protected double Basic4EqMid { get; set; } = 0.5;
	protected double Basic4EqLow { get; set; } = 0.5;
	protected double Basic4Dsp { get; set; } = 0.5;
	protected double Basic4Pan { get; set; } = 0.7;
	protected double Basic4Pfl { get; set; }
	protected double Basic4Mute { get; set; }

	protected double Basic5Fader { get; set; } = 0.9;
	protected double Basic5Gain { get; set; } = 0.5;
	protected double Basic5Comp { get; set; } = 0.5;
	protected double Basic5EqHigh { get; set; } = 0.5;
	protected double Basic5EqMid { get; set; } = 0.5;
	protected double Basic5EqLow { get; set; } = 0.5;
	protected double Basic5Dsp { get; set; } = 0.5;
	protected double Basic5Pan { get; set; } = 0.5;
	protected double Basic5Mute { get; set; }
	protected double Basic5Pfl { get; set; }

	// Custom Colors section - ALL controls
	protected double Color1Fader { get; set; } = 0.7;
	protected double Color1Gain { get; set; } = 0.5;
	protected double Color1Comp { get; set; } = 0.5;
	protected double Color1EqHigh { get; set; } = 0.5;
	protected double Color1EqMid { get; set; } = 0.5;
	protected double Color1EqLow { get; set; } = 0.5;
	protected double Color1Dsp { get; set; } = 0.5;
	protected double Color1Pan { get; set; } = 0.5;
	protected double Color1Pfl { get; set; }
	protected double Color1Mute { get; set; }

	protected double Color2Fader { get; set; } = 0.8;
	protected double Color2Gain { get; set; } = 0.5;
	protected double Color2Comp { get; set; } = 0.5;
	protected double Color2EqHigh { get; set; } = 0.5;
	protected double Color2EqMid { get; set; } = 0.5;
	protected double Color2EqLow { get; set; } = 0.5;
	protected double Color2Dsp { get; set; } = 0.5;
	protected double Color2Pan { get; set; } = 0.5;
	protected double Color2Pfl { get; set; }
	protected double Color2Mute { get; set; }

	protected double Color3Fader { get; set; } = 0.9;
	protected double Color3Gain { get; set; } = 0.5;
	protected double Color3Comp { get; set; } = 0.5;
	protected double Color3EqHigh { get; set; } = 0.5;
	protected double Color3EqMid { get; set; } = 0.5;
	protected double Color3EqLow { get; set; } = 0.5;
	protected double Color3Dsp { get; set; } = 0.5;
	protected double Color3Pan { get; set; } = 0.5;
	protected double Color3Pfl { get; set; }
	protected double Color3Mute { get; set; }

	// Interactive section - ALL controls for each channel
	protected double Channel1Fader { get; set; } = 0.75;
	protected double Channel1Gain { get; set; } = 0.7;
	protected double Channel1Comp { get; set; } = 0.5;
	protected double Channel1EqHigh { get; set; } = 0.5;
	protected double Channel1EqMid { get; set; } = 0.5;
	protected double Channel1EqLow { get; set; } = 0.5;
	protected double Channel1Dsp { get; set; } = 0.5;
	protected double Channel1Pan { get; set; } = 0.5;
	protected double Channel1Mute { get; set; }
	protected double Channel1Pfl { get; set; }

	protected double Channel2Fader { get; set; } = 0.6;
	protected double Channel2Gain { get; set; } = 0.5;
	protected double Channel2Comp { get; set; } = 0.5;
	protected double Channel2EqHigh { get; set; } = 0.5;
	protected double Channel2EqMid { get; set; } = 0.5;
	protected double Channel2EqLow { get; set; } = 0.5;
	protected double Channel2Dsp { get; set; } = 0.5;
	protected double Channel2Pan { get; set; } = 0.5;
	protected double Channel2Mute { get; set; }
	protected double Channel2Pfl { get; set; }

	protected double Channel3Fader { get; set; } = 0.85;
	protected double Channel3Gain { get; set; } = 0.5;
	protected double Channel3Comp { get; set; } = 0.5;
	protected double Channel3EqHigh { get; set; } = 0.5;
	protected double Channel3EqMid { get; set; } = 0.5;
	protected double Channel3EqLow { get; set; } = 0.5;
	protected double Channel3Dsp { get; set; } = 0.5;
	protected double Channel3Pan { get; set; } = 0.5;
	protected double Channel3Mute { get; set; }
	protected double Channel3Pfl { get; set; }

	protected double Channel4Fader { get; set; } = 0.5;
	protected double Channel4Gain { get; set; } = 0.5;
	protected double Channel4Comp { get; set; } = 0.5;
	protected double Channel4EqHigh { get; set; } = 0.5;
	protected double Channel4EqMid { get; set; } = 0.5;
	protected double Channel4EqLow { get; set; } = 0.5;
	protected double Channel4Dsp { get; set; } = 0.5;
	protected double Channel4Pan { get; set; } = 0.5;
	protected double Channel4Mute { get; set; }
	protected double Channel4Pfl { get; set; }

	// Separate event managers for each section
	private readonly EventManager _basicEventManager = new();
	private readonly EventManager _colorEventManager = new();
	private readonly EventManager _interactiveEventManager = new();

	private double OnBasicDeskValueChanged(string channel, string control, double value)
	{
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		
		_basicEventManager.Add(evt);
		return value;
	}

	private double OnColorDeskValueChanged(string channel, string control, double value)
	{
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		
		_colorEventManager.Add(evt);
		return value;
	}

	private double OnChannelValueChanged(string channel, string control, double value)
	{
		var evt = new Event($"{channel} {control}Changed", 
			new EventArgument("Channel", channel),
			new EventArgument("Control", control),
			new EventArgument("Value", value.ToString("F3")));
		
		_interactiveEventManager.Add(evt);
		return value;
	}
}
