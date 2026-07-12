namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration options for the bar rendering area of a <see cref="PanoramicData.Blazor.PDTimeline"/> component.
/// </summary>
public class TimelineBarOptions
{
	/// <summary>Gets or sets the vertical padding in pixels between consecutive bars.</summary>
	public int Padding { get; set; } = 2;
	/// <summary>Gets or sets the height in pixels of each bar.</summary>
	public int Width { get; set; } = 20;
}
