namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration options for the time indicator (current-time marker) in a <see cref="PanoramicData.Blazor.PDTimeline"/> component.
/// </summary>
public class TimelineIndicatorOptions
{
	/// <summary>Gets or sets the background colour of the indicator badge. Defaults to <see cref="TimelineOptions.MainColor"/>.</summary>
	public string BackgroundColour { get; set; } = TimelineOptions.MainColor;
	/// <summary>Gets or sets the text colour of the indicator label. Defaults to <c>"whitesmoke"</c>.</summary>
	public string Colour { get; set; } = "whitesmoke";
	/// <summary>Gets or sets the horizontal padding in pixels inside the indicator badge.</summary>
	public int Padding { get; set; } = 5;
	/// <summary>Gets or sets the width in pixels of the indicator vertical line.</summary>
	public int Width { get; set; } = 20;
}
