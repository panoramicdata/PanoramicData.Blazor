namespace PanoramicData.Blazor.Models;

public class PreviewInfo
{
	public string CssClass { get; set; } = string.Empty;

	public bool PreviewAvailable { get; set; }

	public string Url { get; set; } = string.Empty;

	public MarkupString HtmlContent { get; set; }
}
