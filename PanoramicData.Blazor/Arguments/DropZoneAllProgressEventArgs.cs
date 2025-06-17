namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The DropZoneAllProgressEventArgs class provides information for PDDropZone batch progress events.
/// </summary>
public class DropZoneAllProgressEventArgs
{
	/// <summary>
	/// Gets or sets the progress (0-100).
	/// </summary>
	public double UploadProgress { get; set; }

	/// <summary>
	/// Gets or sets the total number of bytes to be sent.
	/// </summary>
	public long TotalBytes { get; set; }

	/// <summary>
	/// Gets or sets the total number of bytes sent.
	/// </summary>
	public long TotalBytesSent { get; set; }
}
