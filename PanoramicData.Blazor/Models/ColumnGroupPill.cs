namespace PanoramicData.Blazor.Models;

/// <summary>
/// A single facet computed by <c>ColumnGroupHelper.BuildPills</c> for rendering by <c>PDColumnGrouper</c>.
/// </summary>
public class ColumnGroupPill
{
	/// <summary>Gets or sets the column group name, also used as the pill label.</summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>Gets or sets the optional icon CSS class for the pill.</summary>
	public string? Icon { get; set; }

	/// <summary>Gets or sets the optional tooltip for the pill.</summary>
	public string? Description { get; set; }

	/// <summary>Gets or sets the number of listable columns that belong to this group.</summary>
	public int Count { get; set; }
}
