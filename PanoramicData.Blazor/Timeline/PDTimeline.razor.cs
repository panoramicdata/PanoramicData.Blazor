using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Timeline
{
	public partial class PDTimeline : IDisposable
	{
		public delegate ValueTask<DataPoint[]> DataProviderDelegate(DateTime start, DateTime end, TimelineScales scale);

		private static int _seq;
		private int _canvasWidth;
		private int _offset;
		private ElementReference _svgCanvasElement;
		private DotNetObjectReference<PDTimeline>? _objRef;
		private TimelineScales _previousScale = TimelineScales.Days;
		private Dictionary<int, DataPoint> _dataPoints = new Dictionary<int, DataPoint>();

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		[Parameter]
		public TimelineScales Scale { get; set; } = TimelineScales.Months;

		[Parameter]
		public EventCallback<TimelineScales> ScaleChanged { get; set; }

		[Parameter]
		public IDataProviderService<TimelineDataPoint>? DataProvider { get; set; }

		[Parameter]
		public DataProviderDelegate? DataProvider2 { get; set; }

		[Parameter]
		public string Id { get; set; } = $"pd-timeline-{++_seq}";

		[Parameter]
		public int Height { get; set; } = 100;

		[Parameter]
		public DateTime? MaxDateTime { get; set; }

		[Parameter]
		public DateTime MinDateTime { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);

		[Parameter]
		public TimelineOptions Options { get; set; } = new TimelineOptions();

		[Parameter]
		public int Width { get; set; } = 400;

		private DataPoint[] DataPoints { get; set; } = Array.Empty<DataPoint>();

		private int TotalPeriods { get; set; }

		public async Task<DataPoint[]> FetchData()
		{
			if (DataProvider is null)
			{
				DataPoints = Array.Empty<DataPoint>();
			}
			else
			{
				// fetch data
				var request = new DataRequest<TimelineDataPoint>();
				var response = await DataProvider.GetDataAsync(request, default).ConfigureAwait(true);

				// aggregate according to zoom / scale
				var index = 0;
				var buckets = new List<DataPoint>();
				var groups = response.Items.GroupBy(x => x.DateTime.PeriodStart(Scale)).OrderBy(x => x.Key);
				DateTime? previousStart = null;
				foreach (var group in groups)
				{
					// increment index
					index += previousStart.HasValue ? group.Key.TotalPeriodsSince(previousStart.Value, Scale) : 0;
					var dp = new DataPoint
					{
						PeriodIndex = index,
						StartTime = group.Key
					};
					// sum each series for bucket
					var values = new List<double>();
					for (var seriesIdx = 0; seriesIdx < Options.Series.Length; seriesIdx++)
					{
						var sum = group.Where(x => x.Series == seriesIdx).Sum(x => x.Value);
						values.Add(sum);
					}
					dp.SeriesValues = values.ToArray();
					buckets.Add(dp);
					previousStart = dp.StartTime;
				}
				DataPoints = buckets.ToArray();

				// calculate max summed values
				//MaxSeriesSum = DataPoints.Max(x => x.SeriesValues.Sum());

				// calculate period in entire time range
				TotalPeriods = 0;
				if (DataPoints.Length > 0)
				{
					var start = DataPoints[0].StartTime;
					var end = DataPoints[DataPoints.Length - 1].StartTime.PeriodEnd(Scale);
					TotalPeriods = end.TotalPeriodsSince(start, Scale);
				}
			}
			return DataPoints;
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				_objRef = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.timeline.init", Id, Options, _objRef).ConfigureAwait(true);
				_canvasWidth = await JSRuntime.InvokeAsync<int>("panoramicData.getWidth", _svgCanvasElement).ConfigureAwait(true);

				await RefreshAsync().ConfigureAwait(true);
			}
		}

		private async Task OnMouseWheel(WheelEventArgs args)
		{
			if (args.CtrlKey)
			{
				bool scaleChanged = false;
				if (args.DeltaY < 0)
				{
					if (Scale > TimelineScales.Minutes)
					{
						Scale--;
						_previousScale = Scale;
						scaleChanged = true;
					}
				}
				else
				{
					if (Scale < TimelineScales.Years)
					{
						Scale++;
						_previousScale = Scale;
						scaleChanged = true;
					}
				}
				if (scaleChanged)
				{
					_dataPoints.Clear();
					await ScaleChanged.InvokeAsync(Scale).ConfigureAwait(true);
					await RefreshAsync().ConfigureAwait(true);
				}
			}
		}

		protected async override Task OnParametersSetAsync()
		{
			if (Scale != _previousScale)
			{
				_dataPoints.Clear();
				_previousScale = Scale;
				await RefreshAsync().ConfigureAwait(true);
			}
		}

		[JSInvokable("PanoramicData.Blazor.PDTimeline.OnResize")]
		public async Task OnResize()
		{
			_canvasWidth = await JSRuntime.InvokeAsync<int>("panoramicData.getWidth", _svgCanvasElement).ConfigureAwait(true);
			await InvokeAsync(() => StateHasChanged()).ConfigureAwait(true);
		}

		private async Task RefreshViewport()
		{
			if (DataProvider2 != null)
			{
				var start = MinDateTime;
				var end = MaxDateTime ?? DateTime.Now;
				var points = await DataProvider2(start, end, Scale).ConfigureAwait(true);
				foreach (var point in points)
				{
					if (!_dataPoints.ContainsKey(point.PeriodIndex))
					{
						_dataPoints.Add(point.PeriodIndex, point);
					}
				}
			}
		}

		public async Task RefreshAsync()
		{
			if (DataProvider != null)
			{
				await FetchData().ConfigureAwait(true);
				await RefreshViewport().ConfigureAwait(true);
				StateHasChanged();
			}
		}

		private string CalculateViewBox()
		{
			var x = 0;
			var y = 0;
			var width = TotalPeriods * Options.Bar.Width;
			var height = 100;
			return $"{x} {y} {width} {height}";
		}

		private int GetViewPortColumnCount()
		{
			return _canvasWidth / Options.Bar.Width;
		}

		private DataPoint[] GetViewPortDataPoints()
		{
			var cols = GetViewPortColumnCount();
			var points = new DataPoint[cols];
			for (var i = 0; i < points.Length; i++)
			{
				var key = _offset + i;
				if (_dataPoints.ContainsKey(key))
				{
					points[i] = _dataPoints[key];
				}
			}
			return points.ToArray();
		}

		private int CalculateLastColumnIndex()
		{
			var start = MinDateTime.Date;
			var end = MaxDateTime?.Date ?? DateTime.Now.Date;
			var temp = Scale switch
			{
				TimelineScales.Minutes => end.Subtract(start).TotalMinutes,
				TimelineScales.Hours => end.Subtract(start).TotalHours,
				TimelineScales.Hours4 => end.Subtract(start).TotalHours / 4,
				TimelineScales.Hours6 => end.Subtract(start).TotalHours / 6,
				TimelineScales.Hours8 => end.Subtract(start).TotalHours / 8,
				TimelineScales.Hours12 => end.Subtract(start).TotalHours / 12,
				TimelineScales.Weeks => end.Subtract(start).TotalDays / 7,
				TimelineScales.Months => end.TotalMonthsSince(start),
				TimelineScales.Years => end.TotalYearsSince(start),
				_ => end.Subtract(start).TotalDays
			};
			return (int)Math.Ceiling(temp);
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("panoramicData.timeline.term", Id);
		}
	}
}
