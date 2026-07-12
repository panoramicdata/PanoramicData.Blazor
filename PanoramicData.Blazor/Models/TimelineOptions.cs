namespace PanoramicData.Blazor.Models;

/// <summary>
/// Root configuration object for a <see cref="PanoramicData.Blazor.PDTimeline"/> component.
/// </summary>
public class TimelineOptions
{
	/// <summary>The default colour used across multiple sub-option defaults.</summary>
	public const string MainColor = "#404040d1";

	/// <summary>Gets or sets bar rendering options.</summary>
	public TimelineBarOptions Bar { get; set; } = new TimelineBarOptions();
	/// <summary>Gets or sets general display and behaviour options.</summary>
	public TimelineGeneralOptions General { get; set; } = new TimelineGeneralOptions();
	/// <summary>Gets or sets the current-time indicator options.</summary>
	public TimelineIndicatorOptions Indicator { get; set; } = new TimelineIndicatorOptions();
	/// <summary>Gets or sets pan region options.</summary>
	public TimelinePanOptions Pan { get; set; } = new TimelinePanOptions();
	/// <summary>Gets or sets selection highlight options.</summary>
	public TimelineSelectionOptions Selection { get; set; } = new TimelineSelectionOptions();
	/// <summary>Gets or sets the array of series definitions.</summary>
	public TimelineSeries[] Series { get; set; } = [];
	/// <summary>Gets or sets loading spinner options.</summary>
	public TimelineSpinnerOptions Spinner { get; set; } = new TimelineSpinnerOptions();
	/// <summary>Gets or sets X-axis (time axis) rendering options.</summary>
	public TimelineXAxisOptions XAxis { get; set; } = new TimelineXAxisOptions();
	/// <summary>Gets or sets Y-axis (value axis) rendering options.</summary>
	public TimelineYAxisOptions YAxis { get; set; } = new TimelineYAxisOptions();
}