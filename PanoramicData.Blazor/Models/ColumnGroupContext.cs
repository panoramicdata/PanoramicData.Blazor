namespace PanoramicData.Blazor.Models;

/// <summary>
/// Metadata describing a table column group. Supplied by a <c>PDColumnGroup</c> wrapper and consumed
/// by <c>PDColumnGrouper</c> when rendering its facet pills.
/// </summary>
public class ColumnGroupContext
{
	/// <summary>
	/// Gets or sets the unique name of the column group. This is also used as the facet pill label.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets an optional icon CSS class shown on the facet pill (e.g. "fas fa-chart-bar").
	/// </summary>
	public string? Icon { get; set; }

	/// <summary>
	/// Gets or sets the order in which the facet pill appears. Lower values appear first.
	/// </summary>
	public int Ordinal { get; set; } = 1000;

	/// <summary>
	/// Gets or sets an optional description used as the facet pill tooltip.
	/// </summary>
	public string? Description { get; set; }
}
