namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies the docking mode of the <see cref="PanoramicData.Blazor.PDChat"/> component.
/// </summary>
public enum PDChatDockMode
{
	/// <summary>The chat panel is minimized to a button.</summary>
	Minimized,
	/// <summary>The chat panel is anchored to the bottom-right corner of the viewport.</summary>
	BottomRight,
	/// <summary>The chat panel is anchored to the top-right corner of the viewport.</summary>
	TopRight,
	/// <summary>The chat panel is anchored to the bottom-left corner of the viewport.</summary>
	BottomLeft,
	/// <summary>The chat panel is anchored to the top-left corner of the viewport.</summary>
	TopLeft,
	/// <summary>The chat panel expands to fill the entire viewport.</summary>
	FullScreen,
	/// <summary>The chat panel is docked to the left side of its container.</summary>
	Left,
	/// <summary>The chat panel is docked to the right side of its container.</summary>
	Right
}
