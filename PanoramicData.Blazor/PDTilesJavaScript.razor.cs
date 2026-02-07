using Microsoft.JSInterop;
using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor;

/// <summary>
/// PDTilesJavaScript - A JavaScript-based isometric tile grid component wrapper.
/// This wraps the original tile-grid.js implementation for comparison.
/// </summary>
public partial class PDTilesJavaScript : ComponentBase, IAsyncDisposable
{
	private static int _seq;
	private ElementReference _containerElement;
	private IJSObjectReference? _module;
	private DotNetObjectReference<PDTilesJavaScript>? _objRef;

	[Inject]
	public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the unique identifier for the component.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-tiles-js-{++_seq}";

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
	/// Event callback invoked when a tile is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<TileClickEventArgs> TileClick { get; set; }

	/// <summary>
	/// Event callback invoked when a connector is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<ConnectorClickEventArgs> ConnectorClick { get; set; }

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			_objRef = DotNetObjectReference.Create(this);
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>(
				"import",
				"./_content/PanoramicData.Blazor/PDTilesJavaScript.razor.js"
			).ConfigureAwait(true);

			await InitializeGrid().ConfigureAwait(true);
		}
	}

	private async Task InitializeGrid()
	{
		if (_module == null)
		{
			return;
		}

		var config = new
		{
			cols = Options.Columns,
			rows = Options.Rows,
			depth = Options.Depth,
			logoSize = Options.LogoSize,
			logoRotation = Options.LogoRotation,
			gap = Options.Gap,
			population = Options.Population,
			tileColor = Options.TileColor,
			bgColor = Options.BackgroundColor,
			lineColor = Options.LineColor,
			lineOpacity = Options.LineOpacity,
			glow = Options.Glow,
			reflection = Options.Reflection,
			reflectionDepth = Options.ReflectionDepth,
			perspective = Options.Perspective,
			showFloating = Options.ShowFloating,
			floatingPop = Options.FloatingPopulation,
			floatHeight = Options.FloatHeight,
			floatSize = Options.FloatSize,
			connFillPattern = ConnectorOptions.FillPattern.ToString(),
			connDirection = GetConnectorDirection(),
			connPerEdge = ConnectorOptions.PerEdge?.ToString() ?? "random",
			connPopulation = ConnectorOptions.Population,
			connHeight = ConnectorOptions.Height,
			connVAlign = ConnectorOptions.VerticalAlign.ToString().ToLowerInvariant(),
			connOpacity = ConnectorOptions.Opacity,
			connAnimation = ConnectorOptions.Animation,
			connAnimSpeed = ConnectorOptions.AnimationSpeed,
			logos = Logos
		};

		await _module.InvokeVoidAsync("initialize", Id, config, _objRef).ConfigureAwait(true);
	}

	private string GetConnectorDirection()
	{
		return ConnectorOptions.Direction switch
		{
			ConnectorDirection.All => "all",
			ConnectorDirection.Orthogonal => "ortho",
			ConnectorDirection.Diagonal => "diag",
			ConnectorDirection.DiagonalLeftRight => "diag-lr",
			ConnectorDirection.DiagonalFrontBack => "diag-fb",
			_ => "all"
		};
	}

	/// <summary>
	/// Updates the grid configuration.
	/// </summary>
	public async Task UpdateAsync()
	{
		if (_module == null)
		{
			return;
		}

		var config = new
		{
			cols = Options.Columns,
			rows = Options.Rows,
			depth = Options.Depth,
			logoSize = Options.LogoSize,
			logoRotation = Options.LogoRotation,
			gap = Options.Gap,
			population = Options.Population,
			tileColor = Options.TileColor,
			bgColor = Options.BackgroundColor,
			lineColor = Options.LineColor,
			lineOpacity = Options.LineOpacity,
			glow = Options.Glow,
			reflection = Options.Reflection,
			reflectionDepth = Options.ReflectionDepth,
			perspective = Options.Perspective,
			showFloating = Options.ShowFloating,
			floatingPop = Options.FloatingPopulation,
			floatHeight = Options.FloatHeight,
			floatSize = Options.FloatSize
		};

		await _module.InvokeVoidAsync("update", Id, config).ConfigureAwait(true);
	}

	/// <summary>
	/// Shuffles the tile logos.
	/// </summary>
	public async Task ShuffleAsync()
	{
		if (_module == null)
		{
			return;
		}

		await _module.InvokeVoidAsync("shuffle", Id).ConfigureAwait(true);
	}

	/// <summary>
	/// Generates and applies random connectors.
	/// </summary>
	public async Task RandomizeConnectorsAsync()
	{
		if (_module == null)
		{
			return;
		}

		await _module.InvokeVoidAsync("randomizeConnectors", Id).ConfigureAwait(true);
	}

	/// <summary>
	/// Clears all connectors.
	/// </summary>
	public async Task ClearConnectorsAsync()
	{
		if (_module == null)
		{
			return;
		}

		await _module.InvokeVoidAsync("clearConnectors", Id).ConfigureAwait(true);
	}

	/// <summary>
	/// Called from JavaScript when a tile is clicked.
	/// </summary>
	[JSInvokable("OnTileClick")]
	public async Task OnTileClick(int tileId, string tileName, int col, int row)
	{
		await TileClick.InvokeAsync(new TileClickEventArgs
		{
			TileId = tileId,
			TileName = tileName,
			Column = col,
			Row = row
		}).ConfigureAwait(true);
	}

	/// <summary>
	/// Called from JavaScript when a connector is clicked.
	/// </summary>
	[JSInvokable("OnConnectorClick")]
	public async Task OnConnectorClick(string connectorName, int startCol, int startRow, int endCol, int endRow)
	{
		await ConnectorClick.InvokeAsync(new ConnectorClickEventArgs
		{
			ConnectorName = connectorName,
			StartTile = new TileCoordinate { Column = startCol, Row = startRow },
			EndTile = new TileCoordinate { Column = endCol, Row = endRow }
		}).ConfigureAwait(true);
	}

	public async ValueTask DisposeAsync()
	{
		if (_module != null)
		{
			try
			{
				await _module.InvokeVoidAsync("dispose", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}
			catch
			{
				// Ignore errors during disposal
			}
		}

		_objRef?.Dispose();
		GC.SuppressFinalize(this);
	}
}
