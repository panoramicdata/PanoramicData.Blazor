namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a single node in a PDStatusCascade hierarchy.
/// </summary>
public class PDStatusCascadeNode
{
    /// <summary>Gets or sets the status for this node.</summary>
    public StatusType Status { get; set; } = StatusType.Gray;

    /// <summary>Gets or sets the display title shown in the popup header and item rows.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets a short summary sentence shown below the title in the popup header.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gets or sets optional monospace detail text shown below the summary.</summary>
    public string? Detail { get; set; }

    /// <summary>Gets or sets child nodes. Children with their own children open a deeper cascade.</summary>
    public List<PDStatusCascadeNode> Children { get; set; } = [];

    /// <summary>
    /// Controls whether this node shows a drill-down chevron in a lazy-loaded popup.
    /// <c>true</c> forces drillable (children arrive via OnBeforeExpand);
    /// <c>false</c> forces non-drillable (leaf node);
    /// <c>null</c> (default) auto-detects — drillable when Children is non-empty.
    /// </summary>
    public bool? Expandable { get; set; }
}
