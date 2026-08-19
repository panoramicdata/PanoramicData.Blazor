namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Determines how the size of a branch node is derived when laying out a tree map.
/// </summary>
/// <remarks>
/// This matters because data sources differ. A file system typically reports zero bytes for a directory,
/// so a directory's size is only meaningful once its descendants are summed. A database size query, by
/// contrast, usually reports a total that already includes everything beneath it, and summing again
/// would double count.
/// </remarks>
public enum TreeMapSizeMode
{
	/// <summary>
	/// A branch node's size is its own size plus the sizes of all of its descendants.
	/// Use this when the size selector reports only what a node holds directly, such as a
	/// directory listing that reports zero bytes for directories.
	/// </summary>
	Aggregate,

	/// <summary>
	/// A branch node's size is exactly the value returned by the size selector, and descendant
	/// sizes are not added to it. Use this when the size selector already reports subtree totals.
	/// </summary>
	Explicit
}
