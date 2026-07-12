namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// Provides arguments raised when a batch of files is ready to be uploaded, allowing the operation to be cancelled or overwrite behaviour to be configured.
/// </summary>
public class UploadsReadyEventArgs
{
	/// <summary>
	/// Gets or sets a value indicating whether the upload batch should be cancelled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether existing files at the destination should be overwritten.
	/// </summary>
	public bool Overwrite { get; set; }

	/// <summary>
	/// Gets or sets the files to be uploaded.
	/// </summary>
	public DropZoneFile[] Files { get; set; } = [];

	/// <summary>
	/// Gets or sets files in the batch that should be skipped and not uploaded.
	/// </summary>
	public DropZoneFile[] FilesToSkip { get; set; } = [];
}
