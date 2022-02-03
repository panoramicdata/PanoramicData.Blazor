using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDTimelinePage
	{
		private List<ConfigChange> _data = new();
		private PDTimeline _timeline = null!;
		private TimelinePageModel _model = new TimelinePageModel();
		private TimeRange? _selection;
		private bool _isEnabled = true;
		private TimelineOptions _timelineOptions = new TimelineOptions
		{
			Bar = new TimelineBarOptions
			{
				Width = 20,
				Padding = 2
			},
			Series = new[]
			{
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

		private void GenerateData(int startYear = 2015, int endYear = 2020, int points = 5000)
		{
			// generate data
			var random = new Random(Environment.TickCount);
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
					LinesAdded = random.Next(0, 50),
					LinesChanged = random.Next(20, 100),
					LinesDeleted = random.Next(0, 20),
				});
			}
		}

		protected override void OnInitialized()
		{
		}

		private void OnScaleChanged(TimelineScales scale)
		{
			_model.Scale = scale;
		}

		private void OnSelectionChanged(TimeRange? range)
		{
			_selection = range;
		}

		private void OnSelectionChangeEnd()
		{
			EventManager?.Add(new Event("SelectionChangeEnd", new EventArgument("start", _timeline.GetSelection()?.StartTime), new EventArgument("end", _timeline.GetSelection()?.EndTime)));
		}

		private async Task OnClearData()
		{
			// update component parameters
			_data.Clear();
			_minDate = DateTime.MinValue;
			_maxDate = DateTime.MinValue;
			await _timeline.Reset().ConfigureAwait(true);
		}

		private async Task OnSetData()
		{
			// generate new data
			_data.Clear();
			GenerateData(2020, 2020, 10);

			// update component parameters
			_minDate = _data.Min(x => x.DateChanged).Date;
			_maxDate = _data.Max(x => x.DateChanged);

			await _timeline.RefreshAsync().ConfigureAwait(true);
		}

		private async ValueTask<DataPoint[]> GetTimelineData(DateTime start, DateTime end, TimelineScales scale, CancellationToken cancellationToken)
		{
			// aggregate according to zoom / scale
			try
			{
				var points = new List<DataPoint>();
				var groups = _data.Where(x => x.DateChanged >= start && x.DateChanged <= end)
								  .GroupBy(x => x.DateChanged.PeriodStart(scale))
								  .OrderBy(x => x.Key);
				foreach (var group in groups)
				{
					// sum each series for bucket
					points.Add(new DataPoint
					{
						StartTime = group.Key,
						SeriesValues = new[]
						{
							(double)group.Sum(x=> x.LinesDeleted),
							(double)group.Sum(x=> x.LinesChanged),
							(double)group.Sum(x=> x.LinesAdded)
						}
					});
				}

				EventManager?.Add(new Event("GetTimelineData", new EventArgument("start", start), new EventArgument("end", end), new EventArgument("scale", scale)));
				StateHasChanged();

				// add some latency
				await Task.Delay(1000, cancellationToken).ConfigureAwait(true);
				return points.ToArray();
			}
			catch(Exception ex)
			{
				Console.WriteLine($"GetTimelineData: Exception: {ex.Message}");
				return Array.Empty<DataPoint>();
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

	public class ConfigChange
	{
		public DateTime DateChanged { get; set; }
		public int LinesAdded { get; set; }
		public int LinesChanged { get; set; }
		public int LinesDeleted { get; set; }
	}

	public class TimelinePageModel
	{

		public DateTime DisableAfter { get; set; }

		public DateTime DisableBefore { get; set; }

		public TimelineScales Scale { get; set; } = TimelineScales.Months;
	}
}
