using PanoramicData.Blazor.PreviewProviders;

namespace PanoramicData.Blazor.Demo.Data;

public class DemoPreviewProvider : FileExplorerPreviewProvider
{
	protected override async Task<byte[]> DownloadContentAsync(FileExplorerItem item)
	{
		if (item?.FileExtension == "url")
		{
			await Task.Delay(1000);
		}

		if (item is null)
		{
			return [];
		}

		return await base.DownloadContentAsync(item);
	}
}
