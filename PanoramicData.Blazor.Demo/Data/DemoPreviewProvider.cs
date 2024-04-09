using PanoramicData.Blazor.PreviewProviders;

namespace PanoramicData.Blazor.Demo.Data;

public class DemoPreviewProvider : FileExplorerPreviewProvider
{
	protected override async Task<byte[]> DownloadContentAsync(FileExplorerItem item)
	{
		await Task.Delay(5000);

		return await base.DownloadContentAsync(item);
	}
}
