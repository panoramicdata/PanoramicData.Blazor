namespace PanoramicData.Blazor.Models.ColorPicker;

/// <summary>
/// Configuration options for the PDToolbarColorPicker component.
/// </summary>
public class ColorPickerOptions
{
	/// <summary>
	/// Gets or sets the enabled color modes.
	/// </summary>
	public ColorMode EnabledModes { get; set; } = ColorMode.All;

	private ColorMode? _defaultMode;

	/// <summary>
	/// Gets or sets the default color mode to display.
	/// </summary>
	/// <remarks>
	/// The picker opens with the inputs for this mode: <see cref="ColorMode.RGB"/> or <see cref="ColorMode.RGBA"/>
	/// show the RGB inputs, <see cref="ColorMode.HSV"/> or <see cref="ColorMode.HSL"/> the HSV inputs, and
	/// <see cref="ColorMode.Hex"/> the hex input. Any other value shows the RGB inputs.
	/// The property reads <see cref="ColorMode.Hex"/> until it is set, but it only takes effect once it has been set:
	/// a picker whose options never set it opens with the RGB inputs, as it always has.
	/// </remarks>
	public ColorMode DefaultMode
	{
		get => _defaultMode ?? ColorMode.Hex;
		set => _defaultMode = value;
	}

	/// <summary>
	/// Gets whether <see cref="DefaultMode"/> has been set explicitly.
	/// </summary>
	internal bool IsDefaultModeSet => _defaultMode.HasValue;

	/// <summary>
	/// Gets or sets the enabled color space selectors.
	/// </summary>
	public ColorSpaceSelector EnabledSelectors { get; set; } = ColorSpaceSelector.SaturationValueSquare | ColorSpaceSelector.HueStrip | ColorSpaceSelector.AlphaSlider;

	/// <summary>
	/// Gets or sets whether transparency (alpha) is allowed.
	/// </summary>
	public bool AllowTransparency { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show a preview of the selected color.
	/// </summary>
	public bool ShowPreview { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the color value inputs.
	/// </summary>
	public bool ShowInputs { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the palette section.
	/// </summary>
	public bool ShowPalette { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show recently chosen colors.
	/// </summary>
	public bool ShowRecentColors { get; set; } = true;

	/// <summary>
	/// Gets or sets the maximum number of recent colors to remember.
	/// </summary>
	public int MaxRecentColors { get; set; } = 10;

	/// <summary>
	/// Gets or sets the number of columns in the palette grid.
	/// </summary>
	public int PaletteColumns { get; set; } = 8;

	/// <summary>
	/// Gets or sets the size of palette swatches in pixels.
	/// </summary>
	public int SwatchSize { get; set; } = 24;

	/// <summary>
	/// Gets or sets whether to show a "No Color" option.
	/// </summary>
	public bool ShowNoColor { get; set; }

	/// <summary>
	/// Gets or sets the text for the "No Color" option.
	/// </summary>
	public string NoColorText { get; set; } = "No Color";

	/// <summary>
	/// Gets or sets the width of the picker popup in pixels.
	/// </summary>
	public int PopupWidth { get; set; } = 280;

	/// <summary>
	/// Gets or sets the height of the saturation/value selector in pixels.
	/// </summary>
	public int SelectorHeight { get; set; } = 150;

	/// <summary>
	/// Gets or sets whether clicking outside closes the picker.
	/// </summary>
	public bool CloseOnOutsideClick { get; set; } = true;

	/// <summary>
	/// Gets or sets whether selecting a color closes the picker.
	/// </summary>
	public bool CloseOnSelect { get; set; }

	/// <summary>
	/// Gets or sets whether to show OK/Cancel buttons.
	/// </summary>
	public bool ShowButtons { get; set; } = true;

	/// <summary>
	/// Gets or sets the OK button text.
	/// </summary>
	public string OkButtonText { get; set; } = "OK";

	/// <summary>
	/// Gets or sets the Cancel button text.
	/// </summary>
	public string CancelButtonText { get; set; } = "Cancel";

	/// <summary>
	/// Gets or sets whether to update the value in real-time as the user selects.
	/// </summary>
	public bool LivePreview { get; set; } = true;
}
