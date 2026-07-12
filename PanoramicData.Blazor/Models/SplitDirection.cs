namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies the axis along which a <see cref="PanoramicData.Blazor.PDSplitter"/> divides its panels.
/// </summary>
public enum SplitDirection
{
	/// <summary>Panels are arranged side by side with a vertical divider bar.</summary>
	Horizontal,
	/// <summary>Panels are stacked vertically with a horizontal divider bar.</summary>
	Vertical
}
