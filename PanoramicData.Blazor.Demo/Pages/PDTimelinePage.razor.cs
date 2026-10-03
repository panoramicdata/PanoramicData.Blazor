using System.Security.Cryptography;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTimelinePage
{
	private readonly List<ConfigChange> _data = [];
	protected PDTimeline Timeline { get; set; } = null!;
	private readonly TimelinePageModel _model = new();
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
	private TimeRange? _selection;
	protected bool IsEnabled { get; set; } = true;
	private readonly TimelineOptions _timelineOptions = CreateTimelineOptions();
	private DateTime _minDate;
	private DateTime _maxDate;
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

	private void GenerateData(int startYear, int endYear, int points)
	{
		// generate data
		var startDate = new DateTime(startYear, 1, 1);
		var endDate = new DateTime(endYear, 12, 31);
		var dayStart = new TimeSpan(9, 0, 0);
		var dayDuration = new TimeSpan(8, 0, 0);

		var days = endDate.Subtract(startDate).TotalDays;
		var mins = dayDuration.TotalMinutes;

		for (var i = 0; i < points; i++)
		{
			var date = startDate.AddDays(RandomNumberGenerator.GetInt32((int)days + 1)).Add(dayStart).AddMinutes(RandomNumberGenerator.GetInt32((int)mins + 1));
			_data.Add(new ConfigChange
			{
				DateChanged = date,
				// low counts (use ranges of up to 50, 100 and 20 respectively for high counts)
				LinesAdded = RandomNumberGenerator.GetInt32(0, 5),
				LinesChanged = RandomNumberGenerator.GetInt32(0, 5),
				LinesDeleted = RandomNumberGenerator.GetInt32(0, 5),
			});
		}
	}


	private void OnScaleChanged(TimelineScale scale) => _model.Scale = scale;

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

	private void OnSelectionChanged(TimeRange? range) => _selection = range;

	private void OnSelectionChangeEnd() => EventManager?.Add(new Event("SelectionChangeEnd", new EventArgument("start", Timeline.GetSelection()?.StartTime), new EventArgument("end", Timeline.GetSelection()?.EndTime)));

	private async Task OnClearData()
	{
		// update component parameters
		_data.Clear();
		_minDate = DateTime.MinValue;
		_maxDate = DateTime.MinValue;
		if (Timeline is not null)
		{
			await Timeline.Reset().ConfigureAwait(true);
		}
	}

	private async Task OnSetData()
	{
		// generate new data
		_data.Clear();
		// fewer points (use 10,000 points to demonstrate a large data set)
		GenerateData(2015, 2020, 100);

		// update component parameters
		_minDate = _data.Min(x => x.DateChanged);
		_maxDate = _data.Max(x => x.DateChanged);

		if (Timeline is not null)
		{
			await Timeline.RefreshAsync().ConfigureAwait(true);
		}
	}

	private async ValueTask<DataPoint[]> GetTimelineData(DateTime start, DateTime end, TimelineScale scale, CancellationToken cancellationToken)
	{
		// aggregate according to zoom / scale
		try
		{
			var points = new List<DataPoint>();
			var groups = _data.Where(x => x.DateChanged >= start && x.DateChanged <= end)
							  .GroupBy(x => scale.PeriodStart(x.DateChanged))
							  .OrderBy(x => x.Key);
			foreach (var group in groups)
			{
				// sum each series for bucket
				points.Add(new DataPoint
				{
					Count = group.Count(),
					StartTime = group.Key,
					SeriesValues =
					[
						(double)group.Sum(x=> x.LinesDeleted),
						(double)group.Sum(x=> x.LinesChanged),
						(double)group.Sum(x=> x.LinesAdded)
					]
				});
			}

			EventManager?.Add(new Event("GetTimelineData", new EventArgument("start", start), new EventArgument("end", end), new EventArgument("scale", scale)));
			StateHasChanged();

			// add some latency
			await Task.Delay(1000, cancellationToken).ConfigureAwait(true);
			return [.. points];
		}
		catch (TaskCanceledException)
		{
			return [];
		}
		catch (Exception ex)
		{
			Console.WriteLine($"GetTimelineData: Exception: {ex.Message}");
			return [];
		}
	}

	private void OnUpdateMaxDate()
	{
		// generate 1 years more additional data
		var year = _data.Max(x => x.DateChanged.Year) + 1;
		GenerateData(year, year, 10);
		_maxDate = _data.Max(x => x.DateChanged);
	}

	private void OnUpdateMinDate()
	{
		// generate 1 years more previous data
		var year = _data.Min(x => x.DateChanged.Year) - 1;
		GenerateData(year, year, 10);
		_minDate = _data.Min(x => x.DateChanged).Date;
	}

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
		if (Timeline is null)
		{
			return;
		}

		// select last year
		if (Timeline.GetSelection() is null)
		{
			await Timeline.SetSelection(_maxDate.AddYears(-2), _maxDate).ConfigureAwait(true);
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
