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
	private List<string> _tileLogos = [];
	private List<bool> _tileVisible = [];
	private readonly Random _random = new();

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

	/// <summary>
	/// Gets the SVG viewBox attribute value.
	/// </summary>
	private string ViewBox
	{
		get
		{
			var layout = CalculateLayout();
			return $"0 0 {layout.ViewBoxWidth} {layout.ViewBoxHeight}";
		}
	}

	/// <summary>
	/// Gets the SVG preserveAspectRatio value based on alignment.
	/// </summary>
	private string PreserveAspectRatio
	{
		get
		{
			var xAlign = Options.Alignment switch
			{
				GridAlignment.TopLeft or GridAlignment.MiddleLeft or GridAlignment.BottomLeft => "xMin",
				GridAlignment.TopRight or GridAlignment.MiddleRight or GridAlignment.BottomRight => "xMax",
				_ => "xMid"
			};

			var yAlign = Options.Alignment switch
			{
				GridAlignment.TopLeft or GridAlignment.TopCenter or GridAlignment.TopRight => "YMin",
				GridAlignment.BottomLeft or GridAlignment.BottomCenter or GridAlignment.BottomRight => "YMax",
				_ => "YMid"
			};

		return $"{xAlign}{yAlign} meet";
		}
	}




	/// <summary>
	/// Gets the style for the SVG container div.
	/// </summary>
	private string GetSvgContainerStyle()
	{
		if (ChildContent != null)
		{
			// Position absolutely but allow pointer events on SVG elements (tiles/connectors handle their own events)
			return "position: absolute; top: 0; left: 0; width: 100%; height: 100%;";
		}

		return "width: 100%; height: 100%;";
	}

	/// <summary>
	/// Gets the style for the child content container.
	/// </summary>
	private static string GetChildContentStyle() => "position: absolute; top: 0; left: 0; width: 100%; height: 100%; overflow: auto; z-index: 1; pointer-events: none;";

	protected override void OnInitialized()
	{
		base.OnInitialized();
		_colors = GenerateTileColors(Options.TileColor);
		InitializeTiles();
		StartAnimationIfNeeded();
	}

	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_colors = GenerateTileColors(Options.TileColor);
		InitializeTiles();
		StartAnimationIfNeeded();
	}

	private void StartAnimationIfNeeded()
	{
		var hasConnectors = Connectors?.Count > 0;
		var animationEnabled = ConnectorOptions.AnimationSpeed > 0;

		if (hasConnectors && animationEnabled && _animationTimer == null)
		{
			_lastAnimationTime = DateTime.UtcNow;
			_animationTimer = new System.Timers.Timer(1000.0 / 60); // 60 FPS
			_animationTimer.Elapsed += OnAnimationTick;
			_animationTimer.AutoReset = true;
			_animationTimer.Start();
		}
		else if ((!hasConnectors || !animationEnabled) && _animationTimer != null)
		{
			StopAnimation();
		}
	}

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

	private void OnAnimationTick(object? sender, System.Timers.ElapsedEventArgs e)
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

	private void InitializeTiles()
	{
		var totalTiles = Options.Columns * Options.Rows;
		var logoCount = Logos?.Count ?? 0;

		// Check if the grid configuration has changed
		var configChanged = _lastColumns != Options.Columns ||
							_lastRows != Options.Rows ||
							_lastPopulation != Options.Population ||
							_lastLogoCount != logoCount;

		// If configuration hasn't changed, don't re-randomize
		if (!configChanged && _tileVisible.Count == totalTiles && _tileLogos.Count == totalTiles)
		{
			return;
		}

		// Update tracking fields
		_lastColumns = Options.Columns;
		_lastRows = Options.Rows;
		_lastPopulation = Options.Population;
		_lastLogoCount = logoCount;

		// Initialize tile overrides map
		_tileOverrides.Clear();
		if (Tiles != null)
		{
			foreach (var tile in Tiles)
			{
				_tileOverrides[$"{tile.Column},{tile.Row}"] = tile;
			}
		}

		// Initialize logos - shuffle and assign
		_tileLogos = [];
		var shuffledLogos = Logos.OrderBy(_ => _random.Next()).ToList();
		for (var i = 0; i < totalTiles; i++)
		{
			// Check for per-tile logo override
			var col = i % Options.Columns;
			var row = i / Options.Columns;
			var key = $"{col},{row}";
			if (_tileOverrides.TryGetValue(key, out var tileDef) && !string.IsNullOrEmpty(tileDef.Logo))
			{
				_tileLogos.Add(tileDef.Logo);
			}
			else
			{
				_tileLogos.Add(shuffledLogos[i % shuffledLogos.Count]);
			}
		}

		// Initialize visibility based on population
		_tileVisible = [];
		var visibleCount = (int)Math.Ceiling(totalTiles * (Options.Population / 100.0));
		var indices = Enumerable.Range(0, totalTiles).OrderBy(_ => _random.Next()).ToList();
		for (var i = 0; i < totalTiles; i++)
		{
			var isVisible = indices.IndexOf(i) < visibleCount;
			// Check for per-tile visibility override
			var col = i % Options.Columns;
			var row = i / Options.Columns;
			var key = $"{col},{row}";
			if (_tileOverrides.TryGetValue(key, out var tileDef))
			{
				isVisible = tileDef.Visible;
			}

			_tileVisible.Add(isVisible);
		}
	}

	/// <summary>
	/// Shuffles the tile logos randomly.
	/// </summary>
	public void Shuffle()
	{
		_tileLogos = [.. _tileLogos.OrderBy(_ => _random.Next())];
		StateHasChanged();
	}

	/// <summary>
	/// Generates random connectors based on current options.
	/// </summary>
	public List<TileConnector> GenerateRandomConnectors()
	{
		var result = new List<TileConnector>();
		var added = new HashSet<string>();

		for (var row = 0; row < Options.Rows; row++)
		{
			for (var col = 0; col < Options.Columns; col++)
			{
				// Skip invisible tiles
				var startId = row * Options.Columns + col;
				if (startId >= _tileVisible.Count || !_tileVisible[startId])
				{
					continue;
				}

				// Get connectable tiles based on connection mode
				var connectables = GetConnectableTiles(col, row);
				foreach (var adj in connectables)
				{
					// Skip if adjacent tile is invisible
					var endId = adj.Row * Options.Columns + adj.Column;
					if (endId >= _tileVisible.Count || !_tileVisible[endId])
					{
						continue;
					}

					// For StraightLine mode, still apply direction filter
					if (ConnectorOptions.ConnectionMode == ConnectionMode.StraightLine && !MatchesDirection(adj.Type, ConnectorOptions.Direction))
					{
						continue;
					}

					var pairKey = $"{Math.Min(startId, endId)}-{Math.Max(startId, endId)}";

					if (added.Contains(pairKey))
					{
						continue;
					}

					added.Add(pairKey);

					var isDiag = adj.Type.StartsWith("diag-", StringComparison.Ordinal);
					var numConn = isDiag ? 1 : (ConnectorOptions.PerEdge ?? _random.Next(0, 5));

					if (ConnectorOptions.Population < 100 && _random.Next(100) >= ConnectorOptions.Population)
					{
						continue;
					}

					if (numConn == 0)
					{
						continue;
					}

					for (var ci = 0; ci < numConn; ci++)
					{
						var chosenPattern = ConnectorOptions.FillPattern == ConnectorFillPattern.Random
							? (ConnectorFillPattern)_random.Next(1, 4) // Skip Random (0)
							: ConnectorOptions.FillPattern;

						result.Add(new TileConnector
						{
							StartTile = new TileCoordinate { Column = col, Row = row },
							EndTile = new TileCoordinate { Column = adj.Column, Row = adj.Row },
							Direction = adj.Type,
							Reversed = _random.Next(2) == 0,
							Color = _connectorColors[result.Count % _connectorColors.Length],
							Opacity = ConnectorOptions.Opacity,
							AnimationSpeed = ConnectorOptions.AnimationSpeed,
							FillPattern = chosenPattern,
							EdgeIndex = ci,
							EdgeTotal = numConn,
							Height = ConnectorOptions.Height,
							VerticalAlign = ConnectorOptions.VerticalAlign
						});
					}
				}
			}
		}

		return result;
	}

	private LayoutInfo CalculateLayout()
	{
		var depthPixels = (int)Math.Round(_tileWidth * (Options.Depth / 100.0));
		// Gap is percentage of tile size to add as spacing between tiles
		// 0% = tiles tessellate (touch), 100% = one tile-width gap between tiles
		var gapFactor = 1 + (Options.Gap / 100.0);
		var isoSpacingX = (_tileWidth / 2.0) * gapFactor;
		var isoSpacingY = (_tileHeight / 2.0) * gapFactor;

		var diagonalSteps = (Options.Columns - 1) + (Options.Rows - 1);
		var gridPixelWidth = diagonalSteps * isoSpacingX + _tileWidth;
		var gridPixelHeight = diagonalSteps * isoSpacingY + _tileHeight + depthPixels;

		// Apply scale (100% = grid edges touch viewbox, <100% = smaller, >100% = larger/cropped)
		var scaleFactor = Options.Scale / 100.0;

		// Apply padding as percentage of the grid size
		var paddingFactor = 1 + (Options.Padding / 100.0 * 2);

		// Calculate base viewBox dimensions accounting for scale and padding
		var baseViewBoxWidth = gridPixelWidth / scaleFactor;
		var baseViewBoxHeight = gridPixelHeight / scaleFactor;

		// This is the "constrained area" where the grid should be positioned
		var constrainedWidth = baseViewBoxWidth * paddingFactor;
		var constrainedHeight = baseViewBoxHeight * paddingFactor;

		var viewBoxWidth = constrainedWidth;
		var viewBoxHeight = constrainedHeight;

		// Expand the viewBox to respect MaxGridWidthPercent and MaxGridHeightPercent
		// If MaxGridWidthPercent is 50%, the viewBox should be twice as wide so the grid
		// occupies only 50% of the container width (positioned by alignment)
		if (Options.MaxGridWidthPercent.HasValue && Options.MaxGridWidthPercent.Value > 0 && Options.MaxGridWidthPercent.Value < 100)
		{
			viewBoxWidth = constrainedWidth * (100.0 / Options.MaxGridWidthPercent.Value);
		}

		if (Options.MaxGridHeightPercent.HasValue && Options.MaxGridHeightPercent.Value > 0 && Options.MaxGridHeightPercent.Value < 100)
		{
			viewBoxHeight = constrainedHeight * (100.0 / Options.MaxGridHeightPercent.Value);
		}

		return new LayoutInfo
		{
			DepthPixels = depthPixels,
			IsoSpacingX = isoSpacingX,
			IsoSpacingY = isoSpacingY,
			GridWidth = gridPixelWidth,
			GridHeight = gridPixelHeight,
			ConstrainedWidth = constrainedWidth,
			ConstrainedHeight = constrainedHeight,
			ViewBoxWidth = viewBoxWidth,
			ViewBoxHeight = viewBoxHeight
		};
	}

	/// <summary>
	/// Gets the anchor point for positioning the grid based on alignment.
	/// </summary>
	private (double X, double Y) GetGridAnchorPoint(LayoutInfo layout)
	{
		// Calculate the offset for the constrained area within the expanded viewBox
		// When MaxGridWidthPercent/MaxGridHeightPercent are set, the constrained area
		// needs to be positioned within the larger viewBox based on alignment
		var constrainedOffsetX = Options.Alignment switch
		{
			GridAlignment.TopLeft or GridAlignment.MiddleLeft or GridAlignment.BottomLeft
				=> 0,
			GridAlignment.TopRight or GridAlignment.MiddleRight or GridAlignment.BottomRight
				=> layout.ViewBoxWidth - layout.ConstrainedWidth,
			_ => (layout.ViewBoxWidth - layout.ConstrainedWidth) / 2
		};

		var constrainedOffsetY = Options.Alignment switch
		{
			GridAlignment.TopLeft or GridAlignment.TopCenter or GridAlignment.TopRight
				=> 0,
			GridAlignment.BottomLeft or GridAlignment.BottomCenter or GridAlignment.BottomRight
				=> layout.ViewBoxHeight - layout.ConstrainedHeight,
			_ => (layout.ViewBoxHeight - layout.ConstrainedHeight) / 2
		};

		// Calculate padding within the constrained area
		var paddingX = (layout.ConstrainedWidth - layout.GridWidth) / 2;
		var paddingY = (layout.ConstrainedHeight - layout.GridHeight) / 2;

		// Position within the constrained area, then offset by the constrained area position
		var x = Options.Alignment switch
		{
			GridAlignment.TopLeft or GridAlignment.MiddleLeft or GridAlignment.BottomLeft
				=> constrainedOffsetX + paddingX + layout.GridWidth / 2,
			GridAlignment.TopRight or GridAlignment.MiddleRight or GridAlignment.BottomRight
				=> constrainedOffsetX + layout.ConstrainedWidth - paddingX - layout.GridWidth / 2,
			_ => constrainedOffsetX + layout.ConstrainedWidth / 2
		};

		var y = Options.Alignment switch
		{
			GridAlignment.TopLeft or GridAlignment.TopCenter or GridAlignment.TopRight
				=> constrainedOffsetY + paddingY + layout.GridHeight / 2 - layout.DepthPixels / 2,
			GridAlignment.BottomLeft or GridAlignment.BottomCenter or GridAlignment.BottomRight
				=> constrainedOffsetY + layout.ConstrainedHeight - paddingY - layout.GridHeight / 2 - layout.DepthPixels / 2,
			_ => constrainedOffsetY + layout.ConstrainedHeight / 2 - layout.DepthPixels / 2
		};

		return (x, y);
	}

	private List<TileRenderInfo> GetSortedTiles()
	{
		var tiles = new List<TileRenderInfo>();
		var layout = CalculateLayout();
		var tileId = 0;

		// Calculate grid anchor point based on alignment
		var (anchorX, anchorY) = GetGridAnchorPoint(layout);

		var gridCenterCol = (Options.Columns - 1) / 2.0;
		var gridCenterRow = (Options.Rows - 1) / 2.0;

		for (var row = 0; row < Options.Rows; row++)
		{
			for (var col = 0; col < Options.Columns; col++)
			{
				var relCol = col - gridCenterCol;
				var relRow = row - gridCenterRow;
				var x = anchorX + (relCol - relRow) * layout.IsoSpacingX - _tileCenterX;
				var y = anchorY + (relCol + relRow) * layout.IsoSpacingY - _tileCenterY;

				tiles.Add(new TileRenderInfo
				{
					Id = tileId,
					Column = col,
					Row = row,
					X = x,
					Y = y,
					Depth = row + col,
					Logo = _tileLogos.Count > tileId ? _tileLogos[tileId] : null,
					Visible = _tileVisible.Count > tileId && _tileVisible[tileId]
				});
				tileId++;
			}
		}

		return [.. tiles.OrderBy(t => t.Depth)];
	}

	private Dictionary<int, List<ConnectorRenderInfo>> GetConnectorsByDepth()
	{
		var result = new Dictionary<int, List<ConnectorRenderInfo>>();
		var connectors = Connectors ?? [];

		foreach (var conn in connectors)
		{
			var startDepth = conn.StartTile.Row + conn.StartTile.Column;
			var endDepth = conn.EndTile.Row + conn.EndTile.Column;
			var connDepth = Math.Max(startDepth, endDepth);

			var startLogo = GetTileLogo(conn.StartTile.Column, conn.StartTile.Row);
			var endLogo = GetTileLogo(conn.EndTile.Column, conn.EndTile.Row);
			// Include edge index to make name unique for multiple connectors between same tiles
			var connName = $"{GetTileName(startLogo)}?{GetTileName(endLogo)}#{conn.EdgeIndex}";

			var renderInfo = new ConnectorRenderInfo
			{
				Connector = conn,
				Name = connName,
				Depth = connDepth
			};

			if (!result.TryGetValue(connDepth, out List<ConnectorRenderInfo>? value))
			{
				value = [];
				result[connDepth] = value;
			}

			value.Add(renderInfo);
		}

		return result;
	}

	private static List<int> GetAllDepths(List<TileRenderInfo> tiles, Dictionary<int, List<ConnectorRenderInfo>> connectorsByDepth)
	{
		var allDepths = new HashSet<int>(tiles.Select(t => t.Depth));
		foreach (var depth in connectorsByDepth.Keys)
		{
			allDepths.Add(depth);
		}

		return [.. allDepths.OrderBy(d => d)];
	}

	private List<LineInfo> GetBackgroundLines()
	{
		var lines = new List<LineInfo>();
		var layout = CalculateLayout();
		var (anchorX, anchorY) = GetGridAnchorPoint(layout);
		// Adjust for grid lines being at base of tiles (add depth)
		var lineAnchorY = anchorY + layout.DepthPixels;
		var gridCenterCol = (Options.Columns - 1) / 2.0;
		var gridCenterRow = (Options.Rows - 1) / 2.0;

		// Calculate extension to reach container edges
		// We need lines to extend beyond the viewBox to ensure full coverage
		var viewBoxExtent = Math.Max(layout.ViewBoxWidth, layout.ViewBoxHeight);
		var gridExtent = Math.Max(layout.GridWidth, layout.GridHeight);
		var extRatio = gridExtent > 0 ? viewBoxExtent / gridExtent : 2;
		var ext = Math.Max(5, (int)Math.Ceiling(extRatio * Math.Max(Options.Columns, Options.Rows)));

		// Vertical lines (column direction)
		for (var i = -ext; i <= Options.Columns + ext; i++)
		{
			var relCol = i - gridCenterCol;
			var sR = -ext - gridCenterRow;
			var eR = Options.Rows + ext - gridCenterRow;

			lines.Add(new LineInfo
			{
				X1 = anchorX + (relCol - sR) * layout.IsoSpacingX,
				Y1 = lineAnchorY + (relCol + sR) * layout.IsoSpacingY,
				X2 = anchorX + (relCol - eR) * layout.IsoSpacingX,
				Y2 = lineAnchorY + (relCol + eR) * layout.IsoSpacingY
			});
		}

		// Horizontal lines (row direction)
		for (var i = -ext; i <= Options.Rows + ext; i++)
		{
			var relRow = i - gridCenterRow;
			var sC = -ext - gridCenterCol;
			var eC = Options.Columns + ext - gridCenterCol;

			lines.Add(new LineInfo
			{
				X1 = anchorX + (sC - relRow) * layout.IsoSpacingX,
				Y1 = lineAnchorY + (sC + relRow) * layout.IsoSpacingY,
				X2 = anchorX + (eC - relRow) * layout.IsoSpacingX,
				Y2 = lineAnchorY + (eC + relRow) * layout.IsoSpacingY
			});
		}

		return lines;
	}

	private string GetPerspectiveStyle()
	{
		if (Options.Perspective <= 0)
		{
			return string.Empty;
		}

		var tiltDegrees = Options.Perspective * 0.2;
		return $"transform: perspective(1000px) rotateX({tiltDegrees}deg);";
	}

	private static string GetFrontFacePath(int depthPercent)
	{
		var d = (int)Math.Round(_tileWidth * (depthPercent / 100.0));
		return $"M 88,150 C 82,153 82,156 88,158 L 192,214 C 198,217 202,217 208,214 L 312,158 C 318,156 318,153 312,150 L 316.5,154 L 316.5,{155 + d} Q 316.5,{157 + d} 312,{158 + d} L 208,{214 + d} C 202,{217 + d} 198,{217 + d} 192,{214 + d} L 88,{158 + d} Q 83.5,{157 + d} 83.5,{155 + d} L 83.5,154 Z";
	}

	private static string GetReflectionPath(int depthPercent, int reflectionDepthPercent)
	{
		var d = (int)Math.Round(_tileWidth * (depthPercent / 100.0));
		var reflectionHeight = (int)Math.Round(d * (reflectionDepthPercent / 100.0));
		var bottomY = 155 + d;
		var reflectEndY = bottomY + reflectionHeight;
		return $"M 83.5,{bottomY} L 83.5,{reflectEndY} Q 83.5,{reflectEndY + 2} 88,{reflectEndY + 3} L 192,{214 + d + reflectionHeight + 3} C 198,{217 + d + reflectionHeight + 3} 202,{217 + d + reflectionHeight + 3} 208,{214 + d + reflectionHeight + 3} L 312,{reflectEndY + 3} Q 316.5,{reflectEndY + 2} 316.5,{reflectEndY} L 316.5,{bottomY} L 312,{158 + d} L 208,{214 + d} C 202,{217 + d} 198,{217 + d} 192,{214 + d} L 88,{158 + d} Z";
	}

	private string GetConnectorPoints(ConnectorRenderInfo conn)
	{
		var tiles = GetSortedTiles();
		var layout = CalculateLayout();

		var startTile = tiles.FirstOrDefault(t => t.Column == conn.Connector.StartTile.Column && t.Row == conn.Connector.StartTile.Row);
		var endTile = tiles.FirstOrDefault(t => t.Column == conn.Connector.EndTile.Column && t.Row == conn.Connector.EndTile.Row);

		if (startTile == null || endTile == null)
		{
			return string.Empty;
		}

		var direction = conn.Connector.Direction;
		var edgeIndex = conn.Connector.EdgeIndex;
		var edgeTotal = conn.Connector.EdgeTotal;

		// Get attachment points for connectors - use opposite direction for end tile (like JS version)
		var startPoints = GetTileAttachmentPoints(startTile, direction, true, edgeIndex, edgeTotal, layout);
		var endPoints = GetTileAttachmentPoints(endTile, GetOppositeDirection(direction), false, edgeIndex, edgeTotal, layout);

		// Calculate ribbon height based on connector settings
		var connHeight = (conn.Connector.Height ?? ConnectorOptions.Height) / 100.0;
		var vAlign = conn.Connector.VerticalAlign ?? ConnectorOptions.VerticalAlign;

		double t0Y, t1Y, b0Y, b1Y;

		// Apply vertical alignment
		var startRange = startPoints.Bottom - startPoints.Top;
		var endRange = endPoints.Bottom - endPoints.Top;
		var ribbonHeight = Math.Min(startRange, endRange) * connHeight;

		switch (vAlign)
		{
			case ConnectorVerticalAlign.Top:
				t0Y = startPoints.Top;
				b0Y = startPoints.Top + ribbonHeight;
				t1Y = endPoints.Top;
				b1Y = endPoints.Top + ribbonHeight;
				break;
			case ConnectorVerticalAlign.Center:
				var startMid = (startPoints.Top + startPoints.Bottom) / 2;
				var endMid = (endPoints.Top + endPoints.Bottom) / 2;
				t0Y = startMid - ribbonHeight / 2;
				b0Y = startMid + ribbonHeight / 2;
				t1Y = endMid - ribbonHeight / 2;
				b1Y = endMid + ribbonHeight / 2;
				break;
			default: // Bottom
				t0Y = startPoints.Bottom - ribbonHeight;
				b0Y = startPoints.Bottom;
				t1Y = endPoints.Bottom - ribbonHeight;
				b1Y = endPoints.Bottom;
				break;
		}

		return $"{startPoints.X},{t0Y} {endPoints.X},{t1Y} {endPoints.X},{b1Y} {startPoints.X},{b0Y}";
	}

	private AttachmentPoints GetTileAttachmentPoints(TileRenderInfo tile, string direction, bool isOutgoing, int edgeIndex, int edgeTotal, LayoutInfo layout)
	{
		const double connectorYNudge = 3;
		var x = tile.X;
		var y = tile.Y;
		var tileDepth = GetTileProperty(tile.Column, tile.Row, t => t.Depth, Options.Depth);
		var d = _tileWidth * (tileDepth / 100.0);

		var isDiag = direction.StartsWith("diag-", StringComparison.Ordinal);
		if (isDiag)
		{
			TilePoint corner = direction switch
			{
				"diag-front" => _tileFront,
				"diag-back" => _tileBack,
				"diag-right" => _tileRight,
				"diag-left" => _tileLeft,
				_ => _tileFront
			};

			var isLeftRight = corner == _tileLeft || corner == _tileRight;
			var nudge = isLeftRight ? -connectorYNudge : connectorYNudge;
			var ptX = x + corner.X;
			var ptY = y + corner.Y + nudge;
			return new AttachmentPoints(ptX, ptY, ptY + d);
		}

		var frac = isOutgoing
			? (edgeIndex + 1.0) / (edgeTotal + 1.0)
			: (edgeTotal - edgeIndex) / (edgeTotal + 1.0);

		TilePoint p0, p1;
		if (isOutgoing)
		{
			if (direction is "right" or "up")
			{
				p0 = _tileFront;
				p1 = _tileRight;
			}
			else
			{
				p0 = _tileLeft;
				p1 = _tileFront;
			}
		}
		else
		{
			if (direction is "left" or "down")
			{
				p0 = _tileBack;
				p1 = _tileLeft;
			}
			else
			{
				p0 = _tileRight;
				p1 = _tileBack;
			}
		}

		var ptX2 = x + p0.X + (p1.X - p0.X) * frac;
		var ptY2 = y + p0.Y + (p1.Y - p0.Y) * frac + connectorYNudge;
		return new AttachmentPoints(ptX2, ptY2, ptY2 + d);
	}

	private record AttachmentPoints(double X, double Top, double Bottom);

	/// <summary>
	/// Gets the animation offset for connector patterns.
	/// </summary>
	internal double AnimationOffset => _animationOffset;

	private ((double X, double Y) t0, (double X, double Y) t1, (double X, double Y) b0, (double X, double Y) b1, string points)? GetConnectorData(ConnectorRenderInfo conn)
	{
		var points = GetConnectorPoints(conn);
		if (string.IsNullOrEmpty(points))
		{
			return null;
		}

		var pointsArray = points.Split(' ');
		if (pointsArray.Length < 4)
		{
			return null;
		}

		var coords = pointsArray.Select(p =>
		{
			var parts = p.Split(',');
			return (X: double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
					Y: double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
		}).ToArray();

		// Points are: startTop, endTop, endBottom, startBottom
		return (coords[0], coords[1], coords[3], coords[2], points);
	}

	private T GetTileProperty<T>(int col, int row, Func<TileDefinition, T?> selector, T defaultValue) where T : struct
	{
		var key = $"{col},{row}";
		if (_tileOverrides.TryGetValue(key, out var tileDef))
		{
			var value = selector(tileDef);
			if (value.HasValue)
			{
				return value.Value;
			}
		}

		return defaultValue;
	}

	private string GetTileProperty(int col, int row, Func<TileDefinition, string?> selector, string defaultValue)
	{
		var key = $"{col},{row}";
		if (_tileOverrides.TryGetValue(key, out var tileDef))
		{
			var value = selector(tileDef);
			if (!string.IsNullOrEmpty(value))
			{
				return value;
			}
		}

		return defaultValue;
	}

	private string? GetTileLogo(int col, int row)
	{
		var id = row * Options.Columns + col;
		return _tileLogos.Count > id ? _tileLogos[id] : null;
	}

	private TileGradientInfo EnsureGradients(string color, int tileId)
	{
		if (!_tileColorGradients.TryGetValue(color, out var gradInfo))
		{
			var colors = GenerateTileColors(color);
			gradInfo = new TileGradientInfo
			{
				TopGradId = $"{Id}-topGrad-{color.Replace("#", "")}",
				FrontGradId = $"{Id}-frontGrad-{color.Replace("#", "")}",
				Colors = colors
			};
			_tileColorGradients[color] = gradInfo;
		}

		return gradInfo;
	}

	private static TileColors GenerateTileColors(string baseColor)
	{
		var (r, g, b) = ParseHexColor(baseColor);
		return new TileColors
		{
			Dark = ToHex((int)(r * 0.25), (int)(g * 0.25), (int)(b * 0.25)),
			Mid = ToHex((int)(r * 0.5), (int)(g * 0.5), (int)(b * 0.5)),
			Base = baseColor,
			Light = ToHex(Math.Min(255, (int)(r * 1.4)), Math.Min(255, (int)(g * 1.4)), Math.Min(255, (int)(b * 1.4)))
		};
	}

	private static (int r, int g, int b) ParseHexColor(string hex)
	{
		hex = hex.TrimStart('#');
		return (
			Convert.ToInt32(hex[..2], 16),
			Convert.ToInt32(hex.Substring(2, 2), 16),
			Convert.ToInt32(hex.Substring(4, 2), 16)
		);
	}

	private static string ToHex(int r, int g, int b) => $"#{Math.Clamp(r, 0, 255):X2}{Math.Clamp(g, 0, 255):X2}{Math.Clamp(b, 0, 255):X2}";

	private static string HexToRgba(string hex, double alpha)
	{
		var (r, g, b) = ParseHexColor(hex);
		return $"rgba({r}, {g}, {b}, {alpha})";
	}

	private static string GetTileName(string? logoPath)
	{
		if (string.IsNullOrEmpty(logoPath))
		{
			return "Unknown";
		}

		return logoPath
			.Replace("tiles/", "")
			.Replace(" Logo.svg", "")
			.Replace("Logo.svg", "")
			.Replace(".svg", "")
			.Trim();
	}

	private List<AdjacentTile> GetAdjacentTiles(int col, int row)
	{
		var adj = new List<AdjacentTile>();

		if (col > 0)
		{
			adj.Add(new AdjacentTile(col - 1, row, "left"));
		}

		if (col < Options.Columns - 1)
		{
			adj.Add(new AdjacentTile(col + 1, row, "right"));
		}

		if (row > 0)
		{
			adj.Add(new AdjacentTile(col, row - 1, "up"));
		}

		if (row < Options.Rows - 1)
		{
			adj.Add(new AdjacentTile(col, row + 1, "down"));
		}

		if (col < Options.Columns - 1 && row < Options.Rows - 1)
		{
			adj.Add(new AdjacentTile(col + 1, row + 1, "diag-front"));
		}

		if (col > 0 && row > 0)
		{
			adj.Add(new AdjacentTile(col - 1, row - 1, "diag-back"));
		}

		if (col < Options.Columns - 1 && row > 0)
		{
			adj.Add(new AdjacentTile(col + 1, row - 1, "diag-right"));
		}

		if (col > 0 && row < Options.Rows - 1)
		{
			adj.Add(new AdjacentTile(col - 1, row + 1, "diag-left"));
		}

		return adj;
	}

	private static bool MatchesDirection(string type, ConnectorDirection dirFilter) => dirFilter switch
	{
		ConnectorDirection.All => true,
		ConnectorDirection.Orthogonal => !type.StartsWith("diag-", StringComparison.Ordinal),
		ConnectorDirection.Diagonal => type.StartsWith("diag-", StringComparison.Ordinal),
		ConnectorDirection.DiagonalLeftRight => type is "diag-right" or "diag-left",
		ConnectorDirection.DiagonalFrontBack => type is "diag-front" or "diag-back",
		_ => true
	};

	/// <summary>
	/// Validates if a connection between two tiles is valid for the given connection mode.
	/// </summary>
	private static bool IsValidConnection(TileCoordinate start, TileCoordinate end, ConnectionMode mode)
	{
		var rowDiff = Math.Abs(end.Row - start.Row);
		var colDiff = Math.Abs(end.Column - start.Column);

		return mode switch
		{
		// StraightLine: only adjacent tiles (including diagonals) - row/col diff must be 0 or 1
			ConnectionMode.StraightLine => rowDiff <= 1 && colDiff <= 1 && (rowDiff + colDiff > 0),
			// RowCurves: adjacent rows (diff of 1), any column distance
			ConnectionMode.RowCurves => rowDiff == 1 && colDiff >= 0,
			// ColumnCurves: adjacent columns (diff of 1), any row distance
			ConnectionMode.ColumnCurves => colDiff == 1 && rowDiff >= 0,
			_ => true
		};
	}

	/// <summary>
	/// Gets the tiles that can be connected to in the current connection mode.
	/// </summary>
	private List<AdjacentTile> GetConnectableTiles(int col, int row)
	{
		return ConnectorOptions.ConnectionMode switch
		{
			ConnectionMode.RowCurves => GetRowConnectableTiles(col, row),
			ConnectionMode.ColumnCurves => GetColumnConnectableTiles(col, row),
			_ => GetAdjacentTiles(col, row) // StraightLine uses existing adjacent tile logic
		};
	}


	private List<AdjacentTile> GetRowConnectableTiles(int col, int row)
	{
		var tiles = new List<AdjacentTile>();

		// RowCurves: Connect to tiles in adjacent rows (row +/- 1), any column
		// Use standard "up"/"down" directions so attachment points work the same as straight lines
		foreach (var targetRow in new[] { row - 1, row + 1 })
		{
			if (targetRow < 0 || targetRow >= Options.Rows)
			{
				continue;
			}

			for (var c = 0; c < Options.Columns; c++)
			{
				// Use standard directions - attachment points are the same as straight-line mode
				var direction = targetRow < row ? "up" : "down";
				tiles.Add(new AdjacentTile(c, targetRow, direction));
			}
		}

		return tiles;
	}

	private List<AdjacentTile> GetColumnConnectableTiles(int col, int row)
	{
		var tiles = new List<AdjacentTile>();

		// ColumnCurves: Connect to tiles in adjacent columns (col +/- 1), any row
		// Use standard "left"/"right" directions so attachment points work the same as straight lines
		foreach (var targetCol in new[] { col - 1, col + 1 })
		{
			if (targetCol < 0 || targetCol >= Options.Columns)
			{
				continue;
			}

			for (var r = 0; r < Options.Rows; r++)
			{
				// Use standard directions - attachment points are the same as straight-line mode
				var direction = targetCol < col ? "left" : "right";
				tiles.Add(new AdjacentTile(targetCol, r, direction));
			}
		}

		return tiles;
	}

	/// <summary>
	/// Determines if the current connection mode uses bezier curves.
	/// </summary>
	private bool UsesBezierCurves => ConnectorOptions.ConnectionMode is ConnectionMode.RowCurves or ConnectionMode.ColumnCurves;

	/// <summary>
	/// Gets the bezier curve path data for a curved connector.
	/// </summary>
	private BezierConnectorData? GetBezierConnectorData(ConnectorRenderInfo conn)
	{
		var tiles = GetSortedTiles();
		var layout = CalculateLayout();

		var startTile = tiles.FirstOrDefault(t => t.Column == conn.Connector.StartTile.Column && t.Row == conn.Connector.StartTile.Row);
		var endTile = tiles.FirstOrDefault(t => t.Column == conn.Connector.EndTile.Column && t.Row == conn.Connector.EndTile.Row);


		if (startTile == null || endTile == null)
		{
			return null;
		}

		var direction = conn.Connector.Direction;
		var edgeIndex = conn.Connector.EdgeIndex;
		var edgeTotal = conn.Connector.EdgeTotal;

		// Use the SAME attachment point logic as straight-line connectors
		var startPoints = GetTileAttachmentPoints(startTile, direction, true, edgeIndex, edgeTotal, layout);
		var endPoints = GetTileAttachmentPoints(endTile, GetOppositeDirection(direction), false, edgeIndex, edgeTotal, layout);

		// Calculate ribbon height
		var connHeight = (conn.Connector.Height ?? ConnectorOptions.Height) / 100.0;
		var vAlign = conn.Connector.VerticalAlign ?? ConnectorOptions.VerticalAlign;

		var startRange = startPoints.Bottom - startPoints.Top;
		var endRange = endPoints.Bottom - endPoints.Top;
		var ribbonHeight = Math.Min(startRange, endRange) * connHeight;

		double startTopY, startBottomY, endTopY, endBottomY;

		switch (vAlign)
		{
			case ConnectorVerticalAlign.Top:
				startTopY = startPoints.Top;
				startBottomY = startPoints.Top + ribbonHeight;
				endTopY = endPoints.Top;
				endBottomY = endPoints.Top + ribbonHeight;
				break;
			case ConnectorVerticalAlign.Center:
				var startMid = (startPoints.Top + startPoints.Bottom) / 2;
				var endMid = (endPoints.Top + endPoints.Bottom) / 2;
				startTopY = startMid - ribbonHeight / 2;
				startBottomY = startMid + ribbonHeight / 2;
				endTopY = endMid - ribbonHeight / 2;
				endBottomY = endMid + ribbonHeight / 2;
				break;
			default: // Bottom
				startTopY = startPoints.Bottom - ribbonHeight;
				startBottomY = startPoints.Bottom;
				endTopY = endPoints.Bottom - ribbonHeight;
				endBottomY = endPoints.Bottom;
			break;
		}

		// Calculate control point distance based on tension
		var tension = ConnectorOptions.CurveTension / 100.0;
		var midStartY = (startTopY + startBottomY) / 2;
		var midEndY = (endTopY + endBottomY) / 2;
		var distance = Math.Sqrt(Math.Pow(endPoints.X - startPoints.X, 2) + Math.Pow(midEndY - midStartY, 2));
		
		// For curves, we want control points that bow OUTWARD (perpendicular to line of travel)
		// not along the line of travel
		var bowAmount = distance * tension * 0.5;

		// Calculate the midpoint between start and end
		var midX = (startPoints.X + endPoints.X) / 2;
		var midY = (midStartY + midEndY) / 2;

		// Calculate perpendicular direction (rotate 90 degrees)
		var dx = endPoints.X - startPoints.X;
		var dy = midEndY - midStartY;
		var len = Math.Sqrt(dx * dx + dy * dy);
		if (len < 0.001)
		{
			len = 1; // Avoid division by zero
		}

		// Perpendicular vector (rotated 90 degrees counter-clockwise)
		var perpX = -dy / len;
		var perpY = dx / len;

		// Control points bow outward from the midpoint
		// For row curves (up/down), bow horizontally
		// For column curves (left/right), bow vertically
		var ctrlX = midX + perpX * bowAmount;
		var ctrlY = midY + perpY * bowAmount;

		// Use quadratic-style control point at the midpoint for both curves
		// This creates a symmetric bow
		var startTopCtrl = (X: ctrlX, Y: startTopY + (ctrlY - midStartY));
		var endTopCtrl = (X: ctrlX, Y: endTopY + (ctrlY - midEndY));

		var startBottomCtrl = (X: ctrlX, Y: startBottomY + (ctrlY - midStartY));
		var endBottomCtrl = (X: ctrlX, Y: endBottomY + (ctrlY - midEndY));

		// Build SVG path
		// Top edge: start -> end
		// Right edge: end top -> end bottom (straight line)
		// Bottom edge: end bottom -> start bottom (reversed curve)
		// Left edge: start bottom -> start top (straight line)
		var pathData = $"M {F(startPoints.X)},{F(startTopY)} " +
					   $"C {F(startTopCtrl.X)},{F(startTopCtrl.Y)} {F(endTopCtrl.X)},{F(endTopCtrl.Y)} {F(endPoints.X)},{F(endTopY)} " +
					   $"L {F(endPoints.X)},{F(endBottomY)} " +
					   $"C {F(endBottomCtrl.X)},{F(endBottomCtrl.Y)} {F(startBottomCtrl.X)},{F(startBottomCtrl.Y)} {F(startPoints.X)},{F(startBottomY)} " +
					   $"Z";


		// Also generate stroke paths for top and bottom edges
		var topStrokePath = $"M {F(startPoints.X)},{F(startTopY)} C {F(startTopCtrl.X)},{F(startTopCtrl.Y)} {F(endTopCtrl.X)},{F(endTopCtrl.Y)} {F(endPoints.X)},{F(endTopY)}";
		var bottomStrokePath = $"M {F(startPoints.X)},{F(startBottomY)} C {F(startBottomCtrl.X)},{F(startBottomCtrl.Y)} {F(endBottomCtrl.X)},{F(endBottomCtrl.Y)} {F(endPoints.X)},{F(endBottomY)}";

		return new BezierConnectorData(
			pathData,
			topStrokePath,
			bottomStrokePath,
			(startPoints.X, startTopY),
			(endPoints.X, endTopY),
			(startPoints.X, startBottomY),
			(endPoints.X, endBottomY)
		);
	}

	private sealed record BezierConnectorData(
		string FillPath,
		string TopStrokePath,
		string BottomStrokePath,
		(double X, double Y) StartTop,
		(double X, double Y) EndTop,
		(double X, double Y) StartBottom,
		(double X, double Y) EndBottom
	);

	private static string GetOppositeDirection(string direction) => direction switch
	{
		"left" => "right",
		"right" => "left",
		"up" => "down",
		"down" => "up",
		"diag-front" => "diag-back",
		"diag-back" => "diag-front",
		"diag-right" => "diag-left",
		"diag-left" => "diag-right",
		_ => direction
	};

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

	// Internal helper classes
	private record TilePoint(double X, double Y);

	private class TileColors
	{
		public string Dark { get; set; } = "#000000";
		public string Mid { get; set; } = "#333333";
		public string Base { get; set; } = "#666666";
		public string Light { get; set; } = "#999999";
	}

	private class TileGradientInfo
	{
		public string TopGradId { get; set; } = string.Empty;
		public string FrontGradId { get; set; } = string.Empty;
		public TileColors Colors { get; set; } = new();
	}

	private class LayoutInfo
	{
		public double DepthPixels { get; set; }
		public double IsoSpacingX { get; set; }
		public double IsoSpacingY { get; set; }
		public double GridWidth { get; set; }
		public double GridHeight { get; set; }
		/// <summary>
		/// The width of the area where the grid should be positioned (before MaxGridWidthPercent expansion).
		/// </summary>
		public double ConstrainedWidth { get; set; }
		/// <summary>
		/// The height of the area where the grid should be positioned (before MaxGridHeightPercent expansion).
		/// </summary>
		public double ConstrainedHeight { get; set; }
		public double ViewBoxWidth { get; set; }
		public double ViewBoxHeight { get; set; }
	}

	private class TileRenderInfo
	{
		public int Id { get; set; }
		public int Column { get; set; }
		public int Row { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public int Depth { get; set; }
		public string? Logo { get; set; }
		public bool Visible { get; set; }
	}

	private class ConnectorRenderInfo
	{
		public TileConnector Connector { get; set; } = new();
		public string Name { get; set; } = string.Empty;
		public int Depth { get; set; }
	}

	private class LineInfo
	{
		public double X1 { get; set; }
		public double Y1 { get; set; }
		public double X2 { get; set; }
		public double Y2 { get; set; }
	}

	private record AdjacentTile(int Column, int Row, string Type);

	public async ValueTask DisposeAsync()
	{
		_isDisposed = true;
		StopAnimation();
		await ValueTask.CompletedTask.ConfigureAwait(false);
		GC.SuppressFinalize(this);
	}
}
