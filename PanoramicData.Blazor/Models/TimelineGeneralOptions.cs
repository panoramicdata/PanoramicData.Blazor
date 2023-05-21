namespace PanoramicData.Blazor.Models;

public class TimelineGeneralOptions
{
	public bool AllowDisableSelection { get; set; }

	public bool AutoRefresh { get; set; } = true;

	public string DateFormat { get; set; } = "dd/MM/yy";

	public bool FetchAll { get; set; }

	public bool RestrictZoomOut { get; set; }

	public bool RightAlign { get; set; }

	public TimelineScale[] Scales { get; set; } = new[]
	{
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
	};
}
