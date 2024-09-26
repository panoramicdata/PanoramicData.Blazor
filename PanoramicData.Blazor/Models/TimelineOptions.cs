namespace PanoramicData.Blazor.Models;

public class TimelineOptions
{
	public const string MainColor = "#404040d1";

	public TimelineBarOptions Bar { get; set; } = new TimelineBarOptions();
	public TimelineGeneralOptions General { get; set; } = new TimelineGeneralOptions();
	public TimelineIndicatorOptions Indicator { get; set; } = new TimelineIndicatorOptions();
	public TimelinePanOptions Pan { get; set; } = new TimelinePanOptions();
	public TimelineSelectionOptions Selection { get; set; } = new TimelineSelectionOptions();
	public TimelineSeries[] Series { get; set; } = [];
	public TimelineSpinnerOptions Spinner { get; set; } = new TimelineSpinnerOptions();
	public TimelineXAxisOptions XAxis { get; set; } = new TimelineXAxisOptions();
	public TimelineYAxisOptions YAxis { get; set; } = new TimelineYAxisOptions();
}