using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Timeline;
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
		private TimelineOptions _timelineOptions = new TimelineOptions
		{
			Bar = new TimelineBarOptions
			{
				Width = 20
			},
			Colours = new TimelineColours
			{
				Background = "White",
				Border = "Silver"
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
			}
		};
		private DateTime _minDate;
		private DateTime _maxDate;

		private void GenerateData()
		{
			// generate data
			_data.Clear();
			var random = new Random(Environment.TickCount);
			var points = 200;
			var startDate = new DateTime(2020, 1, 1);
			var endDate = new DateTime(2021, 12, 31);
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
			GenerateData();
			_minDate = _data.Min(x => x.DateChanged).Date;
			_maxDate = _data.Max(x => x.DateChanged);
		}

		private void OnSelectionChanged(TimeRange? range)
		{
			_selection = range;
		}

		private ValueTask<DataPoint[]> GetTimelineData(DateTime start, DateTime end, TimelineScales scale)
		{
			// aggregate according to zoom / scale
			var index = 0;
			var points = new List<DataPoint>();
			var groups = _data
							.Where(x => x.DateChanged >= start && x.DateChanged <= end)
							.GroupBy(x => x.DateChanged.PeriodStart(scale))
							.OrderBy(x => x.Key);
			DateTime? previousStart = null;
			foreach (var group in groups)
			{
				// increment index
				index += previousStart.HasValue ? group.Key.TotalPeriodsSince(previousStart.Value, scale) : 0;
				var dp = new DataPoint
				{
					PeriodIndex = index,
					StartTime = group.Key,
					SeriesValues = new[]
					{
						(double)group.Sum(x=> x.LinesDeleted),
						(double)group.Sum(x=> x.LinesChanged),
						(double)group.Sum(x=> x.LinesAdded)
					}
				};
				// sum each series for bucket
				points.Add(dp);
				previousStart = dp.StartTime;
			}
			return ValueTask.FromResult(points.ToArray());
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
		public TimelineScales Scale { get; set; } = TimelineScales.Months;
	}
}
