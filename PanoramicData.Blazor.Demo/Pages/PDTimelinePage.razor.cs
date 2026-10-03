namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTimelinePage
{
	protected PDTimeline Timeline { get; set; } = null!;
	private readonly TimelinePageModel _model = new();
	private TimeRange? _selection;
	protected bool IsEnabled { get; set; } = true;
	private readonly TimelineOptions _timelineOptions = CreateTimelineOptions();
	protected bool MoreDataAvailable { get; set; }

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private static TimelineOptions CreateTimelineOptions() => new()
	{
		Bar = new TimelineBarOptions
		{
			Width = 20,
			Padding = 2
		},
		General = new TimelineGeneralOptions
		{
			DateFormat = "yyyy-MM-dd",
			RestrictZoomOut = false,
			RightAlign = true,
			Scales = CreateTimelineScales()
		},
		Series = CreateTimelineSeries(),
		Selection = new TimelineSelectionOptions
		{
			Enabled = true,
			CanChangeEnd = true
		},
		Spinner = new TimelineSpinnerOptions
		{
			Width = 10,
			ArcStart = 90,
			ArcEnd = 360
		}
	};

	private static TimelineScale[] CreateTimelineScales() =>
	[
		TimelineScale.Seconds,
		TimelineScale.Minutes,
		TimelineScale.Minutes5,
		new TimelineScale("10 Minutes", TimelineUnits.Minutes, 10),
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

	private static TimelineSeries[] CreateTimelineSeries() =>
	[
		new TimelineSeries
		{
			Label = "Lines Deleted",
			Colour = "Red"
		},
		new TimelineSeries
		{
			Label = "Lines Changed",
			Colour = "Orange"
		},
		new TimelineSeries
		{
			Label = "Lines Added",
			Colour = "Green"
		}
	];

	private void OnScaleChanged(TimelineScale scale) => _model.Scale = scale;

	private void OnSelectionChanged(TimeRange? range) => _selection = range;

	private void OnSelectionChangeEnd() => EventManager?.Add(new Event("SelectionChangeEnd", new EventArgument("start", Timeline.GetSelection()?.StartTime), new EventArgument("end", Timeline.GetSelection()?.EndTime)));

	private async Task OnZoomToEnd()
	{
		if (Timeline is null)
		{
			return;
		}

		await Timeline.ZoomToEndAsync().ConfigureAwait(true);
	}

	private async Task OnZoomTo24h()
	{
		if (Timeline is null)
		{
			return;
		}

		await Timeline.ZoomToAsync(DateTime.Now.AddHours(-24), DateTime.Now, TimelinePositions.End).ConfigureAwait(true);
	}

	private async Task OnRefreshed()
	{
		// select last year
		if (Timeline is { } timeline && timeline.GetSelection() is null)
		{
			await timeline.SetSelection(_maxDate.AddYears(-2), _maxDate).ConfigureAwait(true);
		}
	}

	private static double MyYValueTransform(double value) =>
		Math.Sqrt(value);
}

public class ConfigChange
{
	public DateTime DateChanged { get; set; }
	public int LinesAdded { get; set; }
	public int LinesChanged { get; set; }
	public int LinesDeleted { get; set; }
}

public class TimelinePageModel
{

	public DateTime DisableAfter { get; set; } = new DateTime(2019, 11, 01);

	public DateTime DisableBefore { get; set; } = new DateTime(2016, 11, 01);

	public TimelineScale Scale { get; set; } = TimelineScale.Months;
}
