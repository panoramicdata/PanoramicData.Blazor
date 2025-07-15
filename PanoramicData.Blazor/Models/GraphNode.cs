namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a node in the graph with dimensional data for visualization.
/// </summary>
public class GraphNode
{
	/// <summary>
	/// Gets or sets the unique identifier for the node.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the display label for the node.
	/// </summary>
	public string Label { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the dimensional data for visualization controls.
	/// Key is the dimension name, value is the normalized value (0.0-1.0).
	/// </summary>
	public Dictionary<string, double> Dimensions { get; set; } = new();

	/// <summary>
	/// Gets or sets the X coordinate for positioning.
	/// </summary>
	public double X { get; set; }

	/// <summary>
	/// Gets or sets the Y coordinate for positioning.
	/// </summary>
	public double Y { get; set; }

	/// <summary>
	/// Gets or sets the velocity in X direction for force-directed layout.
	/// </summary>
	public double VelocityX { get; set; }

	/// <summary>
	/// Gets or sets the velocity in Y direction for force-directed layout.
	/// </summary>
	public double VelocityY { get; set; }

	/// <summary>
	/// Gets or sets whether this node is currently selected.
	/// </summary>
	public bool IsSelected { get; set; }

	/// <summary>
	/// Gets or sets whether this node is fixed in position.
	/// </summary>
	public bool IsFixed { get; set; }
}