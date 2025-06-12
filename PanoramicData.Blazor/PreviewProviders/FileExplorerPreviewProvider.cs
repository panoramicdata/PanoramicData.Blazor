using Humanizer;
using System.Net.Http;

namespace PanoramicData.Blazor.PreviewProviders;

public class FileExplorerPreviewProvider : DefaultPreviewProvider
{
	public PDFileExplorer? FileExplorer { get; set; }

	protected override async Task<byte[]> DownloadContentAsync(FileExplorerItem item)
	{
		if (FileExplorer == null)
		{
			return await base.DownloadContentAsync(item);
		}

		// skip if preview panel is not shown
		if (!FileExplorer.PreviewPanelVisible)
		{
			return [];
		}

		// simple download of content
		var url = FileExplorer.DownloadUrlFunc(item);
		if (url?.Contains(':') == true)
		{
			int index = url
				.Select((c, i) => new { Character = c, Index = i })
				.Where(x => x.Character == ':')
				.Select(x => x.Index)
				.ElementAtOrDefault(1);
			url = url[++index..];
		}

		using var httpClient = new HttpClient();
		var bytes = await httpClient.GetByteArrayAsync(url);
		return bytes;
	}

	protected override List<string> GetFileDetails(FileExplorerItem item)
	{
		if (FileExplorer is null)
		{
			return base.GetFileDetails(item);
		}
		else
		{
			var dc = item.DateModified?.ToLocalTime().ToString(FileExplorer.DateFormat, CultureInfo.InvariantCulture);
			var dm = item.DateModified?.ToLocalTime().ToString(FileExplorer.DateFormat, CultureInfo.InvariantCulture);
			var details = new List<string>
			{
				$"<i class=\"fa-4x {FileExplorer.GetIconCssClass(item)}\"></i>"
			};
			if (item.EntryType == FileExplorerItemType.Directory)
			{
				details.Add($"<span class=\"h1\">{item.Name}</span>");
				details.Add("<span class=\"h4\">Folder</span>");
			}
			else
			{
				details.Add($"<span class=\"h1\">{Path.GetFileNameWithoutExtension(item.Name)}</span>");
				details.Add($"<span class=\"h4\">{Path.GetExtension(item.Name)[1..].ToUpperInvariant()} File</span>");
				details.Add($"<span title=\"{item.FileSize:N0} bytes\">{item.FileSize.Bytes().Humanize( CultureInfo.InvariantCulture)}</span>");
			}

			details.Add($"<span title=\"{item.DateCreated}\" class=\"text-small text-muted\">Created: {dc}</span>");
			details.Add($"<span title=\"{item.DateModified}\" class=\"text-small text-muted\">Modified: {dm}</span>");
			return details;
		}
	}
}
