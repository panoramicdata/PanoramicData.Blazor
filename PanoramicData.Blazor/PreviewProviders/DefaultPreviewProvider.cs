using Humanizer;
using Markdig;

namespace PanoramicData.Blazor.PreviewProviders;

/// <summary>
/// Default implementation of <see cref="IPreviewProvider"/> used by file explorer previews.
/// </summary>
public partial class DefaultPreviewProvider : IPreviewProvider
{
	private static readonly string[] _downloadableFileTypes = ["html", "htm", "url", "md", "txt"];

	/// <summary>
	/// Gets or sets date-time format used in preview metadata.
	/// </summary>
	public string DateTimeFormat { get; set; } = "dd/MM/yy HH:mm:ss";

	/// <summary>
	/// Gets or sets delay in milliseconds before the loading spinner is shown.
	/// </summary>
	public int SpinnerTriggerMs { get; set; } = 500;

	/// <summary>
	/// Gets or sets minimum display time in milliseconds once the spinner is shown.
	/// </summary>
	public int SpinnerMinDisplayMs { get; set; } = 1000;

	#region IPreviewProvider

	/// <summary>
	/// Builds preview information for a file explorer item.
	/// </summary>
	/// <param name="item">Item to preview.</param>
	/// <returns>Preview metadata and content.</returns>
	public virtual async Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item)
	{
		if (item == null || (item.EntryType == FileExplorerItemType.Directory && item.Name == ".."))
		{
			return new PreviewInfo
			{
				HtmlContent = new MarkupString("<span class=\"user-select-none\">No Preview</span>"),
				CssClass = "basic"
			};
		}

		// download content for better preview?
		if (item.EntryType == FileExplorerItemType.File && _downloadableFileTypes.Contains(item.FileExtension))
		{
			var contentBytes = await DownloadContentAsync(item);
			if (contentBytes.Length > 0 && GetContentPreview(item.FileExtension, Encoding.UTF8.GetString(contentBytes)) is { } contentPreview)
			{
				return contentPreview;
			}
		}

		// fallback to basic info
		return await GetBasicPreviewInfoAsync(item);
	}

	/// <summary>
	/// Builds a basic metadata-only preview, without a spinner.
	/// </summary>
	/// <param name="item">Item to preview.</param>
	/// <returns>Preview metadata and content.</returns>
	public Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item)
		=> GetBasicPreviewInfoAsync(item, false);

	/// <summary>
	/// Builds a basic metadata-only preview.
	/// </summary>
	/// <param name="item">Item to preview.</param>
	/// <param name="spinner">True to include spinner HTML.</param>
	/// <returns>Preview metadata and content.</returns>
	public virtual Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item, bool spinner)
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

	/// <summary>
	/// Gets spinner HTML used while loading preview content.
	/// </summary>
	/// <returns>Spinner markup.</returns>
	public virtual string GetSpinnerHtml()
	{
		return "<i class=\"mt-2 fas fa-2x fa-fw fa-spin fa-spinner \" />";
	}

	#endregion

	/// <summary>
	/// Downloads raw bytes for a file to support richer previews.
	/// </summary>
	/// <param name="item">File item to download.</param>
	/// <returns>File bytes.</returns>
	protected virtual Task<byte[]> DownloadContentAsync(FileExplorerItem item)
	{
		// default assumes path is full path
		return Task.FromResult(Array.Empty<byte>());
	}

	/// <summary>
	/// Builds metadata rows shown in the basic preview.
	/// </summary>
	/// <param name="item">Item to describe.</param>
	/// <returns>HTML detail rows.</returns>
	protected virtual List<string> GetFileDetails(FileExplorerItem item)
	{
		if (item.EntryType == FileExplorerItemType.Directory)
		{
			return
			[
				$"<span class=\"h1 user-select-none\">{Path.GetFileNameWithoutExtension(item.Name)}</span>",
				"<span class=\"h4 user-select-none\">Folder</span>",
				$"<span class=\"text-small text-muted user-select-none\">Created: {item.DateCreated?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>",
				$"<span class=\"text-small text-muted user-select-none\">Modified: {item.DateModified?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>"
			];
		}

		return
		[
			$"<span class=\"h1 user-select-none\">{Path.GetFileNameWithoutExtension(item.Name)}</span>",
			$"<span class=\"h4 user-select-none\">{Path.GetExtension(item.Name)[1..].ToUpperInvariant()} File</span>",
			$"<span class=\"user-select-none\" title=\"{item.FileSize:N0} bytes\">{item.FileSize.Bytes().Humanize(CultureInfo.InvariantCulture)}</span>",
			$"<span class=\"text-small text-muted user-select-none\">Created: {item.DateCreated?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>",
			$"<span class=\"text-small text-muted user-select-none\">Modified: {item.DateModified?.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}</span>"
		];
	}

	/// <summary>
	/// Builds the preview of a downloaded file's content, or returns null when the content does not give one.
	/// </summary>
	private static PreviewInfo? GetContentPreview(string fileExtension, string content) => fileExtension switch
	{
		"html" or "htm" => new PreviewInfo { HtmlContent = new MarkupString(content), CssClass = "html" },
		"url" => GetUrlPreview(content),
		"md" => new PreviewInfo { HtmlContent = new MarkupString(Markdown.ToHtml(content)), CssClass = "md" },

		// txt, the only other downloadable type
		_ => new PreviewInfo { HtmlContent = new MarkupString(content), CssClass = "txt" }
	};

	private static PreviewInfo? GetUrlPreview(string content)
	{
		var match = UrlRegex().Match(content);
		return match.Success ? new PreviewInfo { Url = match.Groups[1].Value, CssClass = "url" } : null;
	}

	[GeneratedRegex("URL=(.+)\r?")]
	private static partial Regex UrlRegex();
}
