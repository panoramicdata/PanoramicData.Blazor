namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// The live timeline example, which follows the current time.
/// </summary>
public partial class PDTimelinePage
{
	protected PDTimeline LiveTimeline { get; set; } = null!;
	protected bool LiveFollowNow { get; set; } = true;
	private TimeRange? _liveSelection;
	private readonly DateTime _liveMinDate = DateTime.Now.AddMinutes(-2);
	private readonly TimelineOptions _liveTimelineOptions = new()
	{
		Bar = new TimelineBarOptions
		{
			Width = 36,
			Padding = 3
		},
		General = new TimelineGeneralOptions
		{
			DateFormat = "yyyy-MM-dd",
			RightAlign = true,
			Scales = [TimelineScale.Seconds, TimelineScale.Minutes]
		},
		Series =
		[
			new TimelineSeries
			{
				Label = "Live activity",
				Colour = "#0d6efd"
			}
		],
		Selection = new TimelineSelectionOptions
		{
			Enabled = true,
			CanChangeStart = true,
			CanChangeEnd = true
		}
	};

	private async Task OnLiveTimelineInitializedAsync()
	{
		var end = LiveTimeline.RoundedMaxDateTime;
		await LiveTimeline.SetSelection(end.AddSeconds(-15), end).ConfigureAwait(true);
	}

	private void OnLiveSelectionChanged(TimeRange? selection)
	{
		_liveSelection = selection;
	}

	private static ValueTask<DataPoint[]> GetLiveTimelineData(DateTime start, DateTime end, TimelineScale scale, CancellationToken cancellationToken)
	{
		var now = DateTime.Now;
		var points = new List<DataPoint>();
		for (var pointTime = scale.PeriodStart(start);
			pointTime < end && pointTime <= now && !cancellationToken.IsCancellationRequested;
			pointTime = scale.AddPeriods(pointTime, 1))
		{
			var pulse = 4 + (3 * Math.Sin(pointTime.TimeOfDay.TotalSeconds / 2));
			points.Add(new DataPoint
			{
				StartTime = pointTime,
				Count = 1,
				SeriesValues = [Math.Max(1, pulse)]
			});
		}

		return ValueTask.FromResult(points.ToArray());
	}
}
