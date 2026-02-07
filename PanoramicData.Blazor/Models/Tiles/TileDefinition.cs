namespace PanoramicData.Blazor.Models.Tiles;

/// <summary>
/// Represents a single tile in the grid with optional per-tile overrides.
/// </summary>
public class TileDefinition
{
	/// <summary>
	/// Column position (0-based).
	/// </summary>
	public int Column { get; set; }

	/// <summary>
	/// Row position (0-based).
	/// </summary>
	public int Row { get; set; }

	/// <summary>
	/// Optional logo path/URL for this tile.
	/// </summary>
	public string? Logo { get; set; }

	/// <summary>
	/// Optional tile color override (hex format).
	/// </summary>
	public string? Color { get; set; }

	/// <summary>
	/// Optional depth override (percentage).
	/// </summary>
	public int? Depth { get; set; }

	/// <summary>
	/// Optional logo size override (percentage).
	/// </summary>
	public int? LogoSize { get; set; }

	/// <summary>
	/// Optional logo rotation override (degrees).
	/// </summary>
	public int? LogoRotation { get; set; }

	/// <summary>
	/// Optional reflection intensity override (percentage).
	/// </summary>
	public int? Reflection { get; set; }

	/// <summary>
	/// Optional reflection depth override (percentage).
	/// </summary>
	public int? ReflectionDepth { get; set; }

	/// <summary>
	/// Optional glow override (percentage).
	/// </summary>
	public int? Glow { get; set; }

	/// <summary>
	/// Whether this tile is visible.
	/// </summary>
	public bool Visible { get; set; } = true;

	/// <summary>
	/// Custom data attached to this tile.
	/// </summary>
	public object? Tag { get; set; }
}
