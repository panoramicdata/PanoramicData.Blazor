using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
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
		LineColor = "#888888",
		LineOpacity = 10,
		Glow = 50,
		Perspective = 0,
		Reflection = 50,
		ReflectionDepth = 150,
		Scale = 50,
		Padding = 5,
		Alignment = GridAlignment.MiddleLeft
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
		TryParseInt(query, "persp", v => _options.Perspective = v);
		TryParseInt(query, "refl", v => _options.Reflection = v);
		TryParseInt(query, "reflD", v => _options.ReflectionDepth = v);
		TryParseInt(query, "scale", v => _options.Scale = v);
		TryParseInt(query, "pad", v => _options.Padding = v);
		TryParseEnum<GridAlignment>(query, "align", v => _options.Alignment = v);
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
			["persp"] = _options.Perspective.ToString(),
			["refl"] = _options.Reflection.ToString(),
			["reflD"] = _options.ReflectionDepth.ToString(),
			["scale"] = _options.Scale.ToString(),
			["pad"] = _options.Padding.ToString(),
			["align"] = _options.Alignment.ToString(),
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

	private void OnShuffle()
	{
		_tilesComponent?.Shuffle();
	}

	private void OnRotateLogo(int degrees)
	{
		_options.LogoRotation = (_options.LogoRotation + degrees) % 360;
		UpdateUrl();
		StateHasChanged();
	}

	// Helper property for binding nullable PerEdge
	private string _perEdgeString
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
}
