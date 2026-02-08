using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Models.ColorPicker;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTilesPage
{
	[Inject]
	private NavigationManager NavigationManager { get; set; } = null!;

	private PDTiles? _tilesComponent;
	private bool _isInitialized;
	private bool _showChildContent = true;


	private readonly TileGridOptions _options = new()
	{
		Columns = 3,
		Rows = 3,
		Depth = 15,
		LogoSize = 85,
		LogoRotation = 0,
		Gap = 100,
		Population = 100,
		TileColor = "#373737",
		BackgroundColor = "#000624",
		LineColor = "#c0c0c0",
		LineOpacity = 15,
		Glow = 30,
		GlowFalloff = 100,
		Perspective = 0,
		Reflection = 50,
		ReflectionDepth = 150,
		Scale = 100,
		Padding = 5,
		Alignment = GridAlignment.MiddleRight
	};

	private readonly TileConnectorOptions _connectorOptions = new()
	{
		FillPattern = ConnectorFillPattern.Random,
		Direction = ConnectorDirection.All,
		PerEdge = null,
		Population = 50,
		Height = 80,
		VerticalAlign = ConnectorVerticalAlign.Center,
		Opacity = 80,
		Animation = true,
		AnimationSpeed = 35
	};

	private List<TileConnector> _connectors = [];

	private readonly List<string> _logos =
	[
		"_content/PanoramicData.Blazor.Demo/images/tiles/Admin Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/AlertMagic Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/CaseMagic Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/CodeMagic Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/ConnectMagicLogo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/DataMagic Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/Magic Suite Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/Merlin Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/MonitorMagic Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/ProMagic Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/ReportMagic Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/SchemaMagic Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/AlertMagic Azure Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/Azure Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/Cisco Meraki Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/Docs Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/LogicMonitor Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/NCalc101Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/ReportMagic 4 Logo.svg",
		"_content/PanoramicData.Blazor.Demo/images/tiles/ThousandEyes Logo.svg"
	];

	// Static menu items for dropdowns
	private readonly List<MenuItem> _alignmentItems =
	[
		new() { Key = "TopLeft", Text = "TL" },
		new() { Key = "TopCenter", Text = "TC" },
		new() { Key = "TopRight", Text = "TR" },
		new() { Key = "MiddleLeft", Text = "ML" },
		new() { Key = "MiddleCenter", Text = "MC" },
		new() { Key = "MiddleRight", Text = "MR" },
		new() { Key = "BottomLeft", Text = "BL" },
		new() { Key = "BottomCenter", Text = "BC" },
		new() { Key = "BottomRight", Text = "BR" }
	];

	private readonly List<MenuItem> _patternItems =
	[
		new() { Key = "Random", Text = "Random" },
		new() { Key = "Solid", Text = "Solid" },
		new() { Key = "Bars", Text = "Bars" },
		new() { Key = "Chevrons", Text = "Chevrons" }
	];

	private readonly List<MenuItem> _directionItems =
	[
		new() { Key = "All", Text = "All" },
		new() { Key = "Orthogonal", Text = "Ortho" },
		new() { Key = "Diagonal", Text = "Diag" },
		new() { Key = "DiagonalLeftRight", Text = "D-LR" },
		new() { Key = "DiagonalFrontBack", Text = "D-FB" }
	];

	private readonly List<MenuItem> _perEdgeItems =
	[
		new() { Key = "", Text = "Rnd" },
		new() { Key = "1", Text = "1" },
		new() { Key = "2", Text = "2" },
		new() { Key = "3", Text = "3" },
		new() { Key = "4", Text = "4" }
	];

	private readonly List<MenuItem> _verticalAlignItems =
	[
		new() { Key = "Bottom", Text = "Bot" },
		new() { Key = "Center", Text = "Mid" },
		new() { Key = "Top", Text = "Top" }
	];

	private readonly List<MenuItem> _animSpeedItems =
	[
		new() { Key = "0", Text = "Off" },
		new() { Key = "15", Text = "Slow" },
		new() { Key = "35", Text = "Medium" },
		new() { Key = "60", Text = "Fast" },
		new() { Key = "100", Text = "V.Fast" }
	];

	private readonly List<MenuItem> _maxSizeItems =
	[
		new() { Key = "", Text = "None" },
		new() { Key = "25", Text = "25%" },
		new() { Key = "33", Text = "33%" },
		new() { Key = "50", Text = "50%" },
		new() { Key = "66", Text = "66%" },
		new() { Key = "75", Text = "75%" },
		new() { Key = "100", Text = "100%" }
	];

	private readonly ColorPickerOptions _colorPickerOptions = new()
	{
		ShowPalette = true,
		ShowRecentColors = false,
		AllowTransparency = false,
		EnabledSelectors = ColorSpaceSelector.SaturationValueSquare | ColorSpaceSelector.HueStrip,
		PaletteColumns = 8,
		SwatchSize = 24,
		ShowButtons = false,
		LivePreview = true,
		CloseOnOutsideClick = true,
		PopupWidth = 260,
		SelectorHeight = 120
	};

	private readonly ColorPickerOptions _bgColorPickerOptions = new()
	{
		ShowPalette = true,
		ShowRecentColors = false,
		AllowTransparency = true,
		EnabledSelectors = ColorSpaceSelector.SaturationValueSquare | ColorSpaceSelector.HueStrip | ColorSpaceSelector.AlphaSlider,
		PaletteColumns = 8,
		SwatchSize = 24,
		ShowButtons = false,
		LivePreview = true,
		CloseOnOutsideClick = true,
		PopupWidth = 260,
		SelectorHeight = 120
	};

	private string _lastEvent = "None";
	private string _lastEventIcon = "";

	// Helper methods for short text display
	private string GetAlignmentShortText() => _options.Alignment switch
	{
		GridAlignment.TopLeft => "TL",
		GridAlignment.TopCenter => "TC",
		GridAlignment.TopRight => "TR",
		GridAlignment.MiddleLeft => "ML",
		GridAlignment.MiddleCenter => "MC",
		GridAlignment.MiddleRight => "MR",
		GridAlignment.BottomLeft => "BL",
		GridAlignment.BottomCenter => "BC",
		GridAlignment.BottomRight => "BR",
		_ => "MC"
	};

	private string GetDirectionShortText() => _connectorOptions.Direction switch
	{
		ConnectorDirection.All => "All",
		ConnectorDirection.Orthogonal => "Ortho",
		ConnectorDirection.Diagonal => "Diag",
		ConnectorDirection.DiagonalLeftRight => "D-LR",
		ConnectorDirection.DiagonalFrontBack => "D-FB",
		_ => "All"
	};

	private string GetVerticalAlignShortText() => _connectorOptions.VerticalAlign switch
	{
		ConnectorVerticalAlign.Bottom => "Bot",
		ConnectorVerticalAlign.Center => "Mid",
		ConnectorVerticalAlign.Top => "Top",
		_ => "Mid"
	};

	private string GetAnimSpeedText() => _connectorOptions.AnimationSpeed switch
	{
		0 => "Off",
		15 => "Slow",
		35 => "Medium",
		60 => "Fast",
		100 => "V.Fast",
		_ => _connectorOptions.AnimationSpeed.ToString()
	};

	protected override void OnInitialized()
	{
		// Parse settings from URL query parameters
		ParseQueryParameters();
		_isInitialized = true;

		// Generate initial random connectors
		OnRandomizeConnectors();

		// Update URL to show current state (ensures deeplink is present by default)
		UpdateUrl();
	}

	private void ParseQueryParameters()
	{
		var uri = new Uri(NavigationManager.Uri);
		var query = QueryHelpers.ParseQuery(uri.Query);

		// Grid options
		TryParseInt(query, "cols", v => _options.Columns = v);
		TryParseInt(query, "rows", v => _options.Rows = v);
		TryParseInt(query, "depth", v => _options.Depth = v);
		TryParseInt(query, "gap", v => _options.Gap = v);
		TryParseInt(query, "pop", v => _options.Population = v);
		TryParseInt(query, "logoSize", v => _options.LogoSize = v);
		TryParseInt(query, "logoRot", v => _options.LogoRotation = v);
		TryParseHexColor(query, "tile", v => _options.TileColor = v);
		TryParseHexColor(query, "bg", v => _options.BackgroundColor = v);
		TryParseHexColor(query, "lineColor", v => _options.LineColor = v);
		TryParseInt(query, "lineOp", v => _options.LineOpacity = v);
		TryParseInt(query, "glow", v => _options.Glow = v);
		TryParseInt(query, "glowFO", v => _options.GlowFalloff = v);
		TryParseInt(query, "persp", v => _options.Perspective = v);
		TryParseInt(query, "refl", v => _options.Reflection = v);
		TryParseInt(query, "reflD", v => _options.ReflectionDepth = v);
		TryParseInt(query, "scale", v => _options.Scale = v);
		TryParseInt(query, "pad", v => _options.Padding = v);
		TryParseEnum<GridAlignment>(query, "align", v => _options.Alignment = v);
		TryParseNullableInt(query, "maxW", v => _options.MaxGridWidthPercent = v);
		TryParseNullableInt(query, "maxH", v => _options.MaxGridHeightPercent = v);
		TryParseBool(query, "content", v => _showChildContent = v);

		// Connector options
		TryParseEnum<ConnectorFillPattern>(query, "cPat", v => _connectorOptions.FillPattern = v);
		TryParseEnum<ConnectorDirection>(query, "cDir", v => _connectorOptions.Direction = v);
		TryParseNullableInt(query, "cN", v => _connectorOptions.PerEdge = v);
		TryParseInt(query, "cPop", v => _connectorOptions.Population = v);
		TryParseInt(query, "cH", v => _connectorOptions.Height = v);
		TryParseEnum<ConnectorVerticalAlign>(query, "cV", v => _connectorOptions.VerticalAlign = v);
		TryParseInt(query, "cOp", v => _connectorOptions.Opacity = v);
		TryParseInt(query, "cAnim", v => _connectorOptions.AnimationSpeed = v);
	}

	private static void TryParseInt(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<int> setter)
	{
		if (query.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
		{
			setter(parsed);
		}
	}

	private static void TryParseBool(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<bool> setter)
	{
		if (query.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed))
		{
			setter(parsed);
		}
	}

	private static void TryParseEnum<TEnum>(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<TEnum> setter) where TEnum : struct, Enum
	{
		if (query.TryGetValue(key, out var value) && Enum.TryParse<TEnum>(value, out var parsed))
		{
			setter(parsed);
		}
	}

	private static void TryParseHexColor(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<string> setter)
	{
		if (query.TryGetValue(key, out var value))
		{
			setter("#" + value.ToString());
		}
	}

	private static void TryParseNullableInt(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query, string key, Action<int?> setter)
	{
		if (query.TryGetValue(key, out var value))
		{
			setter(string.IsNullOrEmpty(value) ? null : int.TryParse(value, out var parsed) ? parsed : null);
		}
	}

	private void UpdateUrl()
	{
		if (!_isInitialized)
		{
			return;
		}

		// Always include all parameters so users have a complete starting point
		var queryParams = new Dictionary<string, string?>
		{
			// Grid options
			["cols"] = _options.Columns.ToString(),
			["rows"] = _options.Rows.ToString(),
			["depth"] = _options.Depth.ToString(),
			["gap"] = _options.Gap.ToString(),
			["pop"] = _options.Population.ToString(),
			["logoSize"] = _options.LogoSize.ToString(),
			["logoRot"] = _options.LogoRotation.ToString(),
			["tile"] = _options.TileColor.TrimStart('#'),
			["bg"] = _options.BackgroundColor.TrimStart('#'),
			["lineColor"] = _options.LineColor.TrimStart('#'),
			["lineOp"] = _options.LineOpacity.ToString(),
			["glow"] = _options.Glow.ToString(),
			["glowFO"] = _options.GlowFalloff.ToString(),
			["persp"] = _options.Perspective.ToString(),
			["refl"] = _options.Reflection.ToString(),
			["reflD"] = _options.ReflectionDepth.ToString(),
			["scale"] = _options.Scale.ToString(),
			["pad"] = _options.Padding.ToString(),
			["align"] = _options.Alignment.ToString(),
			["maxW"] = _options.MaxGridWidthPercent?.ToString() ?? "",
			["maxH"] = _options.MaxGridHeightPercent?.ToString() ?? "",
			["content"] = _showChildContent ? "true" : "false",

			// Connector options
			["cPat"] = _connectorOptions.FillPattern.ToString(),
			["cDir"] = _connectorOptions.Direction.ToString(),
			["cN"] = _connectorOptions.PerEdge?.ToString() ?? "",
			["cPop"] = _connectorOptions.Population.ToString(),
			["cH"] = _connectorOptions.Height.ToString(),
			["cV"] = _connectorOptions.VerticalAlign.ToString(),
			["cOp"] = _connectorOptions.Opacity.ToString(),
			["cAnim"] = _connectorOptions.AnimationSpeed.ToString()
		};

		var newUrl = QueryHelpers.AddQueryString(NavigationManager.Uri.Split('?')[0], queryParams!);
		NavigationManager.NavigateTo(newUrl, replace: true);
	}

	private void OnOptionsChanged()
	{
		UpdateUrl();
		StateHasChanged();
	}

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

	private void OnShuffle()
	{
		_tilesComponent?.Shuffle();
		OnRandomizeConnectors(); // Re-randomize connectors when tiles change
	}

	private void OnRotateLogo(int degrees)
	{
		_options.LogoRotation = (_options.LogoRotation + degrees) % 360;
		UpdateUrl();
		StateHasChanged();
	}

	// Helper property for binding nullable PerEdge
	private string PerEdgeString
	{
		get => _connectorOptions.PerEdge?.ToString() ?? "";
		set => _connectorOptions.PerEdge = string.IsNullOrEmpty(value) ? null : int.Parse(value);
	}

	private void OnRandomizeConnectors()
	{
		_connectors = _tilesComponent?.GenerateRandomConnectors() ?? GenerateDefaultConnectors();
		UpdateUrl();
		StateHasChanged();
	}

	private void OnClearConnectors()
	{
		_connectors = [];
		StateHasChanged();
	}

	private List<TileConnector> GenerateDefaultConnectors()
	{
		// Create a simple set of connectors for initial display
		var connectors = new List<TileConnector>();
		var random = new Random();
		var colors = new[] { "#00FFFF", "#FF00FF", "#00FF00", "#FF6600", "#FFFF00" };

		for (var row = 0; row < _options.Rows; row++)
		{
			for (var col = 0; col < _options.Columns; col++)
			{
				// Connect to right neighbor
				if (col < _options.Columns - 1 && random.Next(100) < _connectorOptions.Population)
				{
					connectors.Add(new TileConnector
					{
						StartTile = new TileCoordinate { Column = col, Row = row },
						EndTile = new TileCoordinate { Column = col + 1, Row = row },
						Direction = "right",
						Color = colors[connectors.Count % colors.Length],
						Opacity = _connectorOptions.Opacity,
						FillPattern = ConnectorFillPattern.Solid
					});
				}

				// Connect to bottom neighbor
				if (row < _options.Rows - 1 && random.Next(100) < _connectorOptions.Population)
				{
					connectors.Add(new TileConnector
					{
						StartTile = new TileCoordinate { Column = col, Row = row },
						EndTile = new TileCoordinate { Column = col, Row = row + 1 },
						Direction = "down",
						Color = colors[connectors.Count % colors.Length],
						Opacity = _connectorOptions.Opacity,
						FillPattern = ConnectorFillPattern.Solid
					});
				}
			}
		}

		return connectors;
	}

	private void OnTileClick(TileClickEventArgs e)
	{
		_lastEvent = $"Tile: {e.TileName} ({e.Column}, {e.Row})";
		_lastEventIcon = "fa-solid fa-cube";
		StateHasChanged();
	}

	private void OnConnectorClick(ConnectorClickEventArgs e)
	{
		_lastEvent = $"Connector: {e.ConnectorName}";
		_lastEventIcon = "fa-solid fa-link";
		StateHasChanged();
	}

	// Dropdown selection handlers for PDToolbarDropdown
	private async Task OnColumnsSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Columns = v; await OnGridSizeChangedAsync().ConfigureAwait(true); }
	}

	private async Task OnRowsSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Rows = v; await OnGridSizeChangedAsync().ConfigureAwait(true); }
	}

	private async Task OnGridSizeChangedAsync()
	{
		OnOptionsChanged();
		// Wait for the component to re-render and update its tile visibility
		await Task.Yield();
		StateHasChanged();
		await Task.Yield();
		OnRandomizeConnectors(); // Re-randomize connectors when grid size changes
	}

	private void OnDepthSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Depth = v; OnOptionsChanged(); }
	}

	private void OnGapSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Gap = v; OnOptionsChanged(); }
	}

	private async Task OnPopulationSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Population = v; await OnGridSizeChangedAsync().ConfigureAwait(true); }
	}

	private void OnLogoSizeSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.LogoSize = v; OnOptionsChanged(); }
	}

	private void OnLineOpacitySelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.LineOpacity = v; OnOptionsChanged(); }
	}

	private void OnGlowSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Glow = v; OnOptionsChanged(); }
	}

	private void OnGlowFalloffSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.GlowFalloff = v; OnOptionsChanged(); }
	}

	private void OnPerspectiveSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Perspective = v; OnOptionsChanged(); }
	}

	private void OnReflectionSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Reflection = v; OnOptionsChanged(); }
	}

	private void OnReflectionDepthSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.ReflectionDepth = v; OnOptionsChanged(); }
	}

	private void OnScaleSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Scale = v; OnOptionsChanged(); }
	}

	private void OnPaddingSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _options.Padding = v; OnOptionsChanged(); }
	}

	private void OnAlignmentSelected(string key)
	{
		if (Enum.TryParse<GridAlignment>(key, out var v)) { _options.Alignment = v; OnOptionsChanged(); }
	}

	private void OnPatternSelected(string key)
	{
		if (Enum.TryParse<ConnectorFillPattern>(key, out var v)) { _connectorOptions.FillPattern = v; OnRandomizeConnectors(); }
	}

	private void OnDirectionSelected(string key)
	{
		if (Enum.TryParse<ConnectorDirection>(key, out var v)) { _connectorOptions.Direction = v; OnRandomizeConnectors(); }
	}

	private void OnPerEdgeSelected(string key)
	{
		_connectorOptions.PerEdge = string.IsNullOrEmpty(key) ? null : int.TryParse(key, out var v) ? v : null;
		OnRandomizeConnectors();
	}

	private void OnConnectorPopSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.Population = v; OnRandomizeConnectors(); }
	}

	private void OnConnectorHeightSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.Height = v; OnOptionsChanged(); }
	}

	private void OnVerticalAlignSelected(string key)
	{
		if (Enum.TryParse<ConnectorVerticalAlign>(key, out var v)) { _connectorOptions.VerticalAlign = v; OnOptionsChanged(); }
	}

	private void OnConnectorOpacitySelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.Opacity = v; OnOptionsChanged(); }
	}

	private void OnAnimSpeedSelected(string key)
	{
		if (int.TryParse(key, out var v)) { _connectorOptions.AnimationSpeed = v; OnOptionsChanged(); }
	}

	private void OnMaxWidthSelected(string key)
	{
		_options.MaxGridWidthPercent = string.IsNullOrEmpty(key) ? null : int.TryParse(key, out var v) ? v : null;
		OnOptionsChanged();
	}

	private void OnMaxHeightSelected(string key)
	{
		_options.MaxGridHeightPercent = string.IsNullOrEmpty(key) ? null : int.TryParse(key, out var v) ? v : null;
		OnOptionsChanged();
	}

	// Helper to create menu items from values
	private static List<MenuItem> CreateMenuItems(int[] values) =>
		[.. values.Select(v => new MenuItem { Key = v.ToString(), Text = v.ToString() })];

	private static List<MenuItem> CreateMenuItems<T>(params (string Key, string Text)[] items) =>
		[.. items.Select(i => new MenuItem { Key = i.Key, Text = i.Text })];
}
