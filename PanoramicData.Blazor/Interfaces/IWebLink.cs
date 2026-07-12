namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines properties for items that represent a hyperlink.
/// </summary>
public interface IWebLink
{
	/// <summary>Gets or sets the HTML anchor <c>target</c> attribute value (e.g. <c>"_blank"</c>).</summary>
	string Target { get; set; }

	/// <summary>Gets or sets the URL that the link points to.</summary>
	string Url { get; set; }
}
