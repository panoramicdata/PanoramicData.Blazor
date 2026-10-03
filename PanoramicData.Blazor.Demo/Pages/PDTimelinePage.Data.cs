using System.Security.Cryptography;

namespace PanoramicData.Blazor.Demo.Pages;

/// <summary>
/// Generating, loading and extending the example config change data.
/// </summary>
public partial class PDTimelinePage
{
	private readonly List<ConfigChange> _data = [];

	private DateTime _minDate;
	private DateTime _maxDate;

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
}
