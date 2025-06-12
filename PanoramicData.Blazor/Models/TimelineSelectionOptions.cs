namespace PanoramicData.Blazor.Models;

public class TimelineSelectionOptions
{
	public string BackgroundColour { get; set; } = "#04a0b563";
	public string BorderColour { get; set; } = "#04a0b5";
	public bool CanChangeEnd { get; set; } = true;
	public bool CanChangeStart { get; set; } = true;
	public bool Enabled { get; set; } = true;
	public string HandleColour { get; set; } = "#7878789e";
	public int HandleWidth { get; set; } = 4;

}
