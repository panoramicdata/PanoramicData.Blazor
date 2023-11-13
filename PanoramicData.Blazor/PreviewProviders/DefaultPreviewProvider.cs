namespace PanoramicData.Blazor.PreviewProviders;

public class DefaultPreviewProvider : IPreviewProvider
{
	public string DateTimeFormat { get; set; } = "dd/MM/yy HH:mm:ss";

	public Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item)
	{
		var info = new PreviewInfo();

		if (item == null || item.EntryType == FileExplorerItemType.Directory)
		{
			info.HtmlContent = new MarkupString("<span>No Preview</span>");
		}
		else
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

		return Task.FromResult(info);
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
