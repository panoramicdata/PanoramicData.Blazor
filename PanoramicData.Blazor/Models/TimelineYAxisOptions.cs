namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration options for the Y-axis (value axis) of a <see cref="PanoramicData.Blazor.PDTimeline"/> component.
/// </summary>
public class TimelineYAxisOptions
{
	/// <summary>Gets or sets an explicit maximum value for the Y axis. When <c>null</c>, the maximum is derived from the data.</summary>
	public double? MaxValue { get; set; }
}
