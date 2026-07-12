namespace PanoramicData.Blazor.Models;

/// <summary>
/// Holds information about a DOM element, including its tag, CSS classes, and parent chain, as returned from JavaScript interop.
/// </summary>
public class ElementInfo
{
	/// <summary>Gets or sets the list of CSS classes applied to this element.</summary>
	public string[] ClassList { get; set; } = [];

	/// <summary>Gets or sets the <c>id</c> attribute value of this element.</summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>Gets or sets the parent element, or <c>null</c> when this is the root element.</summary>
	public ElementInfo? Parent { get; set; }

	/// <summary>Gets or sets the HTML tag name of this element (e.g. <c>"div"</c>).</summary>
	public string Tag { get; set; } = string.Empty;

	/// <summary>
	/// Searches for and returns first matching ancestor.
	/// </summary>
	/// <param name="tag">The HTML Tag name to search for.</param>
	/// <param name="classes">Zero or more classes to match.</param>
	/// <returns>An ElementInfo instance of the matched element or null if no match.</returns>
	public ElementInfo? Find(string tag, params string[] classes)
	{
		var el = this.Parent;
		while (el != null)
		{
			if (el.Tag == tag && !classes.Except(el.ClassList).Any())
			{
				return el;
			}

			el = el.Parent;
		}

		return null;
	}

	/// <summary>
	/// Searches ancestors for given tag that has any classes provided.
	/// </summary>
	/// <param name="tag">The HTML Tag name to search for.</param>
	/// <param name="classes">Zero or more classes to match.</param>
	/// <returns>true if any ancestor matches, otherwise false.</returns>
	public bool HasAncestor(string tag, params string[] classes) => Find(tag, classes) != null;
}
