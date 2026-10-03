using PanoramicData.Blazor.Models.ColorPicker;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDColorPickerPage
{
	protected string Color1 { get; set; } = "#3498db";
	protected string Color2 { get; set; } = "#E91E63";
	protected string Color3 { get; set; } = "rgba(52, 152, 219, 0.7)";
	protected string Color4 { get; set; } = "#27ae60";
	protected string Color5 { get; set; } = "#9b59b6";
	protected string Color6 { get; set; } = "#e74c3c";
	protected string Color7 { get; set; } = "#1abc9c";
	protected string Color8 { get; set; } = "#f39c12";
	protected string ColorHsv { get; set; } = "#ff6b35";
	protected string ColorHsva { get; set; } = "rgba(255, 107, 53, 0.8)";

	private readonly List<string> _recentColors = ["#FF5722", "#2196F3", "#4CAF50"];
	private readonly List<string> _events = [];

	private readonly ColorPickerOptions _paletteOptions = new()
	{
		ShowPalette = true,
		ShowRecentColors = false,
		EnabledSelectors = ColorSpaceSelector.SaturationValueSquare | ColorSpaceSelector.HueStrip,
		PaletteColumns = 8,
		SwatchSize = 28
	};

	private readonly ColorPickerOptions _transparencyOptions = new()
	{
		AllowTransparency = true,
		EnabledSelectors = ColorSpaceSelector.SaturationValueSquare | ColorSpaceSelector.HueStrip | ColorSpaceSelector.AlphaSlider,
		ShowPalette = false
	};

	private readonly ColorPickerOptions _recentOptions = new()
	{
		ShowPalette = true,
		ShowRecentColors = true,
		MaxRecentColors = 8,
		PaletteColumns = 8
	};

	private readonly ColorPickerOptions _slidersOptions = new()
	{
		EnabledSelectors = ColorSpaceSelector.RGBSliders | ColorSpaceSelector.HueStrip,
		ShowPalette = false
	};

	private readonly ColorPickerOptions _hsvOptions = new()
	{
		EnabledSelectors = ColorSpaceSelector.HSVSliders,
		EnabledModes = ColorMode.HSV | ColorMode.Hex,
		ShowPalette = false,
		ShowButtons = false,
		LivePreview = true
	};

	private readonly ColorPickerOptions _hsvaOptions = new()
	{
		EnabledSelectors = ColorSpaceSelector.HSVSliders | ColorSpaceSelector.AlphaSlider,
		EnabledModes = ColorMode.HSV | ColorMode.Hex,
		AllowTransparency = true,
		ShowPalette = false,
		ShowButtons = false,
		LivePreview = true
	};

	private readonly ColorPickerOptions _minimalOptions = new()
	{
		EnabledSelectors = ColorSpaceSelector.None,
		ShowPalette = true,
		ShowPreview = false,
		ShowInputs = false,
		ShowButtons = false,
		CloseOnSelect = true,
		PaletteColumns = 10,
		SwatchSize = 22,
		PopupWidth = 240
	};

	private readonly ColorPickerOptions _liveOptions = new()
	{
		LivePreview = true,
		ShowButtons = false,
		CloseOnOutsideClick = true,
		ShowPalette = false
	};

	private readonly ColorPickerOptions _confirmOptions = new()
	{
		LivePreview = false,
		ShowButtons = true,
		ShowPalette = false
	};

	private void OnRecentColorsChanged(List<string> colors)
	{
		_events.Add($"{DateTime.Now:HH:mm:ss} - Recent colors updated: {colors.Count} colors");
		StateHasChanged();
	}

	private void OnColor7Selected(string color)
	{
		_events.Add($"{DateTime.Now:HH:mm:ss} - Live color selected: {color}");
		StateHasChanged();
	}

	private void OnColor8Selected(string color)
	{
		_events.Add($"{DateTime.Now:HH:mm:ss} - Confirmed color selected: {color}");
		StateHasChanged();
	}
}
