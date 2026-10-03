using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor;

/// <summary>
/// PDTiles: connector geometry (attachment points, straight ribbons and bezier curves).
/// </summary>
public partial class PDTiles
{
	private const double _connectorYNudge = 3;

	/// <summary>
	/// Determines if the current connection mode uses bezier curves.
	/// </summary>
	private bool UsesBezierCurves => ConnectorOptions.ConnectionMode is ConnectionMode.RowCurves or ConnectionMode.ColumnCurves;

	private Dictionary<int, List<ConnectorRenderInfo>> GetConnectorsByDepth()
	{
		var result = new Dictionary<int, List<ConnectorRenderInfo>>();

		foreach (var conn in Connectors ?? [])
		{
			var connDepth = GetConnectorDepth(conn);

			var startLogo = GetTileLogo(conn.StartTile.Column, conn.StartTile.Row);
			var endLogo = GetTileLogo(conn.EndTile.Column, conn.EndTile.Row);
			// Include edge index to make name unique for multiple connectors between same tiles
			var connName = $"{GetTileName(startLogo)}?{GetTileName(endLogo)}#{conn.EdgeIndex}";

			if (!result.TryGetValue(connDepth, out var value))
			{
				value = [];
				result[connDepth] = value;
			}

			value.Add(new ConnectorRenderInfo
			{
				Connector = conn,
				Name = connName,
				Depth = connDepth
			});
		}

		return result;
	}

	/// <summary>
	/// Gets the depth at which a connector is rendered, matching <see cref="GetTileDepth"/> for the connection mode.
	/// </summary>
	private int GetConnectorDepth(TileConnector conn) => ConnectorOptions.ConnectionMode switch
	{
		// Row Curves: depth by ROW only - render after that row's tiles
		ConnectionMode.RowCurves => Math.Min(conn.StartTile.Row, conn.EndTile.Row),
		// Column Curves: depth by COLUMN only
		ConnectionMode.ColumnCurves => Math.Min(conn.StartTile.Column, conn.EndTile.Column),
		// Standard straight-line: use max of row+col depth
		_ => Math.Max(conn.StartTile.Row + conn.StartTile.Column, conn.EndTile.Row + conn.EndTile.Column)
	};

	/// <summary>
	/// Gets the top and bottom edges of a connector ribbon at each end, or null when either end tile is not in the grid.
	/// </summary>
	private ConnectorRibbon? GetConnectorRibbon(ConnectorRenderInfo conn)
	{
		var tiles = GetSortedTiles();
		var startTile = FindTile(tiles, conn.Connector.StartTile);
		var endTile = FindTile(tiles, conn.Connector.EndTile);

		if (startTile == null || endTile == null)
		{
			return null;
		}

		var direction = conn.Connector.Direction;
		var edgeIndex = conn.Connector.EdgeIndex;
		var edgeTotal = conn.Connector.EdgeTotal;

		// Get attachment points for connectors - use opposite direction for end tile (like JS version)
		var startPoints = GetTileAttachmentPoints(startTile, direction, true, edgeIndex, edgeTotal);
		var endPoints = GetTileAttachmentPoints(endTile, GetOppositeDirection(direction), false, edgeIndex, edgeTotal);

		// Calculate ribbon height based on connector settings
		var connHeight = (conn.Connector.Height ?? ConnectorOptions.Height) / 100.0;
		var vAlign = conn.Connector.VerticalAlign ?? ConnectorOptions.VerticalAlign;

		var startRange = startPoints.Bottom - startPoints.Top;
		var endRange = endPoints.Bottom - endPoints.Top;
		var ribbonHeight = Math.Min(startRange, endRange) * connHeight;

		var (startTop, startBottom) = AlignRibbon(startPoints, ribbonHeight, vAlign);
		var (endTop, endBottom) = AlignRibbon(endPoints, ribbonHeight, vAlign);
		return new ConnectorRibbon((startPoints.X, startTop), (endPoints.X, endTop), (startPoints.X, startBottom), (endPoints.X, endBottom));
	}

