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
	}

	/// <summary>
	/// The DropZoneFileUploadOutcome class holds information about the upload outcome of a single file.
	/// </summary>
	public class DropZoneFileUploadOutcome : DropZoneFile
	{
		public bool Success { get; set; }
		public int StatusCode { get; set; }
	}

	/// <summary>
	/// The DropZoneFileUploadOutcome class holds information about the upload outcome of a single file.
	/// </summary>
	public class DropZoneFileUploadProgress : DropZoneFile
	{
		public double Progress { get; set; }
	}
}
