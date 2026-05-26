namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a single node in a PDStatusRollUp hierarchy.
/// </summary>
public class PDStatusRollUpNode
{
	/// <summary>Gets or sets the roll-up status for this node.</summary>
	public RollUpStatus Status { get; set; }

	/// <summary>Gets or sets the display title for this node.</summary>
	public string Title { get; set; } = string.Empty;

	/// <summary>Gets or sets a short summary sentence shown in the popup header.</summary>
	public string Summary { get; set; } = string.Empty;

	/// <summary>Gets or sets optional detail text (monospace, shown below summary).</summary>
	public string? Detail { get; set; }

	/// <summary>Gets or sets child nodes. Children with their own children open a deeper cascade.</summary>
	public List<PDStatusRollUpNode> Children { get; set; } = [];

	/// <summary>
	/// Controls whether this node shows a drill-down chevron when used inside a lazy-loaded popup.
	/// <c>true</c> forces drillable (e.g. children will arrive via OnBeforeExpand);
	/// <c>false</c> forces non-drillable (leaf node that will never have children);
	/// <c>null</c> (default) auto-detects — drillable only when Children is non-empty.
	/// </summary>
	public bool? Expandable { get; set; }
}