	private static TileRenderInfo? FindTile(List<TileRenderInfo> tiles, TileCoordinate coordinate)
		=> tiles.FirstOrDefault(t => t.Column == coordinate.Column && t.Row == coordinate.Row);

	/// <summary>
	/// Positions a ribbon of the given height within an attachment point's vertical range.
	/// </summary>
	private static (double Top, double Bottom) AlignRibbon(AttachmentPoints points, double ribbonHeight, ConnectorVerticalAlign vAlign)
	{
		switch (vAlign)
		{
			case ConnectorVerticalAlign.Top:
				return (points.Top, points.Top + ribbonHeight);
			case ConnectorVerticalAlign.Center:
				var mid = (points.Top + points.Bottom) / 2;
				return (mid - ribbonHeight / 2, mid + ribbonHeight / 2);
			default: // Bottom
				return (points.Bottom - ribbonHeight, points.Bottom);
		}
	}

	private AttachmentPoints GetTileAttachmentPoints(TileRenderInfo tile, string direction, bool isOutgoing, int edgeIndex, int edgeTotal)
	{
		var tileDepth = GetTileProperty(tile.Column, tile.Row, t => t.Depth, Options.Depth);
		var d = _tileWidth * (tileDepth / 100.0);

		var (ptX, ptY) = direction.StartsWith("diag-", StringComparison.Ordinal)
			? GetCornerAttachmentPoint(tile.X, tile.Y, direction)
			: GetEdgeAttachmentPoint(tile.X, tile.Y, direction, isOutgoing, edgeIndex, edgeTotal);

		return new AttachmentPoints(ptX, ptY, ptY + d);
	}

	/// <summary>
	/// Diagonal connectors attach at the tile corner they point towards.
	/// </summary>
	private static (double X, double Y) GetCornerAttachmentPoint(double x, double y, string direction)
	{
		var corner = direction switch
		{
			"diag-back" => _tileBack,
			"diag-right" => _tileRight,
			"diag-left" => _tileLeft,
			_ => _tileFront // diag-front
		};

		var isLeftRight = corner == _tileLeft || corner == _tileRight;
		var nudge = isLeftRight ? -_connectorYNudge : _connectorYNudge;
		return (x + corner.X, y + corner.Y + nudge);
	}

	/// <summary>
	/// Orthogonal connectors attach along the tile edge they leave or enter, spread out by edge index.
	/// </summary>
	private static (double X, double Y) GetEdgeAttachmentPoint(double x, double y, string direction, bool isOutgoing, int edgeIndex, int edgeTotal)
	{
		var frac = isOutgoing
			? (edgeIndex + 1.0) / (edgeTotal + 1.0)
			: (edgeTotal - edgeIndex) / (edgeTotal + 1.0);

		var (p0, p1) = GetAttachmentEdge(direction, isOutgoing);
		return (x + p0.X + (p1.X - p0.X) * frac, y + p0.Y + (p1.Y - p0.Y) * frac + _connectorYNudge);
	}

	private static (TilePoint From, TilePoint To) GetAttachmentEdge(string direction, bool isOutgoing)
	{
		if (isOutgoing)
		{
			return direction is "right" or "up" ? (_tileFront, _tileRight) : (_tileLeft, _tileFront);
		}

		return direction is "left" or "down" ? (_tileBack, _tileLeft) : (_tileRight, _tileBack);
	}

	private ((double X, double Y) t0, (double X, double Y) t1, (double X, double Y) b0, (double X, double Y) b1, string points)? GetConnectorData(ConnectorRenderInfo conn)
	{
		if (GetConnectorRibbon(conn) is not { } ribbon)
		{
			return null;
		}

		// Points are: startTop, endTop, endBottom, startBottom
		var points = $"{FormatPoint(ribbon.StartTop)} {FormatPoint(ribbon.EndTop)} {FormatPoint(ribbon.EndBottom)} {FormatPoint(ribbon.StartBottom)}";
		return (ribbon.StartTop, ribbon.EndTop, ribbon.StartBottom, ribbon.EndBottom, points);
	}

