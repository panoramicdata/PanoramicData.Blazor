namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// The DropZoneUploadEventArgs class provides information for PDDropZone upload events.
/// </summary>
/// <remarks>
/// Initializes a new instance of the DropZoneUploadEventArgs class.
/// </remarks>
/// <param name="path">The path where the file is being uploaded.</param>
/// <param name="name">The name of the file being uploaded.</param>
/// <param name="size">The size of the file being uploaded.</param>
/// <param name="key">Unique key for upload.</param>
/// <param name="sessionId">Unique identifier for the session.</param>
public class DropZoneUploadEventArgs(string path, string name, long size, string key, string sessionId)
{

	/// <summary>
	/// Gets or sets the path where the file is being uploaded to.
	/// </summary>
	public string Path { get; set; } = path;

	/// <summary>
	/// Gets or sets the name of the file being uploaded.
	/// </summary>
	public string Name { get; set; } = name;

	/// <summary>
	/// Gets or sets the size of the file being uploaded.
	/// </summary>
	public long Size { get; set; } = size;

	/// <summary>
	/// Gets or sets the unique identifier of the file being uploaded.
	/// </summary>
	public string Key { get; set; } = key;

	/// <summary>
	/// Gets or sets the unique identifier of the session.
	/// </summary>
	public string SessionId { get; set; } = sessionId;

	/// <summary>
	/// Gets or sets additional form fields to be sent with the upload request.
	/// </summary>
	public Dictionary<string, string> FormFields { get; set; } = [];

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
