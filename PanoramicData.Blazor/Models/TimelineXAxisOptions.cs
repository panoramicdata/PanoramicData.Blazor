namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration options for the X-axis (time axis) of a <see cref="PanoramicData.Blazor.PDTimeline"/> component.
/// </summary>
public class TimelineXAxisOptions
{
	/// <summary>Gets or sets the colour of the axis line. Defaults to <c>"Black"</c>.</summary>
	public string Colour { get; set; } = "Black";
	/// <summary>Gets or sets the colour of major tick marks.</summary>
	public string MajorTickColour { get; set; } = "#3e3e3e";
	/// <summary>Gets or sets the colour of minor tick marks.</summary>
	public string MinorTickColour { get; set; } = "#838383";

}
