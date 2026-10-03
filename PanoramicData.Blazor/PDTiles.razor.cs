using Microsoft.JSInterop;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor;

/// <summary>
/// PDTiles - An isometric tile grid component rendered as pure Blazor SVG.
/// </summary>
public partial class PDTiles : ComponentBase, IAsyncDisposable
{
	private static int _seq;
	private ElementReference _svgElement;
	private TileColors _colors = new();
	private readonly Dictionary<string, TileGradientInfo> _tileColorGradients = [];
	private readonly Dictionary<string, TileDefinition> _tileOverrides = [];
	private List<string?> _tileLogos = [];
	private List<bool> _tileVisible = [];

	// The generated (random) logo and visibility of each tile, before per-tile overrides are applied
	private List<string?> _generatedLogos = [];
	private List<bool> _generatedVisible = [];

	// Track last known grid configuration to avoid re-randomizing on every render
	private int _lastColumns;
	private int _lastRows;
	private int _lastPopulation;
	private int _lastLogoCount;

	// Animation state
	private double _animationOffset;
	private System.Timers.Timer? _animationTimer;
	private DateTime _lastAnimationTime;
	private bool _isDisposed;

	/// <summary>
	/// Gets the injected JavaScript runtime.
	/// </summary>
	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	// Tile geometry constants (based on 400x400 viewBox tile)
	private const int _tileWidth = 224;
	private const int _tileHeight = 118;
	private const int _tileCenterX = 200;
	private const int _tileCenterY = 155;

	private static readonly TilePoint _tileBack = new(200, 96);
	private static readonly TilePoint _tileLeft = new(88, 158);
	private static readonly TilePoint _tileFront = new(200, 214);
	private static readonly TilePoint _tileRight = new(312, 158);

	private const string _topFacePath = "M 88,150 C 82,153 82,156 88,158 L 192,214 C 198,217 202,217 208,214 L 312,158 C 318,156 318,153 312,150 L 208,96 C 202,93 198,93 192,96 L 88,150 Z";

	/// <summary>
	/// Connector color palette.
	/// </summary>
	private static readonly string[] _connectorColors = ["#00FFFF", "#FF00FF", "#00FF00", "#FF6600", "#FFFF00", "#FF0000", "#0066FF", "#FF66FF"];

	/// <summary>
	/// Gets or sets the unique identifier for the component.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-tiles-{++_seq}";

	/// <summary>
	/// Gets or sets the CSS class for the component.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets additional inline styles.
	/// </summary>
	[Parameter]
	public string Style { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the width of the component.
	/// </summary>
	[Parameter]
	public string Width { get; set; } = "100%";

	/// <summary>
	/// Gets or sets the height of the component.
	/// </summary>
	[Parameter]
	public string Height { get; set; } = "400px";

	/// <summary>
	/// Gets or sets the grid options.
	/// </summary>
	[Parameter]
	public TileGridOptions Options { get; set; } = new();

	/// <summary>
	/// Gets or sets the connector options.
	/// </summary>
	[Parameter]
	public TileConnectorOptions ConnectorOptions { get; set; } = new();

	/// <summary>
	/// Gets or sets custom tile definitions with per-tile overrides.
	/// </summary>
	[Parameter]
	public List<TileDefinition>? Tiles { get; set; }

	/// <summary>
	/// Gets or sets custom connector definitions.
	/// </summary>
	[Parameter]
	public List<TileConnector>? Connectors { get; set; }

	/// <summary>
	/// Gets or sets the list of logo paths to use.
	/// </summary>
	[Parameter]
	public List<string> Logos { get; set; } =
	[
		"tiles/Admin Logo.svg",
		"tiles/AlertMagic Logo.svg",
		"tiles/CaseMagic Logo.svg",
		"tiles/CodeMagic Logo.svg",
		"tiles/ConnectMagicLogo.svg",
		"tiles/DataMagic Logo.svg",
		"tiles/Magic Suite Logo.svg",
		"tiles/Merlin Logo.svg",
		"tiles/MonitorMagic Logo.svg",
		"tiles/ProMagic Logo.svg",
		"tiles/ReportMagic Logo.svg",
		"tiles/SchemaMagic Logo.svg"
	];

	/// <summary>
	/// Gets or sets the child content to render on top of the tiles.
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Event callback invoked when a tile is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<TileClickEventArgs> TileClick { get; set; }

	/// <summary>
	/// Event callback invoked when a connector is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<ConnectorClickEventArgs> ConnectorClick { get; set; }

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		_colors = GenerateTileColors(Options.TileColor);
		InitializeTiles();
		StartAnimationIfNeeded();
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_colors = GenerateTileColors(Options.TileColor);
		InitializeTiles();
		StartAnimationIfNeeded();
	}

	private void StartAnimationIfNeeded()
	{
		var shouldAnimate = Connectors?.Count > 0 && ConnectorOptions.Animation && ConnectorOptions.AnimationSpeed > 0;

		if (!shouldAnimate)
		{
			StopAnimation();
		}
		else if (_animationTimer == null)
		{
			StartAnimation();
		}
	}

	private void StartAnimation()
	{
		_lastAnimationTime = DateTime.UtcNow;
		_animationTimer = new System.Timers.Timer(1000.0 / 60); // 60 FPS
		_animationTimer.Elapsed += OnAnimationTick;
		_animationTimer.AutoReset = true;
		_animationTimer.Start();
	}

	/// <summary>
	/// Gets whether the connector animation timer is running.
	/// </summary>
	internal bool IsAnimating => _animationTimer != null;

	/// <summary>
	/// Gets the animation offset for connector patterns.
	/// </summary>
	internal double AnimationOffset => _animationOffset;

	private void StopAnimation()
	{
		if (_animationTimer != null)
		{
			_animationTimer.Stop();
			_animationTimer.Elapsed -= OnAnimationTick;
			_animationTimer.Dispose();
			_animationTimer = null;
		}
	}

	private void OnAnimationTick(object? sender, System.Timers.ElapsedEventArgs e) => AdvanceAnimation();

	/// <summary>
	/// Advances the connector animation by the time elapsed since the previous frame and re-renders.
	/// A timer tick already in flight when the component is disposed is ignored.
	/// </summary>
	internal void AdvanceAnimation()
	{
		if (_isDisposed)
		{
			return;
		}

		var now = DateTime.UtcNow;
		var deltaTime = (now - _lastAnimationTime).TotalSeconds;
		_lastAnimationTime = now;

		_animationOffset = (_animationOffset + deltaTime * (ConnectorOptions.AnimationSpeed / 100.0)) % 1.0;

		_ = InvokeAsync(StateHasChanged);
	}

	private async Task OnTileClicked(TileRenderInfo tile)
	{
		var key = $"{tile.Column},{tile.Row}";
		_tileOverrides.TryGetValue(key, out var tileDef);

		await TileClick.InvokeAsync(new TileClickEventArgs
		{
			TileId = tile.Id,
			TileName = GetTileName(tile.Logo),
			Column = tile.Column,
			Row = tile.Row,
			Tile = tileDef
		}).ConfigureAwait(true);
	}

	private async Task OnConnectorClicked(ConnectorRenderInfo conn) => await ConnectorClick.InvokeAsync(new ConnectorClickEventArgs
	{
		ConnectorName = conn.Name,
		StartTile = conn.Connector.StartTile,
		EndTile = conn.Connector.EndTile,
		Connector = conn.Connector
	}).ConfigureAwait(true);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_isDisposed = true;
		StopAnimation();
		await ValueTask.CompletedTask.ConfigureAwait(false);
		GC.SuppressFinalize(this);
	}
}
