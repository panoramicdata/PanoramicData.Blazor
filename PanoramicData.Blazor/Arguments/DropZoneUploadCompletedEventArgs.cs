namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The DropZoneUploadCompletedEventArgs class provides information for PDDropZone upload completed events.
/// </summary>
public class DropZoneUploadCompletedEventArgs : DropZoneUploadEventArgs
{
	/// <summary>
	/// Initializes a new instance of the DropZoneUploadCompletedEventArgs class.
	/// </summary>
	/// <param name="path">The path where the file is being uploaded.</param>
	/// <param name="name">The name of the file being uploaded.</param>
	/// <param name="size">The size of the file being uploaded.</param>
	public DropZoneUploadCompletedEventArgs(string path, string name, long size, string key, string sessionId)
		: base(path, name, size, key, sessionId)
	{
		Success = true;
	}

	/// <summary>
	/// Initializes a new instance of the DropZoneUploadCompletedEventArgs class.
	/// </summary>
	/// <param name="path">The path where the file is being uploaded.</param>
	/// <param name="name">The name of the file being uploaded.</param>
	/// <param name="size">The size of the file being uploaded.</param>
	/// <param name="reason">Reason for the upload failure.</param>
	public DropZoneUploadCompletedEventArgs(string path, string name, long size, string key, string sessionId, string reason)
		: base(path, name, size, key, sessionId)
	{
		Reason = reason;
	}

	/// <summary>
	/// Gets or sets whether the upload was completed successfully or not.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets or sets an optional short message as to why the upload failed.
	/// </summary>
	public string Reason { get; set; } = string.Empty;
}
