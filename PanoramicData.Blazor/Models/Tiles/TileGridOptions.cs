namespace PanoramicData.Blazor.Models.Tiles;

/// <summary>
/// Configuration options for the PDTiles component.
/// </summary>
public class TileGridOptions
{
	/// <summary>
	/// Number of columns in the grid.
	/// </summary>
	public int Columns { get; set; } = 3;

	/// <summary>
	/// Number of rows in the grid.
	/// </summary>
	public int Rows { get; set; } = 3;

	/// <summary>
	/// Tile depth as percentage of tile width (0-100).
	/// </summary>
	public int Depth { get; set; } = 15;

	/// <summary>
	/// Logo size as percentage (0-100).
	/// </summary>
	public int LogoSize { get; set; } = 80;

	/// <summary>
	/// Logo rotation in degrees.
	/// </summary>
	public int LogoRotation { get; set; } = 0;

	/// <summary>
	/// Gap between tiles as percentage (0-300).
	/// </summary>
	public int Gap { get; set; } = 100;

	/// <summary>
	/// Percentage of tiles visible (0-100).
	/// </summary>
	public int Population { get; set; } = 100;

	/// <summary>
	/// Base tile color (hex format).
	/// </summary>
	public string TileColor { get; set; } = "#373737";

	/// <summary>
	/// Background color (hex format).
	/// </summary>
	public string BackgroundColor { get; set; } = "#000624";

	/// <summary>
	/// Grid line color (hex format).
	/// </summary>
	public string LineColor { get; set; } = "#ffffff";

	/// <summary>
	/// Grid line opacity percentage (0-100).
	/// </summary>
	public int LineOpacity { get; set; } = 15;

	/// <summary>
	/// Background glow percentage (0-100).
	/// </summary>
	public int Glow { get; set; } = 50;

	/// <summary>
	/// Reflection intensity percentage (0-100).
	/// </summary>
	public int Reflection { get; set; } = 75;

	/// <summary>
	/// Reflection depth percentage (0-100).
	/// </summary>
	public int ReflectionDepth { get; set; } = 100;

	/// <summary>
	/// Perspective tilt percentage (0-100).
	/// </summary>
	public int Perspective { get; set; } = 0;

	/// <summary>
	/// Whether to show floating icons.
	/// </summary>
	public bool ShowFloating { get; set; } = false;

	/// <summary>
	/// Floating icons population percentage (0-100).
	/// </summary>
	public int FloatingPopulation { get; set; } = 50;

	/// <summary>
	/// Floating icons height percentage (0-100).
	/// </summary>
	public int FloatHeight { get; set; } = 80;

	/// <summary>
	/// Floating icons size percentage (0-100).
	/// </summary>
	public int FloatSize { get; set; } = 60;

	/// <summary>
	/// Scale of the tile grid as percentage (0-200). 100% means tiles outer edges touch the viewbox edge.
	/// </summary>
	public int Scale { get; set; } = 100;

	/// <summary>
	/// Padding around the tile grid as percentage (0-50).
	/// </summary>
	public int Padding { get; set; } = 0;

	/// <summary>
	/// Alignment of the tile grid within the viewbox.
	/// </summary>
	public GridAlignment Alignment { get; set; } = GridAlignment.MiddleCenter;
}
