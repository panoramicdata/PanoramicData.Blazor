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
	public int LogoRotation { get; set; }

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
	/// Background color (hex format). Set to empty string or "transparent" for no background.
	/// </summary>
	public string BackgroundColor { get; set; } = "#000624";

	/// <summary>
	/// Whether to show the background color. When false, the component is transparent.
	/// </summary>
	public bool ShowBackground { get; set; } = true;

	/// <summary>
	/// Grid line color (hex format).
	/// </summary>
	public string LineColor { get; set; } = "#c0c0c0";

	/// <summary>
	/// Grid line opacity percentage (0-100).
	/// </summary>
	public int LineOpacity { get; set; } = 15;

	/// <summary>
	/// Background glow intensity percentage (0-100).
	/// </summary>
	public int Glow { get; set; } = 30;

	/// <summary>
	/// Glow falloff as percentage of grid width (100-500). Higher values = softer falloff.
	/// </summary>
	public int GlowFalloff { get; set; } = 100;

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
	public int Perspective { get; set; }

	/// <summary>
	/// Whether to show floating icons.
	/// </summary>
	public bool ShowFloating { get; set; }

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
	public int Padding { get; set; } = 5;

	/// <summary>
	/// Alignment of the tile grid within the viewbox.
	/// </summary>
	public GridAlignment Alignment { get; set; } = GridAlignment.MiddleRight;

	/// <summary>
	/// Maximum grid width as percentage of container width (1-100). Null means no limit.
	/// </summary>
	public int? MaxGridWidthPercent { get; set; }

	/// <summary>
	/// Maximum grid height as percentage of container height (1-100). Null means no limit.
	/// </summary>
	public int? MaxGridHeightPercent { get; set; }

	/// <summary>
	/// When true and ChildContent is provided, text wraps around the tile grid
	/// in a newspaper-style layout using CSS float, instead of overlaying the grid.
	/// Requires MaxGridWidthPercent to be set so the grid doesn't fill the full width.
	/// </summary>
	public bool ContentWrapping { get; set; }
}
