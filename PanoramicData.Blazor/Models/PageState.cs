namespace PanoramicData.Blazor.Models;

/// <summary>
/// A lightweight snapshot of pager state that can be persisted and restored across page loads.
/// </summary>
public class PageState
{
	/// <summary>Gets or sets the current page number (1-based).</summary>
	public uint Page { get; set; } = 1;
	/// <summary>Gets or sets the total number of pages.</summary>
	public uint PageCount { get; set; }
	/// <summary>Gets or sets the number of items displayed per page.</summary>
	public uint PageSize { get; set; } = 10;
}