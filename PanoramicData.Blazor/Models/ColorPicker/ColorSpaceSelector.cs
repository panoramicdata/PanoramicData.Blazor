namespace PanoramicData.Blazor.Models.ColorPicker;

/// <summary>
/// Defines the available color space selectors.
/// </summary>
[Flags]
public enum ColorSpaceSelector
{
	/// <summary>
	/// No selectors enabled.
	/// </summary>
	None = 0,

	/// <summary>
	/// Square saturation/value picker with hue slider.
	/// </summary>
	SaturationValueSquare = 1,

	/// <summary>
	/// Circular hue wheel with saturation/lightness center.
	/// </summary>
	HueWheel = 2,

	/// <summary>
	/// Gradient strip selector for hue.
	/// </summary>
	HueStrip = 4,

	/// <summary>
	/// Alpha/opacity slider.
	/// </summary>
	AlphaSlider = 8,

	/// <summary>
	/// RGB sliders for red, green, blue.
	/// </summary>
	RGBSliders = 16,

	/// <summary>
	/// HSL sliders for hue, saturation, lightness.
	/// </summary>
	HSLSliders = 32,

	/// <summary>
	/// HSV sliders for hue, saturation, value.
	/// </summary>
	HSVSliders = 64,

	/// <summary>
	/// All selectors enabled.
	/// </summary>
	All = SaturationValueSquare | HueWheel | HueStrip | AlphaSlider | RGBSliders | HSLSliders | HSVSliders
}
