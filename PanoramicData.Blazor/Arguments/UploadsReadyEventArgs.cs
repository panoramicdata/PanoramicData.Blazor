namespace PanoramicData.Blazor.Arguments;

public class UploadsReadyEventArgs
{
	public bool Cancel { get; set; }

	public bool Overwrite { get; set; }

	public DropZoneFile[] Files { get; set; } = [];

	public DropZoneFile[] FilesToSkip { get; set; } = [];
}
