namespace PanoramicData.Blazor.Models;

/// <summary>
/// Options for the loading spinner shown in timeline views.
/// </summary>
public class TimelineSpinnerOptions
{
	/// <summary>
	/// Gets or sets the starting angle of the spinner arc.
	/// </summary>
	public int ArcStart { get; set; } = 15;
	/// <summary>
	/// Gets or sets the ending angle of the spinner arc.
	/// </summary>
	public int ArcEnd { get; set; } = 345;
	/// <summary>
	/// Gets or sets the spinner colour.
	/// </summary>
	public string Colour { get; set; } = TimelineOptions.MainColor;
	/// <summary>
	/// Gets or sets the spinner stroke width.
	/// </summary>
	public int Width { get; set; } = 6;
}
