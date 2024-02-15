using Markdig;

namespace PanoramicData.Blazor.PreviewProviders;

public class DefaultPreviewProvider : IPreviewProvider
{
	public string DateTimeFormat { get; set; } = "dd/MM/yy HH:mm:ss";

	public virtual async Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item)
	{
		PreviewInfo? info = null;

		if (item == null || item.EntryType == FileExplorerItemType.Directory)
		{
			info = new PreviewInfo
			{
				HtmlContent = new MarkupString("<span>No Preview</span>"),
				CssClass = "basic"
			};
		}
		else if (item.FileExtension == "html" || item.FileExtension == "htm")
		{
			var contentBytes = await DownloadContentAsync(item);
			if (contentBytes.Length > 0)
			{
				var contentString = Encoding.UTF8.GetString(contentBytes);
				info = new PreviewInfo
				{
					HtmlContent = new MarkupString(contentString),
					CssClass = "html"
				};
			}
		}
		else if (item.FileExtension == "url")
		{
			var contentBytes = await DownloadContentAsync(item);
			if (contentBytes.Length > 0)
			{
				var contentString = Encoding.UTF8.GetString(contentBytes);
				var match = Regex.Match(contentString, "URL=(.+)\r?");
				if (match.Success && match.Groups.Count > 1)
				{
					info = new PreviewInfo
					{
						Url = match.Groups[1].Value,
						CssClass = "url"
					};
				}
			}
		}
		else if (item.FileExtension == "md")
		{
			// download content and convert markdown to html
			var contentBytes = await DownloadContentAsync(item);
			if (contentBytes.Length > 0)
			{
				string contentString = Encoding.UTF8.GetString(contentBytes);
				var result = Markdown.ToHtml(contentString);
				info = new PreviewInfo
				{
					HtmlContent = new MarkupString(result),
					CssClass = "md"
				};
			}
		}
		else if (item.FileExtension == "txt")
		{
			// download content and convert markdown to html
			throw new Exception("Aaaaargh crash!!!!");
			var contentBytes = await DownloadContentAsync(item);
			if (contentBytes.Length > 0)
			{
				string contentString = Encoding.UTF8.GetString(contentBytes);
				info = new PreviewInfo
				{
					HtmlContent = new MarkupString(contentString),
					CssClass = "txt"
				};
			}
		}

		// fallback to basic info
		info ??= await GetBasicPreviewInfoAsync(item);

		return info;
	}

	public virtual Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item)
	{
		var info = new PreviewInfo();
		var sb = new StringBuilder();
		sb.Append("<div class=\"stacked\">");
		if (item != null)
		{
			foreach (var detail in GetFileDetails(item))
			{
				sb.Append(detail);
			}
		}
		sb.Append("</div>");
		info.HtmlContent = new MarkupString(sb.ToString());
		info.CssClass = "basic";
		return Task.FromResult(info);
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
