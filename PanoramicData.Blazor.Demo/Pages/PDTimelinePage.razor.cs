namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDTimelinePage
{
	private readonly List<ConfigChange> _data = [];
	private PDTimeline _timeline = null!;
	private readonly TimelinePageModel _model = new();
	private TimeRange? _selection;
	private bool _isEnabled = true;
	private readonly TimelineOptions _timelineOptions = new()
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
			Scales =
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
			]
		},
		Series =
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
		],
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
	private DateTime _minDate;
	private DateTime _maxDate;
	private bool _moreDataAvailable;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private void GenerateData(int startYear = 2015, int endYear = 2020, int points = 10000)
	{
		// generate data
		var random = new Random(System.Environment.TickCount);
		var startDate = new DateTime(startYear, 1, 1);
		var endDate = new DateTime(endYear, 12, 31);
		var dayStart = new TimeSpan(9, 0, 0);
		var dayDuration = new TimeSpan(8, 0, 0);

		var days = endDate.Subtract(startDate).TotalDays;
		var mins = dayDuration.TotalMinutes;

		for (var i = 0; i < points; i++)
		{
			var date = startDate.AddDays(random.Next((int)days + 1)).Add(dayStart).AddMinutes(random.Next((int)mins + 1));
			_data.Add(new ConfigChange
			{
				DateChanged = date,
				// high counts
				//LinesAdded = random.Next(0, 50),
				//LinesChanged = random.Next(20, 100),
				//LinesDeleted = random.Next(0, 20),
				// low counts
				LinesAdded = random.Next(0, 5),
				LinesChanged = random.Next(0, 5),
				LinesDeleted = random.Next(0, 5),
			});
		}
	}

	protected override void OnInitialized()
	{
	}

	private void OnScaleChanged(TimelineScale scale) => _model.Scale = scale;

	private void OnSelectionChanged(TimeRange? range) => _selection = range;

	private void OnSelectionChangeEnd() => EventManager?.Add(new Event("SelectionChangeEnd", new EventArgument("start", _timeline.GetSelection()?.StartTime), new EventArgument("end", _timeline.GetSelection()?.EndTime)));

	private async Task OnClearData()
	{
		// update component parameters
		_data.Clear();
		_minDate = DateTime.MinValue;
		_maxDate = DateTime.MinValue;
		if (_timeline is not null)
		{
			await _timeline.Reset().ConfigureAwait(true);
		}
	}

	private async Task OnSetData()
	{
		// generate new data
		_data.Clear();
		// lots of points
		//GenerateData(2015, 2020, 10000);
		// fewer points
		GenerateData(2015, 2020, 100);

		// update component parameters
		_minDate = _data.Min(x => x.DateChanged);
		_maxDate = _data.Max(x => x.DateChanged);

		if (_timeline is not null)
		{
			await _timeline.RefreshAsync().ConfigureAwait(true);
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
		if (_timeline is null)
		{
			return;
		}

		await _timeline.ZoomToEndAsync().ConfigureAwait(true);
	}

	private async Task OnZoomTo24h()
	{
		if (_timeline is null)
		{
			return;
		}

		await _timeline.ZoomToAsync(DateTime.Now.AddHours(-24), DateTime.Now, TimelinePositions.End).ConfigureAwait(true);
	}

	private async Task OnRefreshed()
	{
		if (_timeline is null)
		{
			return;
		}

		// select last year
		if (_timeline.GetSelection() is null)
		{
			await _timeline.SetSelection(_maxDate.AddYears(-2), _maxDate).ConfigureAwait(true);
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