	/// <summary>
	/// Gets the bezier curve path data for a curved connector.
	/// </summary>
	private BezierConnectorData? GetBezierConnectorData(ConnectorRenderInfo conn)
	{
		// Use the SAME attachment point logic as straight-line connectors
		if (GetConnectorRibbon(conn) is not { } ribbon)
		{
			return null;
		}

		var direction = conn.Connector.Direction;

		// Calculate control point distance based on tension
		var tension = ConnectorOptions.CurveTension / 100.0;
		var distance = Math.Sqrt(Math.Pow(ribbon.EndTop.X - ribbon.StartTop.X, 2) + Math.Pow(ribbon.EndTop.Y - ribbon.StartTop.Y, 2));
		var controlDistance = distance * tension * 0.5;

		// Control points are PERPENDICULAR TO THE TILE EDGE, pointing outward
		// This makes curves exit/enter the tile edge at right angles
		var startPerp = GetEdgeOutwardPerpendicular(direction, true);
		var endPerp = GetEdgeOutwardPerpendicular(GetOppositeDirection(direction), false);

		var startTopCtrl = Offset(ribbon.StartTop, startPerp, controlDistance);
		var endTopCtrl = Offset(ribbon.EndTop, endPerp, controlDistance);
		var startBottomCtrl = Offset(ribbon.StartBottom, startPerp, controlDistance);
		var endBottomCtrl = Offset(ribbon.EndBottom, endPerp, controlDistance);

		// Stroke paths for the top and bottom edges
		var topStrokePath = $"M {FormatPoint(ribbon.StartTop)} C {FormatPoint(startTopCtrl)} {FormatPoint(endTopCtrl)} {FormatPoint(ribbon.EndTop)}";
		var bottomStrokePath = $"M {FormatPoint(ribbon.StartBottom)} C {FormatPoint(startBottomCtrl)} {FormatPoint(endBottomCtrl)} {FormatPoint(ribbon.EndBottom)}";

		// Fill path
		// Top edge: start -> end
		// Right edge: end top -> end bottom (straight line)
		// Bottom edge: end bottom -> start bottom (reversed curve)
		// Left edge: start bottom -> start top (straight line)
		var pathData = $"{topStrokePath} L {FormatPoint(ribbon.EndBottom)} C {FormatPoint(endBottomCtrl)} {FormatPoint(startBottomCtrl)} {FormatPoint(ribbon.StartBottom)} Z";

		return new BezierConnectorData(
			pathData,
			topStrokePath,
			bottomStrokePath,
			ribbon.StartTop,
			ribbon.EndTop,
			ribbon.StartBottom,
			ribbon.EndBottom,
			startTopCtrl,
			endTopCtrl,
			startBottomCtrl,
			endBottomCtrl
		);
	}

	private static (double X, double Y) Offset((double X, double Y) point, (double X, double Y) direction, double distance)
		=> (point.X + direction.X * distance, point.Y + direction.Y * distance);

	private static string FormatPoint((double X, double Y) point) => $"{F(point.X)},{F(point.Y)}";

	/// <summary>
	/// Gets the outward perpendicular direction for a tile edge based on connector direction.
	/// </summary>
	private static (double X, double Y) GetEdgeOutwardPerpendicular(string direction, bool isOutgoing)
	{
		if (isOutgoing)
		{
			return direction switch
			{
				"right" or "up" => (0.707, 0.707),
				"left" or "down" => (-0.707, 0.707),
				_ => (0.0, 1.0)
			};
		}

		return direction switch
		{
			"left" or "down" => (-0.707, -0.707),
			"right" or "up" => (0.707, -0.707),
			_ => (0.0, -1.0)
		};
	}

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
}
