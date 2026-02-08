namespace PanoramicData.Blazor.Models.ColorPicker;

/// <summary>
/// Defines the available color input modes.
/// </summary>
[Flags]
public enum ColorMode
{
	/// <summary>
	/// No color modes enabled.
	/// </summary>
	None = 0,

	/// <summary>
	/// RGB color mode (Red, Green, Blue).
	/// </summary>
	RGB = 1,

	/// <summary>
	/// RGBA color mode (Red, Green, Blue, Alpha).
	/// </summary>
	RGBA = 2,

	/// <summary>
	/// HSV/HSB color mode (Hue, Saturation, Value/Brightness).
	/// </summary>
	HSV = 4,

	/// <summary>
	/// HSL color mode (Hue, Saturation, Lightness).
	/// </summary>
	HSL = 8,

	/// <summary>
	/// Hexadecimal color mode.
	/// </summary>
	Hex = 16,

	/// <summary>
	/// All color modes enabled.
	/// </summary>
	All = RGB | RGBA | HSV | HSL | Hex
}
