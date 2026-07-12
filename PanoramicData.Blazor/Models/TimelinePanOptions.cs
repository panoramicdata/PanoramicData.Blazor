namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration options for the pan region of a <see cref="PanoramicData.Blazor.PDTimeline"/> component.
/// </summary>
public class TimelinePanOptions
{
	/// <summary>Gets or sets the border colour of the pan handle. Defaults to <c>"#91919142"</c>.</summary>
	public string BorderColour { get; set; } = "#91919142";
	/// <summary>Gets or sets the border width in pixels of the pan handle.</summary>
	public int BorderWidth { get; set; } = 5;
	/// <summary>Gets or sets the fill colour of the pan region. Defaults to <see cref="TimelineOptions.MainColor"/>.</summary>
	public string Colour { get; set; } = TimelineOptions.MainColor;
	/// <summary>Gets or sets the height in pixels of the pan region.</summary>
	public int Height { get; set; } = 20;
}
