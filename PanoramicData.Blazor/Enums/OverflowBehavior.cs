namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Defines the overflow behavior for widget content.
/// </summary>
public enum OverflowBehavior
{
	/// <summary>
	/// Content is clipped with no scrollbars. Default.
	/// </summary>
	Hidden,

	/// <summary>
	/// Scrollbars appear when content overflows.
	/// </summary>
	Auto,

	/// <summary>
	/// Scrollbars are always visible.
	/// </summary>
	Scroll,

	/// <summary>
	/// Content overflows visibly (not clipped).
	/// </summary>
	Visible
}
