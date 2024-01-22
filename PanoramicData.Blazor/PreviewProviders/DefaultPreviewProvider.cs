using Markdig;

namespace PanoramicData.Blazor.PreviewProviders;

public class DefaultPreviewProvider : IPreviewProvider
{
	public string DateTimeFormat { get; set; } = "dd/MM/yy HH:mm:ss";

	public virtual async Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item)
	{
		var info = new PreviewInfo();

		if (item == null || item.EntryType == FileExplorerItemType.Directory)
		{
			info.HtmlContent = new MarkupString("<span>No Preview</span>");
		}
		else if (item.FileExtension == "md")
		{
			// download content and convert markdown to html
			var contentBytes = await DownloadContentAsync(item);
			if (contentBytes.Length > 0)
			{
				string contentString = Encoding.UTF8.GetString(contentBytes);
				var result = Markdown.ToHtml(contentString);
				info.HtmlContent = new MarkupString(result);
			}
			// convert markdown to html
		}

		// default is to show basic details
		if (string.IsNullOrEmpty(info.HtmlContent.Value))
		{
			var sb = new StringBuilder();
			sb.Append("<div class=\"stacked\">");
			foreach (var detail in GetFileDetails(item))
			{
				sb.Append(detail);
			}
			sb.Append("</div>");
			info.HtmlContent = new MarkupString(sb.ToString());
		}

		return info;
	}

	protected virtual Task<byte[]> DownloadContentAsync(FileExplorerItem item)
	{
		// default assumes path is full path
		return Task.FromResult(Array.Empty<byte>());
	}

	protected virtual List<string> GetFileDetails(FileExplorerItem item)
	{
		return new List<string>
		{
			$"<span class=\"h1\">{Path.GetFileNameWithoutExtension(item.Name)}</span>",
			$"<span class=\"h4\">{Path.GetExtension(item.Name)[1..].ToUpperInvariant()} File</span>",
			$"<span>{item.FileSize:N0} bytes</span>",
			$"<span class=\"text-small text-muted\">Created: {item.DateCreated?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>",
			$"<span class=\"text-small text-muted\">Modified: {item.DateModified?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>"
		};
	}
}
