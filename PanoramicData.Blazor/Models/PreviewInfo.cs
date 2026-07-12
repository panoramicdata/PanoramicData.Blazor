namespace PanoramicData.Blazor.Models;

/// <summary>
/// Holds the resolved preview content for a file explorer item.
/// </summary>
public class PreviewInfo
{
	/// <summary>Gets or sets additional CSS classes applied to the preview container.</summary>
	public string CssClass { get; set; } = string.Empty;

	/// <summary>Gets or sets a value indicating whether a preview is available for the item.</summary>
	public bool PreviewAvailable { get; set; }

	/// <summary>Gets or sets the URL of the preview resource (e.g. an image or document).</summary>
	public string Url { get; set; } = string.Empty;

	/// <summary>Gets or sets inline HTML markup that is rendered directly as the preview content.</summary>
	public MarkupString HtmlContent { get; set; }
}
