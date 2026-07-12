namespace PanoramicData.Blazor.Models;

/// <summary>
/// General display and behaviour options for the <see cref="PanoramicData.Blazor.PDTimeline"/> component.
/// </summary>
public class TimelineGeneralOptions
{
	/// <summary>Gets or sets a value indicating whether the user can disable the current selection by clicking on empty space.</summary>
	public bool AllowDisableSelection { get; set; }

	/// <summary>Gets or sets a value indicating whether the timeline automatically refreshes data when the view range changes. Defaults to <c>true</c>.</summary>
	public bool AutoRefresh { get; set; } = true;

	/// <summary>Gets or sets the date format string used for axis labels. Defaults to <c>"dd/MM/yy"</c>.</summary>
	public string DateFormat { get; set; } = "dd/MM/yy";

	/// <summary>Gets or sets a value indicating whether all data should be fetched regardless of the current view range.</summary>
	public bool FetchAll { get; set; }

	/// <summary>Gets or sets a value indicating whether zooming out beyond the initial range is prevented.</summary>
	public bool RestrictZoomOut { get; set; }

	/// <summary>Gets or sets a value indicating whether bar labels are right-aligned inside their segments.</summary>
	public bool RightAlign { get; set; }

	/// <summary>Gets or sets the available zoom scale steps.</summary>
	public TimelineScale[] Scales { get; set; } =
	[
		TimelineScale.Seconds,
		TimelineScale.Minutes,
		TimelineScale.Hours,
		TimelineScale.Hours4,
		TimelineScale.Hours6,
		TimelineScale.Hours8,
		TimelineScale.Hours12,
		TimelineScale.Days,
		TimelineScale.Weeks,
		TimelineScale.Months,
		TimelineScale.Years
	];
}
