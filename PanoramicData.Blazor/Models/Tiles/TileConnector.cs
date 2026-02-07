namespace PanoramicData.Blazor.Models.Tiles;

/// <summary>
/// Represents a connector between two tiles.
/// </summary>
public class TileConnector
{
	/// <summary>
	/// Starting tile coordinates.
	/// </summary>
	public TileCoordinate StartTile { get; set; } = new();

	/// <summary>
	/// Ending tile coordinates.
	/// </summary>
	public TileCoordinate EndTile { get; set; } = new();

	/// <summary>
	/// Direction type of the connector.
	/// </summary>
	public string Direction { get; set; } = string.Empty;

	/// <summary>
	/// Whether the connector direction is reversed.
	/// </summary>
	public bool Reversed { get; set; }

	/// <summary>
	/// Connector color (hex format).
	/// </summary>
	public string Color { get; set; } = "#00FFFF";

	/// <summary>
	/// Opacity percentage (0-100).
	/// </summary>
	public int Opacity { get; set; } = 80;

	/// <summary>
	/// Animation speed (0 = off).
	/// </summary>
	public int AnimationSpeed { get; set; } = 35;

	/// <summary>
	/// Fill pattern for this connector.
	/// </summary>
	public ConnectorFillPattern FillPattern { get; set; } = ConnectorFillPattern.Solid;

	/// <summary>
	/// Edge index when multiple connectors per edge.
	/// </summary>
	public int EdgeIndex { get; set; }

	/// <summary>
	/// Total connectors on this edge.
	/// </summary>
	public int EdgeTotal { get; set; } = 1;

	/// <summary>
	/// Optional height override (percentage of tile depth).
	/// </summary>
	public int? Height { get; set; }

	/// <summary>
	/// Optional vertical alignment override.
	/// </summary>
	public ConnectorVerticalAlign? VerticalAlign { get; set; }
}

/// <summary>
/// Simple coordinate for a tile in the grid.
/// </summary>
public class TileCoordinate
{
	/// <summary>
	/// Column position (0-based).
	/// </summary>
	public int Column { get; set; }

	/// <summary>
	/// Row position (0-based).
	/// </summary>
	public int Row { get; set; }
}
