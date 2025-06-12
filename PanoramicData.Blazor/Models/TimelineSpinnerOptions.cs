namespace PanoramicData.Blazor.Models;

public class TimelineSpinnerOptions
{
	public int ArcStart { get; set; } = 15;
	public int ArcEnd { get; set; } = 345;
	public string Colour { get; set; } = TimelineOptions.MainColor;
	public int Width { get; set; } = 6;
}
