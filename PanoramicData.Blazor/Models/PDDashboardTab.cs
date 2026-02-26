namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a tab within a PDDashboard.
/// </summary>
public class PDDashboardTab
{
	/// <summary>
	/// Gets or sets the tab display name.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets optional CSS classes for this tab's content area.
	/// </summary>
	public string? Css { get; set; }

	/// <summary>
	/// Gets or sets an override column count for this tab. Null uses the dashboard default.
	/// </summary>
	public int? ColumnCount { get; set; }

	/// <summary>
	/// Gets or sets an override tile row height for this tab. Null uses the dashboard default.
	/// </summary>
	public int? TileRowHeightPx { get; set; }

	/// <summary>
	/// Gets or sets a tab-specific rotation interval override in seconds.
	/// When set, overrides the dashboard-level <see cref="PDDashboard.RotationIntervalSeconds"/> for this tab. 0 = never rotate this tab.
	/// </summary>
	public int? RotationIntervalSecondsOverride { get; set; }

	/// <summary>
	/// Gets or sets the tiles within this tab.
	/// </summary>
	public List<PDDashboardTile> Tiles { get; set; } = [];
}
