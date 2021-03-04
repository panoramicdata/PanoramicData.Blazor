using System.Collections.Generic;

namespace PanoramicData.Blazor
{
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
		public string? Key { get; set; }

		public string GetFullPath(string? rootDir = null)
		{
			var segs = new List<string>();
			segs.AddRange(rootDir.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries));
			segs.AddRange(Path.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries));
			var folderPath = $"/{string.Join("/", segs)}";
			var fullPath = $"{folderPath.TrimEnd('/')}/{Name}";
			return fullPath;
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
}
