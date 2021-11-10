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
		private int _columnOffset;
		private bool _isDragging;
		private double _dragOrigin;
		private double _panHandleWidth;
		private double _panHandleX;
		private double _panHandleDragOrigin;
		private int _totalColumns;
		private int _viewportColumns;
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
		public DataProviderDelegate? DataProvider { get; set; }

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

		private TextInfo GetTextInfo(DateTime dt)
		{
			if (Scale == TimelineScales.Years)
			{
				return new TextInfo
				{
					OffsetX = 4,
					Text = dt.ToString("yy")
				};
			}
			else if (Scale == TimelineScales.Months)
			{
				return new TextInfo
				{
					Skip = 1,
					Text = dt.ToString("MMM yy")
				};
			}
			else if (Scale == TimelineScales.Weeks)
			{
				return new TextInfo
				{
					Skip = 3,
					Text = dt.ToString("dd/MM/yy")
				};
			}
			else if (Scale == TimelineScales.Days)
			{
				return new TextInfo
				{
					Skip = 2,
					Text = dt.ToString("dd/MM/yy")
				};
			}
			else if (Scale == TimelineScales.Hours8)
			{
				return new TextInfo
				{
					Skip = 2,
					Text = dt.ToString("dd/MM/yy")
				};
			}
			else if (Scale == TimelineScales.Hours4)
			{
				return new TextInfo
				{
					Skip = 5,
					Text = dt.ToString("dd/MM/yy")
				};
			}
			else if (Scale == TimelineScales.Hours)
			{
				return new TextInfo
				{
					Skip = 3,
					Text = dt.ToString("dd/MM/yy HH:00")
				};
			}
			else if (Scale == TimelineScales.Minutes)
			{
				return new TextInfo
				{
					Skip = 3,
					Text = dt.ToString("dd/MM/yy HH:mm")
				};
			}
			else
			{
				return new TextInfo
				{
					Skip = 3,
					Text = dt.ToString("dd/MM/yy")
				};
			}
		}

		private DataPoint[] GetViewPortDataPoints()
		{
			var cols = _viewportColumns;
			var points = new DataPoint[cols];
			for (var i = 0; i < points.Length; i++)
			{
				var key = _columnOffset + i;
				if (_dataPoints.ContainsKey(key))
				{
					points[i] = _dataPoints[key];
				}
			}

			// right align if within viewport window
			if(Options.RightAlign)
			{

			}

			return points.ToArray();
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				_objRef = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.timeline.init", Id, Options, _objRef).ConfigureAwait(true);
				_canvasWidth = await JSRuntime.InvokeAsync<int>("panoramicData.getWidth", _svgCanvasElement).ConfigureAwait(true);
				await SetScale(Scale, true);
			}
		}

		private void OnPanMouseDown(MouseEventArgs args)
		{
			if (!_isDragging)
			{
				_isDragging = true;
				_dragOrigin = args.ClientX;
				_panHandleDragOrigin = _panHandleX;
			}
		}

		private void OnPanMouseMove(MouseEventArgs args)
		{
			if (_isDragging)
			{
				var delta = args.ClientX - _dragOrigin;
				_panHandleX = _panHandleDragOrigin + delta;
				if (_panHandleX < 0)
				{
					_panHandleX = 0;
				}
				else if (_panHandleX > (_canvasWidth - _panHandleWidth))
				{
					_panHandleX = _canvasWidth - _panHandleWidth;
				}
				_columnOffset = (int)Math.Floor((_panHandleX / (double)_canvasWidth) * _totalColumns);
			}
		}

		private void OnPanMouseUp(MouseEventArgs args)
		{
			if (_isDragging)
			{
				_isDragging = false;
			}
		}

		private async Task OnMouseWheel(WheelEventArgs args)
		{
			if (args.CtrlKey)
			{
				if (args.DeltaY < 0)
				{
					if (Scale > TimelineScales.Minutes)
					{
						await SetScale(Scale - 1).ConfigureAwait(true);
					}
				}
				else
				{
					if (Scale < TimelineScales.Years)
					{
						await SetScale(Scale + 1).ConfigureAwait(true);
					}
				}
			}
		}

		protected async override Task OnParametersSetAsync()
		{
			await SetScale(Scale).ConfigureAwait(true);
		}

		[JSInvokable("PanoramicData.Blazor.PDTimeline.OnResize")]
		public async Task OnResize()
		{
			_canvasWidth = await JSRuntime.InvokeAsync<int>("panoramicData.getWidth", _svgCanvasElement).ConfigureAwait(true);
			await SetScale(Scale, true).ConfigureAwait(true);
			await InvokeAsync(() => StateHasChanged()).ConfigureAwait(true);
		}

		public async Task RefreshAsync()
		{
			if (DataProvider != null)
			{
				var start = MinDateTime;
				var end = MaxDateTime ?? DateTime.Now;
				var points = await DataProvider(start, end, Scale).ConfigureAwait(true);
				foreach (var point in points)
				{
					if (!_dataPoints.ContainsKey(point.PeriodIndex))
					{
						_dataPoints.Add(point.PeriodIndex, point);
					}
				}
			}
			StateHasChanged();
		}

		public async Task SetScale(TimelineScales scale, bool forceRefresh = false)
		{
			if (scale != _previousScale || forceRefresh)
			{
				if (scale != _previousScale)
				{
					await ScaleChanged.InvokeAsync(scale).ConfigureAwait(true);
				}
				_dataPoints.Clear();
				_previousScale = Scale;
				Scale = scale;
				// calculate total number of columns for scale
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
				_totalColumns = (int)Math.Ceiling(temp);
				// calculate visible columns
				_viewportColumns = (int)Math.Floor(_canvasWidth / (double)Options.Bar.Width);
				// calculate pan handle width - min 5 px
				_panHandleWidth = Math.Min(Math.Max(((double)_viewportColumns / (double)_totalColumns) * _canvasWidth, 5), _canvasWidth);
				if (_canvasWidth > 0 && (_panHandleX + _panHandleWidth > _canvasWidth))
				{
					_panHandleX = _canvasWidth - _panHandleWidth;
				}
				_columnOffset = (int)Math.Floor((_panHandleX / (double)_canvasWidth) * _totalColumns);
				// refresh data for new scale
				await RefreshAsync().ConfigureAwait(true);
				StateHasChanged();
			}
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("panoramicData.timeline.term", Id);
		}

		public class TextInfo
		{
			public int OffsetX { get; set; } = 3;
			public int OffsetY { get; set; } = 14;
			public int Skip { get; set; }
			public string Text { get; set; } = string.Empty;
		}
	}
}
