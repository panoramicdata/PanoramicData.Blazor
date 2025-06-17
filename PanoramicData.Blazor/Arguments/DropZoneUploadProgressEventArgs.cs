namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The DropZoneUploadProgressEventArgs class provides information for PDDropZone upload events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the DropZoneUploadEventArgs class.
/// </remarks>
/// <param name="path">The path where the file is being uploaded.</param>
/// <param name="name">The name of the file being uploaded.</param>
/// <param name="size">The size of the file being uploaded.</param>
public class DropZoneUploadProgressEventArgs(string path, string name, long size, string key, string sessionId, double progress) : DropZoneUploadEventArgs(path, name, size, key, sessionId)
{

	/// <summary>
	/// Gets or sets the
	/// </summary>
	public double Progress { get; set; } = progress;

	/// <summary>
	/// Gets or sets whether the operation should be cancelled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Optional string detailing why the operation is to be cancelled.
	/// </summary>
	public string CancelReason { get; set; } = string.Empty;
}
