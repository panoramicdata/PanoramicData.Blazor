namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a tile within a PDDashboard tab, positioned on a CSS grid.
/// </summary>
public class PDDashboardTile
{
	/// <summary>
	/// Gets or sets the zero-based row index.
	/// </summary>
	public int RowIndex { get; set; }

	/// <summary>
	/// Gets or sets the zero-based column index.
	/// </summary>
	public int ColumnIndex { get; set; }

	/// <summary>
	/// Gets or sets the number of rows this tile spans. Minimum 1.
	/// </summary>
	public int RowSpanCount { get; set; } = 1;

	/// <summary>
	/// Gets or sets the number of columns this tile spans. Minimum 1.
	/// </summary>
	public int ColumnSpanCount { get; set; } = 1;

	/// <summary>
	/// Gets or sets optional CSS classes for this tile.
	/// </summary>
	public string? Css { get; set; }

	/// <summary>
	/// Gets or sets whether the maximize button is shown in view mode for this tile.
	/// When null, inherits the dashboard-level ShowMaximize setting.
	/// </summary>
	public bool? ShowMaximize { get; set; }

	/// <summary>
	/// Gets or sets the render fragment for the tile content.
	/// </summary>
	public RenderFragment? ChildContent { get; set; }
}
