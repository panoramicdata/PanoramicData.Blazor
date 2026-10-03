using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
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

	private void OnOptionsChanged()
	{
		UpdateUrl();
		StateHasChanged();
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
