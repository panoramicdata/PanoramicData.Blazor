using Markdig;

namespace PanoramicData.Blazor.PreviewProviders;

public class DefaultPreviewProvider : IPreviewProvider
{
	private static readonly string[] _downloadableFileTypes = ["html", "htm", "url", "md", "txt"];

	public string DateTimeFormat { get; set; } = "dd/MM/yy HH:mm:ss";

	public int SpinnerTriggerMs { get; set; } = 500;

	public int SpinnerMinDisplayMs { get; set; } = 1000;

	#region IPreviewProvider

	public virtual async Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item)
	{
		if (item == null || (item.EntryType == FileExplorerItemType.Directory && item.Name == ".."))
		{
			return new PreviewInfo
			{
				HtmlContent = new MarkupString("<span>No Preview</span>"),
				CssClass = "basic"
			};
		}
		else if (item.EntryType == FileExplorerItemType.File)
		{
			// download content for better preview?
			if (_downloadableFileTypes.Contains(item.FileExtension))
			{
				// download bytes
				var contentBytes = await DownloadContentAsync(item);
				if (contentBytes.Length > 0)
				{
					// convert to string and process
					var contentString = Encoding.UTF8.GetString(contentBytes);
					if (item.FileExtension == "html" || item.FileExtension == "htm")
					{
						return new PreviewInfo
						{
							HtmlContent = new MarkupString(contentString),
							CssClass = "html"
						};
					}
					else if (item.FileExtension == "url")
					{
						var match = Regex.Match(contentString, "URL=(.+)\r?");
						if (match.Success && match.Groups.Count > 1)
						{
							return new PreviewInfo
							{
								Url = match.Groups[1].Value,
								CssClass = "url"
							};
						}
					}
					else if (item.FileExtension == "md")
					{
						return new PreviewInfo
						{
							HtmlContent = new MarkupString(Markdown.ToHtml(contentString)),
							CssClass = "md"
						};
					}
					else if (item.FileExtension == "txt")
					{
						return new PreviewInfo
						{
							HtmlContent = new MarkupString(contentString),
							CssClass = "txt"
						};
					}
				}
			}
		}

		// fallback to basic info
		return await GetBasicPreviewInfoAsync(item);
	}

	public virtual Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item, bool spinner = false)
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

		if (spinner)
		{
			sb.Append(GetSpinnerHtml());
		}

		sb.Append("</div>");
		info.HtmlContent = new MarkupString(sb.ToString());
		info.CssClass = "basic";
		return Task.FromResult(info);
	}

	public virtual string GetSpinnerHtml()
	{
		return "<i class=\"mt-2 fas fa-2x fa-fw fa-spin fa-spinner \" />";
	}

	#endregion

	protected virtual Task<byte[]> DownloadContentAsync(FileExplorerItem item)
	{
		// default assumes path is full path
		return Task.FromResult(Array.Empty<byte>());
	}

	protected virtual List<string> GetFileDetails(FileExplorerItem item)
	{
		if (item.EntryType == FileExplorerItemType.Directory)
		{
			return
			[
				$"<span class=\"h1\">{Path.GetFileNameWithoutExtension(item.Name)}</span>",
				"<span class=\"h4\">Folder</span>",
				$"<span class=\"text-small text-muted\">Created: {item.DateCreated?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>",
				$"<span class=\"text-small text-muted\">Modified: {item.DateModified?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>"
			];
		}

		return
		[
			$"<span class=\"h1\">{Path.GetFileNameWithoutExtension(item.Name)}</span>",
			$"<span class=\"h4\">{Path.GetExtension(item.Name)[1..].ToUpperInvariant()} File</span>",
			$"<span>{item.FileSize:N0} bytes</span>",
			$"<span class=\"text-small text-muted\">Created: {item.DateCreated?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>",
			$"<span class=\"text-small text-muted\">Modified: {item.DateModified?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>"
		];
	}
}
