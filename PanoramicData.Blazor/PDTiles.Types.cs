using PanoramicData.Blazor.Models.Tiles;

namespace PanoramicData.Blazor;

/// <summary>
/// PDTiles: internal rendering types.
/// </summary>
public partial class PDTiles
{
	private sealed record TilePoint(double X, double Y);

	private sealed record AttachmentPoints(double X, double Top, double Bottom);

	/// <summary>
	/// The top and bottom corners of a connector ribbon at its start and end tiles.
	/// </summary>
	private sealed record ConnectorRibbon(
		(double X, double Y) StartTop,
		(double X, double Y) EndTop,
		(double X, double Y) StartBottom,
		(double X, double Y) EndBottom);

	private sealed record TopFaceFilter(string? Id, double? Glow);

	private sealed record AdjacentTile(int Column, int Row, string Type);

	private sealed record BezierConnectorData(
		string FillPath,
		string TopStrokePath,
		string BottomStrokePath,
		(double X, double Y) StartTop,
		(double X, double Y) EndTop,
		(double X, double Y) StartBottom,
		(double X, double Y) EndBottom,
		(double X, double Y) StartTopCtrl,
		(double X, double Y) EndTopCtrl,
		(double X, double Y) StartBottomCtrl,
		(double X, double Y) EndBottomCtrl
	);

	private sealed class TileColors
	{
		public string Dark { get; set; } = "#000000";
		public string Mid { get; set; } = "#333333";
		public string Base { get; set; } = "#666666";
		public string Light { get; set; } = "#999999";
	}

	private sealed class TileGradientInfo
	{
		public string TopGradId { get; set; } = string.Empty;
		public string FrontGradId { get; set; } = string.Empty;
		public TileColors Colors { get; set; } = new();
	}

	private sealed class LayoutInfo
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

	private sealed class TileRenderInfo
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

	private sealed class ConnectorRenderInfo
	{
		public TileConnector Connector { get; set; } = new();
		public string Name { get; set; } = string.Empty;
		public int Depth { get; set; }
	}

	private sealed class LineInfo
	{
		public double X1 { get; set; }
		public double Y1 { get; set; }
		public double X2 { get; set; }
		public double Y2 { get; set; }
	}
}
