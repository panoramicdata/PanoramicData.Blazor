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

		private int _canvasHeight;
		private int _canvasWidth;
		private int _canvasX;
		private int _columnOffset;
		private int _totalColumns;
		private int _viewportColumns;

		private bool _isChartDragging;
		private double _chartDragOrigin;
		private int _selectionStartIndex = -1;
		private int _selectionEndIndex = -1;

		private bool _isPanDragging;
		private double _panDragOrigin;
		private double _panHandleWidth;
		private double _panHandleX;

		private ElementReference _svgCanvasElement;
		private DotNetObjectReference<PDTimeline>? _objRef;
		private TimelineScales _previousScale = TimelineScales.Days;
		private readonly Dictionary<int, DataPoint> _dataPoints = new Dictionary<int, DataPoint>();

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		[Parameter]
		public TimelineScales Scale { get; set; } = TimelineScales.Months;

		[Parameter]
		public EventCallback<TimelineScales> ScaleChanged { get; set; }

		[Parameter]
		public EventCallback<TimeRange?> SelectionChanged { get; set; }

		[Parameter]
		public DataProviderDelegate? DataProvider { get; set; }

		[Parameter]
		public string Id { get; set; } = $"pd-timeline-{++_seq}";

		[Parameter]
		public DateTime? MaxDateTime { get; set; }

		[Parameter]
		public DateTime MinDateTime { get; set; } = new DateTime(DateTime.Now.Year, 1, 1);

		[Parameter]
		public TimelineOptions Options { get; set; } = new TimelineOptions();

		public async Task ClearSelection()
		{
			await SetSelection(-1, -1).ConfigureAwait(true);
		}

		private int GetColumnIndexAtPoint(double clientX)
		{
			return _columnOffset + (int)Math.Floor((clientX - _canvasX) / Options.Bar.Width);
		}

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
			else if (Scale == TimelineScales.Hours12)
			{
				return new TextInfo
				{
					Skip = 3,
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

		private static string GetTitleDateFormat(TimelineScales scale)
		{
			return scale switch
			{
				TimelineScales.Years => "yyyy",
				TimelineScales.Months => "MMM yyyy",
				TimelineScales.Weeks => "dd/MM/yy",
				TimelineScales.Days => "dd/MM/yy",
				TimelineScales.Hours12 => "dd/MM/yy HH:00",
				TimelineScales.Hours8 => "dd/MM/yy HH:00",
				TimelineScales.Hours6 => "dd/MM/yy HH:00",
				TimelineScales.Hours4 => "dd/MM/yy HH:00",
				TimelineScales.Hours => "dd/MM/yy HH:00",
				TimelineScales.Minutes => "dd/MM/yy HH:mm",
				_ => "dd/MM/yy"
			};
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
			return points.ToArray();
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				_objRef = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.timeline.init", Id, Options, _objRef).ConfigureAwait(true);
				_canvasHeight = await JSRuntime.InvokeAsync<int>("panoramicData.getHeight", _svgCanvasElement).ConfigureAwait(true);
				_canvasWidth = await JSRuntime.InvokeAsync<int>("panoramicData.getWidth", _svgCanvasElement).ConfigureAwait(true);
				_canvasX = await JSRuntime.InvokeAsync<int>("panoramicData.getX", _svgCanvasElement).ConfigureAwait(true);
				await SetScale(Scale, true);
			}
		}

		private async Task OnChartMouseDown(MouseEventArgs args)
		{
			if (!_isChartDragging)
			{
				// initialize drag
				_isChartDragging = true;
				_chartDragOrigin = args.ClientX;

				// initialize selection
				var index = GetColumnIndexAtPoint(args.ClientX);
				await SetSelection(index, index).ConfigureAwait(true);
			}
		}

		private async Task OnChartMouseMove(MouseEventArgs args)
		{
			if(_isChartDragging)
			{
				var delta = args.ClientX - _chartDragOrigin;
				_chartDragOrigin = args.ClientX;

				// calculate column
				var index = GetColumnIndexAtPoint(args.ClientX);
				await SetSelection(_selectionStartIndex, index).ConfigureAwait(true);
			}
		}

		private void OnChartMouseUp(MouseEventArgs args)
		{
			if (_isChartDragging)
			{
				_isChartDragging = false;
			}
		}

		private void OnPanMouseDown(MouseEventArgs args)
		{
			if (!_isPanDragging)
			{
				_isPanDragging = true;
				_panDragOrigin = args.ClientX;
			}
		}

		private void OnPanMouseMove(MouseEventArgs args)
		{
			if (_isPanDragging)
			{
				var delta = args.ClientX - _panDragOrigin;
				_panHandleX += delta;
				_panDragOrigin = args.ClientX;
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
			if (_isPanDragging)
			{
				_isPanDragging = false;
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
				// clear selection
				await ClearSelection().ConfigureAwait(true);
				// refresh data for new scale
				await RefreshAsync().ConfigureAwait(true);
				StateHasChanged();
			}
		}

		private async Task SetSelection(int startIndex, int endIndex)
		{
			_selectionStartIndex = startIndex;
			_selectionEndIndex = endIndex;
			TimeRange? range = null;
			if(startIndex > -1 && endIndex > -1)
			{
				range = new TimeRange
				{
					StartTime = MinDateTime.AddPeriods(Scale, startIndex).PeriodStart(Scale),
					EndTime = MinDateTime.AddPeriods(Scale, endIndex).PeriodEnd(Scale)
				};
			}
			await SelectionChanged.InvokeAsync(range).ConfigureAwait(true);
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
