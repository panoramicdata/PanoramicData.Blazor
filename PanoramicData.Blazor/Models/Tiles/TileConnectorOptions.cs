namespace PanoramicData.Blazor.Models.Tiles;

/// <summary>
/// Configuration options for tile connectors.
/// </summary>
public class TileConnectorOptions
{
	/// <summary>
	/// Fill pattern for connectors.
	/// </summary>
	public ConnectorFillPattern FillPattern { get; set; } = ConnectorFillPattern.Random;

	/// <summary>
	/// Direction filter for connectors.
	/// </summary>
	public ConnectorDirection Direction { get; set; } = ConnectorDirection.All;

	/// <summary>
	/// Number of connectors per edge (null = random).
	/// </summary>
	public int? PerEdge { get; set; } = null;

	/// <summary>
	/// Population percentage of eligible edges that get connectors (0-100).
	/// </summary>
	public int Population { get; set; } = 50;

	/// <summary>
	/// Connector height as percentage of tile depth (0-100).
	/// </summary>
	public int Height { get; set; } = 100;

	/// <summary>
	/// Vertical alignment of connectors.
	/// </summary>
	public ConnectorVerticalAlign VerticalAlign { get; set; } = ConnectorVerticalAlign.Bottom;

	/// <summary>
	/// Connector opacity percentage (0-100).
	/// </summary>
	public int Opacity { get; set; } = 80;

	/// <summary>
	/// Whether connector animation is enabled.
	/// </summary>
	public bool Animation { get; set; } = true;

	/// <summary>
	/// Animation speed (0 = off, 1-400).
	/// </summary>
	public int AnimationSpeed { get; set; } = 35;
}

/// <summary>
/// Fill pattern options for connectors.
/// </summary>
public enum ConnectorFillPattern
{
	Random,
	Solid,
	Bars,
	Chevrons
}

/// <summary>
/// Direction filter for connectors.
/// </summary>
public enum ConnectorDirection
{
	/// <summary>All directions including diagonal.</summary>
	All,
	/// <summary>Only orthogonal (up, down, left, right).</summary>
	Orthogonal,
	/// <summary>Only diagonal.</summary>
	Diagonal,
	/// <summary>Diagonal left-right only.</summary>
	DiagonalLeftRight,
	/// <summary>Diagonal front-back only.</summary>
	DiagonalFrontBack
}

/// <summary>
/// Vertical alignment options for connectors.
/// </summary>
public enum ConnectorVerticalAlign
{
	Bottom,
	Center,
	Top
}
