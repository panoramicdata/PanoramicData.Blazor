﻿using PanoramicData.Blazor.Extensions;
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
		private string _dataCalls = string.Empty;

		private void GenerateData()
		{
			// generate data
			_data.Clear();
			var random = new Random(Environment.TickCount);
			var points = 10000;
			var startDate = new DateTime(2016, 1, 1);
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

		private void OnScaleChanged(TimelineScales scale)
		{
			_model.Scale = scale;
		}

		private void OnSelectionChanged(TimeRange? range)
		{
			_selection = range;
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

				_dataCalls += $"<br/>{start:g} - {end:g}, count: {points.Count}";
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
