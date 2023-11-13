using Humanizer;

namespace PanoramicData.Blazor.PreviewProviders;

public class FileExplorerPreviewProvider : DefaultPreviewProvider
{
	public PDFileExplorer? FileExplorer { get; set; }

	protected override List<string> GetFileDetails(FileExplorerItem item)
	{
		var details = base.GetFileDetails(item);
		if (FileExplorer != null)
		{
			// add icon
			details.Insert(0, $"<i class=\"fa-4x {FileExplorer.GetIconCssClass(item)}\"></i>");
			var dc = item.DateModified?.ToLocalTime().ToString(FileExplorer.DateFormat, CultureInfo.InvariantCulture);
			details.RemoveAt(3);
			details.Insert(3, $"<span title=\"{item.FileSize:N0} bytes\">{item.FileSize.Bytes().Humanize(FileExplorer.SizeFormat, CultureInfo.InvariantCulture)}</span>");
			details.RemoveAt(4);
			details.Insert(4, $"<span title=\"{item.DateCreated}\" class=\"text-small text-muted\">Created: {dc}</span>");
			var dm = item.DateModified?.ToLocalTime().ToString(FileExplorer.DateFormat, CultureInfo.InvariantCulture);
			details.RemoveAt(5);
			details.Insert(5, $"<span title=\"{item.DateModified}\" class=\"text-small text-muted\">Modified: {dm}</span>");
		}
		return details;
	}
}
