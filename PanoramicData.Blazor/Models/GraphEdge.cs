namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents an edge in the graph connecting two nodes.
/// </summary>
public class GraphEdge
{
	/// <summary>
	/// Gets or sets the unique identifier for the edge.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the ID of the source node.
	/// </summary>
	public string FromNodeId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the ID of the target node.
	/// </summary>
	public string ToNodeId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the dimensional data for visualization controls.
	/// Key is the dimension name, value is the normalized value (0.0-1.0).
	/// </summary>
	public Dictionary<string, double> Dimensions { get; set; } = new();

	/// <summary>
	/// Gets or sets the strength of the connection for force-directed layout.
	/// </summary>
	public double Strength { get; set; } = 1.0;

	/// <summary>
	/// Gets or sets the display label for the edge.
	/// </summary>
	public string Label { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether this edge is currently selected.
	/// </summary>
	public bool IsSelected { get; set; }
}