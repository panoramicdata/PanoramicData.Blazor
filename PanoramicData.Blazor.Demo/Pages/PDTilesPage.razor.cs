using System.Security.Cryptography;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Models.ColorPicker;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTilesPage
{
	[Inject]
	private NavigationManager NavigationManager { get; set; } = null!;

	protected PDTiles? TilesComponent { get; set; }
	private bool _isInitialized;
	protected bool ShowChildContent { get; set; } = true;

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
		Alignment = GridAlignment.MiddleRight,
		ContentWrapping = true
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
		TilesComponent?.Shuffle();
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
		_connectors = TilesComponent?.GenerateRandomConnectors() ?? GenerateDefaultConnectors();
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
		var colors = new[] { "#00FFFF", "#FF00FF", "#00FF00", "#FF6600", "#FFFF00" };

		for (var row = 0; row < _options.Rows; row++)
		{
			for (var col = 0; col < _options.Columns; col++)
			{
				// Connect to right neighbor
				if (col < _options.Columns - 1 && RandomNumberGenerator.GetInt32(100) < _connectorOptions.Population)
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
				if (row < _options.Rows - 1 && RandomNumberGenerator.GetInt32(100) < _connectorOptions.Population)
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

}
