namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents the complete graph data containing nodes and edges.
/// </summary>
public class GraphData
{
	/// <summary>
	/// Gets or sets the collection of nodes in the graph.
	/// </summary>
	public List<GraphNode> Nodes { get; set; } = new();

	/// <summary>
	/// Gets or sets the collection of edges connecting the nodes.
	/// </summary>
	public List<GraphEdge> Edges { get; set; } = new();
}