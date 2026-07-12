namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines a service that generates file preview information for items displayed in the file explorer.
/// </summary>
public interface IPreviewProvider
{
	/// <summary>Gets or sets the format string used when rendering date/time values in preview content.</summary>
	string DateTimeFormat { get; set; }

	/// <summary>Gets or sets the elapsed time in milliseconds before a loading spinner is displayed while retrieving a preview.</summary>
	int SpinnerTriggerMs { get; set; }

	/// <summary>Gets or sets the minimum duration in milliseconds that the loading spinner is shown, even if the preview loads faster.</summary>
	int SpinnerMinDisplayMs { get; set; }

	/// <summary>
	/// Returns basic (lightweight) preview information for the given file explorer item.
	/// </summary>
	/// <param name="item">The file explorer item to preview, or <c>null</c> to obtain an empty preview.</param>
	/// <param name="spinner">When <c>true</c>, shows a loading spinner if the operation exceeds <see cref="SpinnerTriggerMs"/>.</param>
	/// <returns>A <see cref="PreviewInfo"/> object describing the preview content.</returns>
	Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item, bool spinner = false);

	/// <summary>
	/// Returns full preview information for the given file explorer item, which may include rich content.
	/// </summary>
	/// <param name="item">The file explorer item to preview, or <c>null</c> to obtain an empty preview.</param>
	/// <returns>A <see cref="PreviewInfo"/> object describing the preview content.</returns>
	Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item);
}
