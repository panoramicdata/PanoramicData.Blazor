namespace PanoramicData.Blazor.Interfaces;

public interface IPreviewProvider
{
	string DateTimeFormat { get; set; }

	int SpinnerTriggerMs { get; set; }

	int SpinnerMinDisplayMs { get; set; }

	Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item, bool spinner = false);

	Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item);
}
