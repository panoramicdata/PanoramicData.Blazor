using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Colour pickers and colour normalisation for the PDTiles demo.
/// </summary>
public partial class PDTilesPage
{
	private readonly ColorPickerOptions _colorPickerOptions = CreateColorPickerOptions(false);

	// The background colour picker also allows transparency
	private readonly ColorPickerOptions _bgColorPickerOptions = CreateColorPickerOptions(true);

	private static ColorPickerOptions CreateColorPickerOptions(bool allowTransparency) => new()
	{
		ShowPalette = true,
		ShowRecentColors = false,
		AllowTransparency = allowTransparency,
		EnabledSelectors = allowTransparency
			? ColorSpaceSelector.SaturationValueSquare | ColorSpaceSelector.HueStrip | ColorSpaceSelector.AlphaSlider
			: ColorSpaceSelector.SaturationValueSquare | ColorSpaceSelector.HueStrip,
		PaletteColumns = 8,
		SwatchSize = 24,
		ShowButtons = false,
		LivePreview = true,
		CloseOnOutsideClick = true,
		PopupWidth = 260,
		SelectorHeight = 120
	};

	private void OnTileColorChanged(string color)
	{
		_options.TileColor = NormalizeColor(color);
		OnOptionsChanged();
	}

	private void OnBackgroundColorChanged(string color)
	{
		// Keep rgba format for transparency support
		_options.BackgroundColor = color;
		OnOptionsChanged();
	}

	private void OnLineColorChanged(string color)
	{
		_options.LineColor = NormalizeColor(color);
		OnOptionsChanged();
	}

	private static string NormalizeColor(string color)
	{
		// PDToolbarColorPicker may return rgba() format, convert to hex if needed
		if (color.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase) ||
			color.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase))
		{
			var colorValue = ColorValue.FromHex("#000000");
			// Parse rgb/rgba format
			var values = color
				.Replace("rgba(", "", StringComparison.OrdinalIgnoreCase)
				.Replace("rgb(", "", StringComparison.OrdinalIgnoreCase)
				.Replace(")", "")
				.Split(',');

			if (values.Length >= 3 &&
				byte.TryParse(values[0].Trim(), out var r) &&
				byte.TryParse(values[1].Trim(), out var g) &&
				byte.TryParse(values[2].Trim(), out var b))
			{
				colorValue.SetRgb(r, g, b);
				return colorValue.ToHex();
			}
		}

		// Already hex or other format
		return color;
	}
}
