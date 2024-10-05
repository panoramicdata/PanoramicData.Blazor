namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The DropZoneEventArgs class provides information for PDDropZone drop events.
/// </summary>
public class DropZoneEventArgs
{
	/// <summary>
	/// Initializes a new instance of the MenuItemEventArgs class.
	/// </summary>
	/// <param name="sender">The object that raised the event.</param>
	/// <param name="files">Files dropped onto the zone.</param>
	public DropZoneEventArgs(object sender, DropZoneFile[] files)
	{
		Sender = sender;
		Files = files;
	}

	/// <summary>
	/// Gets the object that raised the event.
	/// </summary>
	public object Sender { get; }

	/// <summary>
	/// Gets the files dropped onto the zone.
	/// </summary>
	public DropZoneFile[] Files { get; }

	/// <summary>
	/// Gets or sets whether the operation should be canceled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Optional string detailing why the operation is to be canceled.
	/// </summary>
	public string CancelReason { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the base folder.
	/// </summary>
	public string BaseFolder { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets application defined state.
	/// </summary>
	public object? State { get; set; }
}

/// <summary>
/// The DropZoneUploadEventArgs class provides information for PDDropZone upload events.
/// </summary>
public class DropZoneUploadEventArgs
{
	/// <summary>
	/// Initializes a new instance of the DropZoneUploadEventArgs class.
	/// </summary>
	/// <param name="path">The path where the file is being uploaded.</param>
	/// <param name="name">The name of the file being uploaded.</param>
	/// <param name="size">The size of the file being uploaded.</param>
	/// <param name="key">Unique key for upload.</param>
	/// <param name="sessionId">Unique identifier for the session.</param>
	public DropZoneUploadEventArgs(string path, string name, long size, string key, string sessionId)
	{
		Key = key;
		Path = path;
		Name = name;
		Size = size;
		SessionId = sessionId;
		FormFields = [];
	}

	/// <summary>
	/// Gets or sets the path where the file is being uploaded to.
	/// </summary>
	public string Path { get; set; }

	/// <summary>
	/// Gets or sets the name of the file being uploaded.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the size of the file being uploaded.
	/// </summary>
	public long Size { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier of the file being uploaded.
	/// </summary>
	public string Key { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier of the session.
	/// </summary>
	public string SessionId { get; set; }

	/// <summary>
	/// Gets or sets additional form fields to be sent with the upload request.
	/// </summary>
	public Dictionary<string, string> FormFields { get; set; }

	/// <summary>
	/// Gets or sets the total number of files in the batch.
	/// </summary>
	public int BatchCount { get; set; }

	/// <summary>
	/// Gets or sets the current file within the batch being uploaded.
	/// </summary>
	public int BatchProgress { get; set; }

	public string FullPath
	{
		get
		{
			return $"{Path.TrimEnd('/')}/{Name.TrimStart('/')}";
		}
	}
}

/// <summary>
/// The DropZoneUploadProgressEventArgs class provides information for PDDropZone upload events.
/// </summary>
public class DropZoneUploadProgressEventArgs : DropZoneUploadEventArgs
{
	/// <summary>
	/// Initializes a new instance of the DropZoneUploadEventArgs class.
	/// </summary>
	/// <param name="path">The path where the file is being uploaded.</param>
	/// <param name="name">The name of the file being uploaded.</param>
	/// <param name="size">The size of the file being uploaded.</param>
	public DropZoneUploadProgressEventArgs(string path, string name, long size, string key, string sessionId, double progress)
		: base(path, name, size, key, sessionId)
	{
		Progress = progress;
	}

	/// <summary>
	/// Gets or sets the
	/// </summary>
	public double Progress { get; set; }

	/// <summary>
	/// Gets or sets whether the operation should be canceled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Optional string detailing why the operation is to be canceled.
	/// </summary>
	public string CancelReason { get; set; } = string.Empty;
}

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
