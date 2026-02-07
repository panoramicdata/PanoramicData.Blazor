namespace PanoramicData.Blazor.Models.Tiles;

/// <summary>
/// Specifies the alignment of the tile grid within its container.
/// </summary>
public enum GridAlignment
{
	/// <summary>Top-left corner alignment.</summary>
	TopLeft,

	/// <summary>Top-center alignment.</summary>
	TopCenter,

	/// <summary>Top-right corner alignment.</summary>
	TopRight,

	/// <summary>Middle-left alignment.</summary>
	MiddleLeft,

	/// <summary>Middle-center alignment (default).</summary>
	MiddleCenter,

	/// <summary>Middle-right alignment.</summary>
	MiddleRight,

	/// <summary>Bottom-left corner alignment.</summary>
	BottomLeft,

	/// <summary>Bottom-center alignment.</summary>
	BottomCenter,

	/// <summary>Bottom-right corner alignment.</summary>
	BottomRight
}
