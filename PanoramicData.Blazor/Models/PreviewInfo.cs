namespace PanoramicData.Blazor.Models;

public class PreviewInfo
{
	public bool PreviewAvailable { get; set; }

	public string Url { get; set; } = string.Empty;

	public MarkupString HtmlContent { get; set; }
}
