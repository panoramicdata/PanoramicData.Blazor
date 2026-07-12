namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies the direction in which items are sorted in a data table or list.
/// </summary>
public enum SortDirection
{
	/// <summary>No sort is applied; items appear in their natural order.</summary>
	None,
	/// <summary>Items are sorted from smallest to largest (A-Z, 0-9, oldest first).</summary>
	Ascending,
	/// <summary>Items are sorted from largest to smallest (Z-A, 9-0, newest first).</summary>
	Descending
}
