using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDTimeline : IDisposable
	{
		public delegate ValueTask<DataPoint[]> DataProviderDelegate(DateTime start, DateTime end, TimelineScales scale, CancellationToken cancellationToken);

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
		private TimeRange? _selectionRange = null;
		private ElementReference _svgSelectionHandleStart;
		private ElementReference _svgSelectionHandleEnd;
		private bool _isSelectionStartDragging;
		private bool _isSelectionEndDragging;

		private bool _isPanDragging;
		private double _panDragOrigin;
		private double _panHandleWidth;
		private double _panHandleX;

		private ElementReference _svgPlotElement;
		private ElementReference _svgPanElement;
		private DotNetObjectReference<PDTimeline>? _objRef;
		private TimelineScales _previousScale = TimelineScales.Days;
		private CancellationTokenSource? _refreshCancellationToken;
		private bool _loading;
		private DateTime _lastMinDateTime = new DateTime(DateTime.Now.Year, 1, 1);
		private readonly Dictionary<int, DataPoint> _dataPoints = new Dictionary<int, DataPoint>();

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		[Parameter]
		public bool FetchAll { get; set; } = true;

		[Parameter]
		public bool IsEnabled { get; set; } = true;

		[Parameter]
		public TimelineScales Scale { get; set; } = TimelineScales.Days;

		[Parameter]
		public EventCallback<TimelineScales> ScaleChanged { get; set; }

		[Parameter]
		public EventCallback<TimeRange?> SelectionChanged { get; set; }

		[Parameter]
		public EventCallback SelectionChangeEnd { get; set; }

		[Parameter]
		public DataProviderDelegate? DataProvider { get; set; }

		[Parameter]
		public string Id { get; set; } = $"pd-timeline-{++_seq}";

		[Parameter]
		public DateTime? MaxDateTime { get; set; }

		[Parameter]
		public DateTime MinDateTime { get; set; }

		[Parameter]
		public TimelineOptions Options { get; set; } = new TimelineOptions();

		public void Clear()
		{
			_dataPoints.Clear();
		}

		public async Task ClearSelection()
		{
			await SetSelection(-1, -1).ConfigureAwait(true);
		}

		private int GetColumnIndexAtPoint(double clientX)
		{
			return _columnOffset + (int)Math.Floor((clientX - _canvasX) / Options.Bar.Width);
		}

		private int GetMajorMarkOffsetForViewport()
		{
			var dt = MinDateTime.AddPeriods(Scale, _columnOffset).PeriodStart(Scale);
			var majorTickOffset = 0;
			if (Scale <= TimelineScales.Minutes)
			{
				while (dt.TimeOfDay.Seconds != 0)
				{
					majorTickOffset++;
					dt = dt.AddPeriods(Scale, 1);
				}
			}
			else if (Scale <= TimelineScales.Hours)
			{
				while (dt.TimeOfDay.Minutes != 0)
				{
					majorTickOffset++;
					dt = dt.AddPeriods(Scale, 1);
				}
			}
			else if (Scale < TimelineScales.Weeks)
			{
				while (dt.TimeOfDay.Hours != 0)
				{
					majorTickOffset++;
					dt = dt.AddPeriods(Scale, 1);
				}
			}
			return majorTickOffset;
		}

		public TimeRange? GetSelection()
		{
			return _selectionRange;
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
			var points = new DataPoint[_viewportColumns];
			for (var i = 0; i < _viewportColumns; i++)
			{
				var key = _columnOffset + i;
				if (_dataPoints.ContainsKey(key))
				{
					points[i] = _dataPoints[key];
				}
				else if(!_loading)
				{
					points[i] = new DataPoint { PeriodIndex = key };
				}
			}
			return points.ToArray();
		}

		private void MovePanHandle(double x)
		{
			_panHandleX = x;
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

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				_objRef = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.timeline.init", Id, Options, _objRef).ConfigureAwait(true);
				_canvasHeight = (int)(await JSRuntime.InvokeAsync<double>("panoramicData.getHeight", _svgPlotElement).ConfigureAwait(true));
				_canvasWidth = (int)(await JSRuntime.InvokeAsync<double>("panoramicData.getWidth", _svgPlotElement).ConfigureAwait(true));
				_canvasX = (int)(await JSRuntime.InvokeAsync<double>("panoramicData.getX", _svgPlotElement).ConfigureAwait(true));
				await SetScale(Scale, true);
			}
		}

		private async Task OnChartPointerDown(PointerEventArgs args)
		{
			if (IsEnabled && Options.Selection.Enabled && !_isChartDragging
				&& !_isSelectionStartDragging && !_isSelectionEndDragging
				&& MinDateTime != DateTime.MinValue)
			{
				_isChartDragging = true;
				_chartDragOrigin = args.ClientX;
				await JSRuntime.InvokeVoidAsync("panoramicData.setPointerCapture", args.PointerId, _svgPlotElement).ConfigureAwait(true);
				var index = GetColumnIndexAtPoint(args.ClientX);
				await SetSelection(index, index).ConfigureAwait(true);
			}
		}

		private async Task OnChartPointerMove(PointerEventArgs args)
		{
			if(_isChartDragging)
			{
				_chartDragOrigin = args.ClientX;
				var index = GetColumnIndexAtPoint(args.ClientX);
				await SetSelection(_selectionStartIndex, index).ConfigureAwait(true);
			}
		}

		private async Task OnChartPointerUp(PointerEventArgs args)
		{
			_isChartDragging = false;
			if (MinDateTime != DateTime.MinValue)
			{
				await SelectionChangeEnd.InvokeAsync(null).ConfigureAwait(true);
			}
		}

		private async Task OnPanPointerDown(PointerEventArgs args)
		{
			if (IsEnabled && !_isPanDragging)
			{
				_panDragOrigin = args.ClientX;
				await JSRuntime.InvokeVoidAsync("panoramicData.setPointerCapture", args.PointerId, _svgPanElement).ConfigureAwait(true);
			}
		}

		private void OnPanPointerMove(PointerEventArgs args)
		{
			// initiare a drag operation?
			if (IsEnabled && !_isPanDragging && args.Buttons == 1)
			{
				_isPanDragging = true;
			}
			if (_isPanDragging)
			{
				MovePanHandle(_panHandleX + (args.ClientX - _panDragOrigin));
				_panDragOrigin = args.ClientX;
			}
		}

		private async Task OnPanPointerUp(PointerEventArgs args)
		{
			bool refresh = false;
			if (_isPanDragging)
			{
				_isPanDragging = false;
				refresh = true;
			}
			else
			{
				// move entire viewport along?
				if((args.ClientX - _canvasX) < _panHandleX)
				{
					MovePanHandle(_panHandleX - _panHandleWidth);
					refresh = true;
				}
				else if ((args.ClientX - _canvasX) > (_panHandleX + _panHandleWidth))
				{
					MovePanHandle(_panHandleX + _panHandleWidth);
					refresh = true;
				}
			}
			if (refresh && !FetchAll)
			{
				await RefreshAsync().ConfigureAwait(true);
			}
		}

		private async Task OnSelectionEndPointerDown(PointerEventArgs args)
		{
			if (IsEnabled && Options.Selection.Enabled && !_isChartDragging && !_isSelectionStartDragging && !_isSelectionEndDragging)
			{
				_isSelectionEndDragging = true;
				_chartDragOrigin = args.ClientX;
				await JSRuntime.InvokeVoidAsync("panoramicData.setPointerCapture", args.PointerId, _svgSelectionHandleEnd).ConfigureAwait(true);
			}
		}

		private async Task OnSelectionEndPointerMove(PointerEventArgs args)
		{
			if (_isSelectionEndDragging)
			{
				_chartDragOrigin = args.ClientX;
				var index = GetColumnIndexAtPoint(args.ClientX);
				if (index >= _selectionStartIndex)
				{
					await SetSelection(_selectionStartIndex, index).ConfigureAwait(true);
				}
			}
		}

		private void OnSelectionEndPointerUp(PointerEventArgs args)
		{
			_isSelectionEndDragging = false;
		}

		private async Task OnSelectionStartPointerDown(PointerEventArgs args)
		{
			if (IsEnabled && Options.Selection.Enabled && !_isChartDragging && !_isSelectionStartDragging && !_isSelectionEndDragging)
			{
				_isSelectionStartDragging = true;
				_chartDragOrigin = args.ClientX;
				await JSRuntime.InvokeVoidAsync("panoramicData.setPointerCapture", args.PointerId, _svgSelectionHandleStart).ConfigureAwait(true);
			}
		}

		private async Task OnSelectionStartPointerMove(PointerEventArgs args)
		{
			if (_isSelectionStartDragging)
			{
				_chartDragOrigin = args.ClientX;
				var index = GetColumnIndexAtPoint(args.ClientX);
				if (index <= _selectionEndIndex)
				{
					await SetSelection(index, _selectionEndIndex).ConfigureAwait(true);
				}
			}
		}

		private void OnSelectionStartPointerUp(PointerEventArgs args)
		{
			_isSelectionStartDragging = false;
		}

		private async Task OnMouseWheel(WheelEventArgs args)
		{
			if (IsEnabled && args.CtrlKey)
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
			if (_canvasWidth > 0)
			{
				await SetScale(Scale).ConfigureAwait(true);
			}

			// reset if earliest date changes
			if(MinDateTime != _lastMinDateTime)
			{
				_lastMinDateTime = MinDateTime;
				await Reset().ConfigureAwait(true);
				await SetScale(Scale, true).ConfigureAwait(true);
				await RefreshAsync().ConfigureAwait(true);
			}
		}

		[JSInvokable("PanoramicData.Blazor.PDTimeline.OnResize")]
		public async Task OnResize()
		{
			_canvasWidth = await JSRuntime.InvokeAsync<int>("panoramicData.getWidth", _svgPlotElement).ConfigureAwait(true);
			await SetScale(Scale, true).ConfigureAwait(true);
			await InvokeAsync(() => StateHasChanged()).ConfigureAwait(true);
		}

		public async Task RefreshAsync()
		{
			if (DataProvider != null && MinDateTime != DateTime.MinValue)
			{
				// cancel previous query?
				if(_refreshCancellationToken != null)
				{
					_refreshCancellationToken.Cancel();
					_refreshCancellationToken = null;
				}

				// either fetch all data points for scale, or just the current viewport
				_loading = true;
				var start = FetchAll ? MinDateTime.PeriodStart(Scale) : MinDateTime.AddPeriods(Scale, _columnOffset).PeriodStart(Scale);
				var end = FetchAll ? (MaxDateTime ?? DateTime.Now).PeriodEnd(Scale) : MinDateTime.AddPeriods(Scale, _columnOffset + _viewportColumns).PeriodEnd(Scale);
				_refreshCancellationToken = new CancellationTokenSource();
				var points = await DataProvider(start, end, Scale, _refreshCancellationToken.Token).ConfigureAwait(true);
				foreach (var point in points)
				{
					point.PeriodIndex =  point.StartTime.TotalPeriodsSince(MinDateTime, Scale);
					if (!_dataPoints.ContainsKey(point.PeriodIndex))
					{
						_dataPoints.Add(point.PeriodIndex, point);
					}
				}
				_loading = false;
			}
			StateHasChanged();
		}

		public async Task Reset()
		{
			Clear();
			await ClearSelection().ConfigureAwait(true);
		}

		private double SelectionStartX => (Math.Min(_selectionStartIndex, _selectionEndIndex) - _columnOffset) * Options.Bar.Width;

		private double SelectionEndX => ((Math.Max(_selectionStartIndex, _selectionEndIndex) - _columnOffset) * Options.Bar.Width) + Options.Bar.Width;

		public async Task SetScale(TimelineScales scale, bool forceRefresh = false, DateTime? centerOn = null)
		{
			if (scale != _previousScale || forceRefresh)
			{
				var previousCenter = MinDateTime.PeriodStart(_previousScale).AddPeriods(_previousScale, _columnOffset + (_viewportColumns / 2));
				var scaleChanged = scale != _previousScale;
				var refreshData = (scaleChanged) || !FetchAll;
				_previousScale = scale;
				await ScaleChanged.InvokeAsync(scale).ConfigureAwait(true);
				if (scaleChanged)
				{
					_dataPoints.Clear();
					Scale = scale;
				}
				// calculate total number of columns for scale
				var start = MinDateTime.Date.PeriodStart(Scale);
				var end = (MaxDateTime?.Date ?? DateTime.Now.Date).PeriodEnd(Scale);
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
				if (_canvasWidth > 0)
				{
					_viewportColumns = (int)Math.Floor(_canvasWidth / (double)Options.Bar.Width);
					// calculate pan handle width - min 5 px
					_panHandleWidth = Math.Min(Math.Max(((double)_viewportColumns / (double)_totalColumns) * _canvasWidth, 5), _canvasWidth);
					if (_canvasWidth > 0 && (_panHandleX + _panHandleWidth > _canvasWidth))
					{
						_panHandleX = _canvasWidth - _panHandleWidth;
					}
					_columnOffset = (int)Math.Floor((_panHandleX / (double)_canvasWidth) * _totalColumns);
				}
				// re-calc selection in new scale
				if (_selectionRange != null)
				{
					await SetSelection(_selectionRange.StartTime, _selectionRange.EndTime).ConfigureAwait(true);
				}
				// re-position viewport?
				CenterOn(centerOn ?? previousCenter);
				// refresh data for new scale?
				await RefreshAsync().ConfigureAwait(true);
				// mark state as changed
				StateHasChanged();
			}
		}

		private void CenterOn(DateTime dateTime)
		{
			if(dateTime < MinDateTime || dateTime > (MaxDateTime ?? DateTime.Now))
			{
				var totalSeconds = (MaxDateTime ?? DateTime.Now).Subtract(MinDateTime).TotalSeconds;
				dateTime = MinDateTime.AddSeconds(totalSeconds / 2);
			}
			var maxDate = (MaxDateTime ?? DateTime.Now).PeriodEnd(Scale);


			var maxOffset = maxDate.TotalPeriodsSince(MinDateTime.PeriodStart(Scale), Scale) - _viewportColumns;
			if(maxOffset <= 0)
			{
				_columnOffset = 0;
			}
			else
			{
				// validate
				var newOffset = dateTime.TotalPeriodsSince(MinDateTime.PeriodStart(Scale), Scale) - (_viewportColumns / 2);
				if (newOffset >= 0 && newOffset <= maxOffset)
				{
					Console.WriteLine($"Old Offset = {_columnOffset}, New Offset = {newOffset}, TotalColumns = {_totalColumns}");
					_columnOffset = newOffset;
				}
			}

			// update pan handle x
			_panHandleX = (_columnOffset / (double)_totalColumns) * (double)_canvasWidth;
		}

		public async Task SetSelection(DateTime start, DateTime end)
		{
			// calculate start index
			if (start < MinDateTime)
			{
				start = MinDateTime.PeriodStart(Scale);
			}
			var startIndex = start.TotalPeriodsSince(MinDateTime.PeriodStart(Scale), Scale);
			if(startIndex < 0)
			{
				startIndex = 0;
			}

			// calculate end index
			if (end > (MaxDateTime ?? DateTime.Now).PeriodEnd(Scale))
			{
				end = (MaxDateTime ?? DateTime.Now).PeriodEnd(Scale);
			}
			var endIndex = end.TotalPeriodsSince(MinDateTime.PeriodStart(Scale), Scale) - 1;
			if(endIndex >= _totalColumns)
			{
				endIndex = _totalColumns - 1;
			}
			await SetSelection(startIndex, endIndex).ConfigureAwait(true);
		}

		private async Task SetSelection(int startIndex, int endIndex)
		{
			_selectionStartIndex = startIndex;
			_selectionEndIndex = endIndex;
			if(startIndex > -1 && endIndex > -1)
			{
				_selectionRange = new TimeRange
				{
					StartTime = MinDateTime.AddPeriods(Scale, startIndex).PeriodStart(Scale),
					EndTime = MinDateTime.AddPeriods(Scale, endIndex).PeriodEnd(Scale)
				};
			}
			else
			{
				_selectionRange = null;
			}
			await SelectionChanged.InvokeAsync(_selectionRange).ConfigureAwait(true);
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("panoramicData.timeline.term", Id);
		}

		public static class Utilities
		{
			public static string DescribeArc(double x, double y, double radius, double startAngle, double endAngle)
			{
				var sp = PolarToCartesian(x, y, radius, endAngle);
				var ep = PolarToCartesian(x, y, radius, startAngle);
				var arcSweep = endAngle - startAngle <= 180 ? "0" : "1";
				var d = string.Join(" ", new[] {
				"M", sp.x.ToString("0.00"), sp.y.ToString("0.00"),
				"A", radius.ToString(), radius.ToString(), "0", arcSweep, "0", ep.x.ToString("0.00"), ep.y.ToString("0.00")
				});
				return d;
			}

			public static (double x, double y) PolarToCartesian(double centerX, double centerY, double radius, double angleInDegrees)
			{
				var angleInRadians = angleInDegrees * Math.PI / 180.0;
				var x = centerX + radius * Math.Cos(angleInRadians);
				var y = centerY + radius * Math.Sin(angleInRadians);
				return (x, y);
			}
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
