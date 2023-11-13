namespace PanoramicData.Blazor.Interfaces;

public interface IPreviewProvider
{
	Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item);
}
