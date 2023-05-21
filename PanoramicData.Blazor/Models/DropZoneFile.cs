namespace PanoramicData.Blazor.Models;

/// <summary>
/// The DropZoneFile class hold information on a single file that has been dropped onto a PDDropZone instance.
/// </summary>
public class DropZoneFile
{
	public string? Path { get; set; }
	public string? Name { get; set; }
	public long Size { get; set; }
	public bool Skip { get; set; }
	public string? NewName { get; set; }
	public string Key { get; set; } = string.Empty;
	public string SessionId { get; set; } = string.Empty;

	public string GetFullPath(string? rootDir = null)
	{
		var segs = new List<string>();
		if (rootDir != null)
		{
			segs.AddRange(rootDir.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries));
		}

		if (Path != null)
		{
			segs.AddRange(Path.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries));
		}

		var folderPath = $"/{string.Join("/", segs)}";
		return $"{folderPath.TrimEnd('/')}/{Name}";
	}
}

/// <summary>
/// The DropZoneFileUploadOutcome class holds information about the upload outcome of a single file.
/// </summary>
public class DropZoneFileUploadOutcome : DropZoneFile
{
	public bool Success { get; set; }
	public int StatusCode { get; set; }
	public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// The DropZoneFileUploadOutcome class holds information about the upload outcome of a single file.
/// </summary>
public class DropZoneFileUploadProgress : DropZoneFile
{
	public double Progress { get; set; }
}
