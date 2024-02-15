namespace PanoramicData.Blazor.Interfaces;

public interface IPreviewProvider
{
	Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item);

	Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item);
}
