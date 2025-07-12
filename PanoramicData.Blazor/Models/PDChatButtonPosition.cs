namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies the position where the minimized chat button appears on screen.
/// </summary>
public enum PDChatButtonPosition
{
	/// <summary>
	/// No button is shown - developer will provide their own chat trigger.
	/// </summary>
	None,
	
	/// <summary>
	/// Button appears in the top-left corner.
	/// </summary>
	TopLeft,
	
	/// <summary>
	/// Button appears in the top-right corner.
	/// </summary>
	TopRight,
	
	/// <summary>
	/// Button appears in the bottom-left corner.
	/// </summary>
	BottomLeft,
	
	/// <summary>
	/// Button appears in the bottom-right corner (default).
	/// </summary>
	BottomRight
}