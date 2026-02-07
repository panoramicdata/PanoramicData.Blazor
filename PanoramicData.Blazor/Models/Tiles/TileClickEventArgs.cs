namespace PanoramicData.Blazor.Models.Tiles;

/// <summary>
/// Event arguments for tile click events.
/// </summary>
public class TileClickEventArgs
{
	/// <summary>
	/// The unique identifier of the clicked tile.
	/// </summary>
	public int TileId { get; set; }

	/// <summary>
	/// The name of the tile (derived from logo).
	/// </summary>
	public string TileName { get; set; } = string.Empty;

	/// <summary>
	/// Column position of the tile.
	/// </summary>
	public int Column { get; set; }

	/// <summary>
	/// Row position of the tile.
	/// </summary>
	public int Row { get; set; }

	/// <summary>
	/// The tile definition if available.
	/// </summary>
	public TileDefinition? Tile { get; set; }
}

/// <summary>
/// Event arguments for connector click events.
/// </summary>
public class ConnectorClickEventArgs
{
	/// <summary>
	/// Name of the connector (e.g., "TileA?TileB").
	/// </summary>
	public string ConnectorName { get; set; } = string.Empty;

	/// <summary>
	/// Starting tile coordinates.
	/// </summary>
	public TileCoordinate StartTile { get; set; } = new();

	/// <summary>
	/// Ending tile coordinates.
	/// </summary>
	public TileCoordinate EndTile { get; set; } = new();

	/// <summary>
	/// The connector definition.
	/// </summary>
	public TileConnector? Connector { get; set; }
}
