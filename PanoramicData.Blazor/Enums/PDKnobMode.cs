namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Specifies the operational mode of a knob control, which affects its visual range and default behaviour.
/// </summary>
public enum PDKnobMode
{
	/// <summary>The knob controls volume (0 to 100 %).</summary>
	Volume,
	/// <summary>The knob controls stereo balance (left to right, centred at the default).</summary>
	Balance,
	/// <summary>The knob controls gain or a general-purpose parameter.</summary>
	Gain
}