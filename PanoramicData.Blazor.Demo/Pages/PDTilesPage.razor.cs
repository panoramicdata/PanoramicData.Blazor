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
		LineColor = "#4A90D0",
		LineOpacity = 15,
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
		if (query.TryGetValue("cols", out var cols) && int.TryParse(cols, out var colsVal))
			_options.Columns = colsVal;
		if (query.TryGetValue("rows", out var rows) && int.TryParse(rows, out var rowsVal))
			_options.Rows = rowsVal;
		if (query.TryGetValue("depth", out var depth) && int.TryParse(depth, out var depthVal))
			_options.Depth = depthVal;
		if (query.TryGetValue("gap", out var gap) && int.TryParse(gap, out var gapVal))
			_options.Gap = gapVal;
		if (query.TryGetValue("pop", out var pop) && int.TryParse(pop, out var popVal))
			_options.Population = popVal;
		if (query.TryGetValue("logoSize", out var logoSize) && int.TryParse(logoSize, out var logoSizeVal))
			_options.LogoSize = logoSizeVal;
		if (query.TryGetValue("logoRot", out var logoRot) && int.TryParse(logoRot, out var logoRotVal))
			_options.LogoRotation = logoRotVal;
		if (query.TryGetValue("tile", out var tile))
			_options.TileColor = "#" + tile.ToString();
		if (query.TryGetValue("bg", out var bg))
			_options.BackgroundColor = "#" + bg.ToString();
		if (query.TryGetValue("lineColor", out var lineColor))
			_options.LineColor = "#" + lineColor.ToString();
		if (query.TryGetValue("lineOp", out var lineOp) && int.TryParse(lineOp, out var lineOpVal))
			_options.LineOpacity = lineOpVal;
		if (query.TryGetValue("glow", out var glow) && int.TryParse(glow, out var glowVal))
			_options.Glow = glowVal;
		if (query.TryGetValue("persp", out var persp) && int.TryParse(persp, out var perspVal))
			_options.Perspective = perspVal;
		if (query.TryGetValue("refl", out var refl) && int.TryParse(refl, out var reflVal))
			_options.Reflection = reflVal;
		if (query.TryGetValue("reflD", out var reflD) && int.TryParse(reflD, out var reflDVal))
			_options.ReflectionDepth = reflDVal;
		if (query.TryGetValue("scale", out var scale) && int.TryParse(scale, out var scaleVal))
			_options.Scale = scaleVal;
		if (query.TryGetValue("pad", out var pad) && int.TryParse(pad, out var padVal))
			_options.Padding = padVal;
		if (query.TryGetValue("align", out var align) && Enum.TryParse<GridAlignment>(align, out var alignVal))
			_options.Alignment = alignVal;
		if (query.TryGetValue("content", out var content) && bool.TryParse(content, out var contentVal))
			_showChildContent = contentVal;

		// Connector options
		if (query.TryGetValue("cPat", out var cPat) && Enum.TryParse<ConnectorFillPattern>(cPat, out var cPatVal))
			_connectorOptions.FillPattern = cPatVal;
		if (query.TryGetValue("cDir", out var cDir) && Enum.TryParse<ConnectorDirection>(cDir, out var cDirVal))
			_connectorOptions.Direction = cDirVal;
		if (query.TryGetValue("cN", out var cN))
			_connectorOptions.PerEdge = string.IsNullOrEmpty(cN) ? null : int.TryParse(cN, out var cNVal) ? cNVal : null;
		if (query.TryGetValue("cPop", out var cPop) && int.TryParse(cPop, out var cPopVal))
			_connectorOptions.Population = cPopVal;
		if (query.TryGetValue("cH", out var cH) && int.TryParse(cH, out var cHVal))
			_connectorOptions.Height = cHVal;
		if (query.TryGetValue("cV", out var cV) && Enum.TryParse<ConnectorVerticalAlign>(cV, out var cVVal))
			_connectorOptions.VerticalAlign = cVVal;
		if (query.TryGetValue("cOp", out var cOp) && int.TryParse(cOp, out var cOpVal))
			_connectorOptions.Opacity = cOpVal;
		if (query.TryGetValue("cAnim", out var cAnim) && int.TryParse(cAnim, out var cAnimVal))
			_connectorOptions.AnimationSpeed = cAnimVal;
	}

	private void UpdateUrl()
	{
		if (!_isInitialized)
			return;

		var queryParams = new Dictionary<string, string?>();

		// Grid options - only include non-default values
		if (_options.Columns != 3) queryParams["cols"] = _options.Columns.ToString();
		if (_options.Rows != 3) queryParams["rows"] = _options.Rows.ToString();
		if (_options.Depth != 15) queryParams["depth"] = _options.Depth.ToString();
		if (_options.Gap != 100) queryParams["gap"] = _options.Gap.ToString();
		if (_options.Population != 100) queryParams["pop"] = _options.Population.ToString();
		if (_options.LogoSize != 85) queryParams["logoSize"] = _options.LogoSize.ToString();
		if (_options.LogoRotation != 0) queryParams["logoRot"] = _options.LogoRotation.ToString();
		if (_options.TileColor != "#373737") queryParams["tile"] = _options.TileColor.TrimStart('#');
		if (_options.BackgroundColor != "#000624") queryParams["bg"] = _options.BackgroundColor.TrimStart('#');
		if (_options.LineColor != "#4A90D0") queryParams["lineColor"] = _options.LineColor.TrimStart('#');
		if (_options.LineOpacity != 15) queryParams["lineOp"] = _options.LineOpacity.ToString();
		if (_options.Glow != 50) queryParams["glow"] = _options.Glow.ToString();
		if (_options.Perspective != 0) queryParams["persp"] = _options.Perspective.ToString();
		if (_options.Reflection != 50) queryParams["refl"] = _options.Reflection.ToString();
		if (_options.ReflectionDepth != 150) queryParams["reflD"] = _options.ReflectionDepth.ToString();
		if (_options.Scale != 50) queryParams["scale"] = _options.Scale.ToString();
		if (_options.Padding != 5) queryParams["pad"] = _options.Padding.ToString();
		if (_options.Alignment != GridAlignment.MiddleLeft) queryParams["align"] = _options.Alignment.ToString();
		if (!_showChildContent) queryParams["content"] = "false";

		// Connector options - only include non-default values
		if (_connectorOptions.FillPattern != ConnectorFillPattern.Random) queryParams["cPat"] = _connectorOptions.FillPattern.ToString();
		if (_connectorOptions.Direction != ConnectorDirection.All) queryParams["cDir"] = _connectorOptions.Direction.ToString();
		if (_connectorOptions.PerEdge != null) queryParams["cN"] = _connectorOptions.PerEdge.ToString();
		if (_connectorOptions.Population != 50) queryParams["cPop"] = _connectorOptions.Population.ToString();
		if (_connectorOptions.Height != 80) queryParams["cH"] = _connectorOptions.Height.ToString();
		if (_connectorOptions.VerticalAlign != ConnectorVerticalAlign.Center) queryParams["cV"] = _connectorOptions.VerticalAlign.ToString();
		if (_connectorOptions.Opacity != 80) queryParams["cOp"] = _connectorOptions.Opacity.ToString();
		if (_connectorOptions.AnimationSpeed != 35) queryParams["cAnim"] = _connectorOptions.AnimationSpeed.ToString();

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
