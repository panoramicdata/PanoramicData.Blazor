namespace PanoramicData.Blazor
{
	/// <summary>
	/// The DropZoneFile class hold information on a single file that has been dropped onto a PDDropZone instance.
	/// </summary>
	public class DropZoneFile
	{
		public string? Name { get; set; }
		public long Size { get; set; }
	}
}
