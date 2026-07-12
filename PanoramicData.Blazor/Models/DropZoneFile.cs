namespace PanoramicData.Blazor.Models;

/// <summary>
/// The DropZoneFile class hold information on a single file that has been dropped onto a PDDropZone instance.
/// </summary>
public class DropZoneFile
{
	/// <summary>Gets or sets the path component of the file, inherited from <see cref="DropZoneFile"/>.</summary>
	public string? Path { get; set; }
	/// <summary>Gets or sets the file name.</summary>
	public string? Name { get; set; }
	/// <summary>Gets or sets the file size in bytes.</summary>
	public long Size { get; set; }
	/// <summary>Gets or sets a value indicating whether this file should be skipped during the upload batch.</summary>
	public bool Skip { get; set; }
	/// <summary>Gets or sets an optional new name to use when saving the file at the destination.</summary>
	public string? NewName { get; set; }
	/// <summary>Gets or sets the unique upload key assigned to this file.</summary>
	public string Key { get; set; } = string.Empty;
	/// <summary>Gets or sets the unique session identifier for this upload.</summary>
	public string SessionId { get; set; } = string.Empty;

	/// <summary>
	/// Returns the full destination path, combining the upload path and file name.
	/// </summary>
	/// <returns>The combined path string.</returns>
	public string GetFullPath() => GetFullPath(null);

	/// <summary>
	/// Returns the full destination path, optionally prepending a root directory, then combining the upload path and file name.
	/// </summary>
	/// <param name="rootDir">Optional root directory to prepend to the path segments.</param>
	/// <returns>The combined path string.</returns>
	public string GetFullPath(string? rootDir)
	{
		var segs = new List<string>();
		if (rootDir != null)
		{
			segs.AddRange(rootDir.Split(['/'], StringSplitOptions.RemoveEmptyEntries));
		}

		if (Path != null)
		{
			segs.AddRange(Path.Split(['/'], StringSplitOptions.RemoveEmptyEntries));
		}

		var folderPath = $"/{string.Join("/", segs)}";
		return $"{folderPath.TrimEnd('/')}/{Name}";
	}
}

/// <summary>
/// Holds information about the upload outcome of a single file.
/// </summary>
public class DropZoneFileUploadOutcome : DropZoneFile
{
	/// <summary>Gets or sets a value indicating whether the upload succeeded.</summary>
	public bool Success { get; set; }
	/// <summary>Gets or sets the HTTP status code returned by the upload endpoint.</summary>
	public int StatusCode { get; set; }
	/// <summary>Gets or sets a human-readable reason phrase describing the outcome.</summary>
	public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Holds upload progress information for a single file within a batch upload.
/// </summary>
public class DropZoneFileUploadProgress : DropZoneFile
{
	/// <summary>Gets or sets the upload progress as a fraction between 0.0 (not started) and 1.0 (complete).</summary>
	public double Progress { get; set; }
}
