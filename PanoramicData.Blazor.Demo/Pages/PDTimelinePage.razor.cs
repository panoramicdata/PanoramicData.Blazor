using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDTimelinePage
	{
		private ZoombarValue _zoomPan = new();
		private ZoomBarOptions _zoombarOptions = new()
		{
			ZoomSteps = new double[] { 100, 75, 50, 25 },
			Colours = new ZoomBarColours
			{
				Background = "Silver",
				Border = "Black",
				HandleBackground = "Blue",
				HandleForeground = "Silver"
			}
		};

		private IDataProviderService<TimelineDataPoint> _timelineDataProvder = new DataProvider();
		private TimelineOptions _timelineOptions = new TimelineOptions
		{
			Colours = new TimelineColours
			{
				Background = "White",
				Border = "Silver"
			},
			Series = new[]
			{
				new TimelineSeries
				{
					 Label = "Lines Added",
					 Colour = "Green"
				},
				new TimelineSeries
				{
					 Label = "Lines Changed",
					 Colour = "Orange"
				},
				new TimelineSeries
				{
					 Label = "Lines Deleted",
					 Colour = "Red"
				}
			}
		};

		protected override void OnInitialized()
		{

		}
	}

	public class DataProvider : IDataProviderService<TimelineDataPoint>
	{
		private List<ConfigChange> _data = new();

		public DateTime StartDate { get; set; } = new DateTime(2018, 1, 1);
		public DateTime EndDate { get; set; } = DateTime.Now;
		public TimeSpan DayStart { get; set; } = new TimeSpan(9, 0, 0);
		public TimeSpan DaySuration { get; set; } = new TimeSpan(8, 0, 0);

		public DataProvider()
		{
			GenerateData();
		}

		private void GenerateData()
		{
			// generate data
			_data.Clear();
			var random = new Random(Environment.TickCount);
			var points = 100;
			var startDate = new DateTime(2018, 1, 1);
			var endDate = DateTime.Now;
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

		public Task<DataResponse<TimelineDataPoint>> GetDataAsync(DataRequest<TimelineDataPoint> request, CancellationToken cancellationToken)
		{
			// TODO: group data

			var results = new List<TimelineDataPoint>();
			foreach (var pt in _data)
			{
				results.Add(new TimelineDataPoint(pt.DateChanged, 0, pt.LinesAdded));
				results.Add(new TimelineDataPoint(pt.DateChanged, 1, pt.LinesChanged));
				results.Add(new TimelineDataPoint(pt.DateChanged, 2, pt.LinesDeleted));
			}

			var response = new DataResponse<TimelineDataPoint>(results, results.Count);
			return Task.FromResult(response);
		}

		#region Not Implemented

		public Task<OperationResponse> DeleteAsync(TimelineDataPoint item, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<OperationResponse> UpdateAsync(TimelineDataPoint item, IDictionary<string, object> delta, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<OperationResponse> CreateAsync(TimelineDataPoint item, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	public class ConfigChange
	{
		public DateTime DateChanged { get; set; }
		public int LinesAdded { get; set; }
		public int LinesChanged { get; set; }
		public int LinesDeleted { get; set; }
	}
}
