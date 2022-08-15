namespace PanoramicData.Blazor;

public partial class PDTimeline : IDisposable
{
	public delegate ValueTask<DataPoint[]> DataProviderDelegate(DateTime start, DateTime end, TimelineScale scale, CancellationToken cancellationToken);

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
	private int _lastSelectionStartIndex;
	private int _lastSelectionEndIndex;
	private TimeRange? _selectionRange = null;
	private ElementReference _svgSelectionHandleStart;
	private ElementReference _svgSelectionHandleEnd;
	private bool _isSelectionStartDragging;
	private bool _isSelectionEndDragging;

	private bool _isPanDragging;
	private double _panDragOrigin;
	private double _panHandleWidth;
	private double _panHandleX;

	private IJSObjectReference? _module;
	private ElementReference _svgPlotElement;
	private ElementReference _svgPanElement;
	private DotNetObjectReference<PDTimeline>? _objRef;
	private TimelineScale _previousScale = TimelineScale.Years;
	private CancellationTokenSource? _refreshCancellationToken;
	private bool _loading;
	private DateTime _lastMinDateTime;
	private DateTime? _lastMaxDateTime;
	private IJSObjectReference? _commonModule;
	private readonly Dictionary<int, DataPoint> _dataPoints = new Dictionary<int, DataPoint>();

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	[Parameter]
	public DateTime DisableAfter { get; set; }

	[Parameter]
	public DateTime DisableBefore { get; set; }

	[Parameter]
	public EventCallback Initialized { get; set; }

	[Parameter]
	public bool IsEnabled { get; set; } = true;

	[Parameter]
	public TimelineScale Scale { get; set; } = TimelineScale.Years;

	[Parameter]
	public EventCallback<TimelineScale> ScaleChanged { get; set; }

	[Parameter]
	public EventCallback Refreshed { get; set; }

	[Parameter]
	public EventCallback<TimeRange?> SelectionChanged { get; set; }

	[Parameter]
	public EventCallback SelectionChangeEnd { get; set; }

	[Parameter]
	public DataProviderDelegate? DataProvider { get; set; }

	[Parameter]
	public string Id { get; set; } = $"pd-timeline-{++_seq}";

	[Parameter]
	public bool NewMaxDateTimeAvailable { get; set; }

	[Parameter]
	public bool NewMinDateTimeAvailable { get; set; }

	[Parameter]
	public DateTime? MaxDateTime { get; set; }

	[Parameter]
	public DateTime MinDateTime { get; set; }

	[Parameter]
	public TimelineOptions Options { get; set; } = new TimelineOptions();

	[Parameter]
	public EventCallback UpdateMaxDate { get; set; }

	[Parameter]
	public EventCallback UpdateMinDate { get; set; }

	[Parameter]
	public Func<double, double> YValueTransform { get; set; } = (v) => v;


	public bool CanZoomIn()
	{
		if (IsEnabled)
		{
			var scale = Options.General.Scales.FirstOrDefault(x => x.Name == Scale.Name);
			if (scale != null)
			{
				var idx = Options.General.Scales.ToList().IndexOf(scale);
				if (idx > 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool CanZoomOut()
	{
		if (IsEnabled)
		{
			var scale = Options.General.Scales.FirstOrDefault(x => x.Name == Scale.Name);
			if (scale != null)
			{
				var idx = Options.General.Scales.ToList().IndexOf(scale);
				if (idx < Options.General.Scales.Length - 1)
				{
					if (!Options.General.RestrictZoomOut)
					{
						return true;
					}
					// calculate total number of columns for new scale
					var newScale = Options.General.Scales[idx + 1];
					var totalColumns = newScale.PeriodsBetween(RoundedMinDateTime, RoundedMaxDateTime);
					var viewportColumns = _canvasWidth > 0 ? (int)Math.Floor(_canvasWidth / (double)Options.Bar.Width) : 0;
					return viewportColumns > 0 && viewportColumns <= totalColumns;
				}
			}
		}
		return false;
	}

	public async Task Clear()
	{
		_dataPoints.Clear();
		await ClearSelection().ConfigureAwait(true);
	}

	public async Task ClearSelection()
	{
		if (_selectionRange != null)
		{
			_selectionRange = null;
			_selectionStartIndex = _selectionEndIndex = -1;
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	public void Dispose()
	{
		if (_module != null)
		{
			_module.InvokeVoidAsync("dispose", Id);
			_module.DisposeAsync();
		}
	}

	private int GetColumnIndexAtPoint(double clientX)
	{
		var index = _columnOffset + (int)Math.Floor((clientX - _canvasX) / Options.Bar.Width);
		return index < 0 ? 0 : index;
	}

	public TimelineScale? GetScaleToFit(DateTime? date1 = null, DateTime? date2 = null)
	{
		if (date1 is null)
		{
			date1 = RoundedMinDateTime;
		}
		if (date2 is null)
		{
			date2 = RoundedMaxDateTime;
		}
		var viewportColumns = _canvasWidth > 0 ? (int)Math.Floor(_canvasWidth / (double)Options.Bar.Width) : 0;
		for (var i = 0; i < Options.General.Scales.Length - 1; i++)
		{
			var newScale = Options.General.Scales[i];
			var totalColumns = newScale.PeriodsBetween(date1.Value, date2.Value);
			if (totalColumns > 0 && totalColumns <= viewportColumns)
			{
				return newScale;
			}
		}
		return Options.General.Scales.FirstOrDefault();
	}

	public TimeRange? GetSelection()
	{
		return _selectionRange;
	}

	private double GetMaxValue(DataPoint[] points)
	{
		double max = 0;

		var tempArray = points.Where(x => x != null && x.SeriesValues.Length > 0).ToArray();
		if (tempArray.Any())
		{
			max = tempArray.Max(x => x.SeriesValues.Sum(y => YValueTransform(y)));
		}

		return max;
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
			else if (!_loading)
			{
				points[i] = new DataPoint
				{
					PeriodIndex = key,
					StartTime = Scale.AddPeriods(RoundedMinDateTime, key)
				};
			}
		}
		return points.ToArray();
	}

	private bool IsPointEnabled(DataPoint point)
	{
		if (point is null
			|| !IsEnabled
			|| (DisableAfter != DateTime.MinValue && Scale.PeriodEnd(point.StartTime) > DisableAfter)
			|| (DisableBefore != DateTime.MinValue && point.StartTime < DisableBefore))
		{
			return false;
		}
		return true;
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
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDTimeline.razor.js").ConfigureAwait(true);
			if (_module != null)
			{
				await _module.InvokeVoidAsync("initialize", Id, Options, _objRef).ConfigureAwait(true);
			}
			_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/js/common.js");
			if (_commonModule != null)
			{
				_canvasHeight = (int)(await _commonModule.InvokeAsync<double>("getHeight", _svgPlotElement).ConfigureAwait(true));
				_canvasWidth = (int)(await _commonModule.InvokeAsync<double>("getWidth", _svgPlotElement).ConfigureAwait(true));
				_canvasX = (int)(await _commonModule.InvokeAsync<double>("getX", _svgPlotElement).ConfigureAwait(true));
			}
			if (Options.General.AutoRefresh)
			{
				await SetScale(Scale, true);
			}
			// notify app
			await Initialized.InvokeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnChartPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && Options.Selection.Enabled && !_isChartDragging
			&& !_isSelectionStartDragging && !_isSelectionEndDragging
			&& MinDateTime != DateTime.MinValue)
		{
			// check start time is enabled
			var index = GetColumnIndexAtPoint(args.ClientX);
			var startTime = Scale.AddPeriods(RoundedMinDateTime, index);
			if ((DisableBefore != DateTime.MinValue && startTime < DisableBefore)
				|| (DisableAfter != DateTime.MinValue && startTime >= DisableAfter))
			{
				return;
			}

			_isChartDragging = true;
			_chartDragOrigin = args.ClientX;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgPlotElement).ConfigureAwait(true);
			}
			await SetSelectionFromDrag(index, index).ConfigureAwait(true);
		}
	}

	private async Task OnChartPointerMove(PointerEventArgs args)
	{
		if (_isChartDragging)
		{
			_chartDragOrigin = args.ClientX;
			var index = GetColumnIndexAtPoint(args.ClientX);
			await SetSelectionFromDrag(_selectionStartIndex, index).ConfigureAwait(true);
		}
	}

	private async Task OnChartPointerUp(PointerEventArgs args)
	{
		if (_isChartDragging)
		{
			_isChartDragging = false;
			if (MinDateTime != DateTime.MinValue)
			{
				await OnSelectionChangeEnd().ConfigureAwait(true);
			}
		}
	}

	private async Task OnMouseWheel(WheelEventArgs args)
	{
		if (IsEnabled && args.CtrlKey)
		{
			var index = Array.FindIndex(Options.General.Scales, x => x.Name == Scale.Name);
			if (args.DeltaY < 0)
			{
				// zoom in
				if (index > 0)
				{
					await SetScale(Options.General.Scales[index - 1]).ConfigureAwait(true);
				}
			}
			else
			{
				// zoom out
				if (index < Options.General.Scales.Length - 1)
				{
					await SetScale(Options.General.Scales[index + 1]).ConfigureAwait(true);
				}
			}
		}
	}

	private async Task OnPanPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && !_isPanDragging)
		{
			_panDragOrigin = args.ClientX;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgPanElement).ConfigureAwait(true);
			}
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
			if ((args.ClientX - _canvasX) < _panHandleX)
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
		if (refresh && !Options.General.FetchAll)
		{
			await RefreshAsync().ConfigureAwait(true);
		}
	}

	protected async override Task OnParametersSetAsync()
	{
		if (Options.General.AutoRefresh)
		{
			if (_canvasWidth > 0)
			{
				await SetScale(Scale).ConfigureAwait(true);
			}

			// reset if earliest date changes
			if (MinDateTime != _lastMinDateTime)
			{
				_lastMinDateTime = MinDateTime;
				await Reset().ConfigureAwait(true);
				await SetScale(Scale, true).ConfigureAwait(true);
			}

			// reset if latest date changes
			if (MaxDateTime != _lastMaxDateTime)
			{
				_lastMaxDateTime = MaxDateTime;
				await Reset().ConfigureAwait(true);
				await SetScale(Scale, true).ConfigureAwait(true);
			}
		}
	}

	[JSInvokable("PanoramicData.Blazor.PDTimeline.OnResize")]
	public async Task OnResize()
	{
		if (_commonModule != null)
		{
			_canvasWidth = await _commonModule.InvokeAsync<int>("getWidth", _svgPlotElement).ConfigureAwait(true);
		}
		await SetScale(Scale, true).ConfigureAwait(true);
		await InvokeAsync(() => StateHasChanged()).ConfigureAwait(true);
	}

	private async Task OnSelectionChangeEnd()
	{
		// suppress event if selection not changed
		if (_selectionStartIndex != _lastSelectionStartIndex || _selectionEndIndex != _lastSelectionEndIndex)
		{
			_lastSelectionStartIndex = _selectionStartIndex;
			_lastSelectionEndIndex = _selectionEndIndex;
			await SelectionChangeEnd.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	private async Task OnSelectionEndPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && Options.Selection.CanChangeEnd && !_isChartDragging && !_isSelectionStartDragging && !_isSelectionEndDragging)
		{
			_isSelectionEndDragging = true;
			_lastSelectionStartIndex = _selectionStartIndex;
			_lastSelectionEndIndex = _selectionEndIndex;
			_chartDragOrigin = args.ClientX;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgSelectionHandleEnd).ConfigureAwait(true);
			}
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
				await SetSelectionFromDrag(_selectionStartIndex, index).ConfigureAwait(true);
			}
		}
	}

	private async Task OnSelectionEndPointerUp(PointerEventArgs args)
	{
		if (_isSelectionEndDragging)
		{
			_isSelectionEndDragging = false;
			if (MinDateTime != DateTime.MinValue)
			{
				await OnSelectionChangeEnd().ConfigureAwait(true);
			}
		}
	}

	private async Task OnSelectionStartPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && Options.Selection.CanChangeStart && !_isChartDragging && !_isSelectionStartDragging && !_isSelectionEndDragging)
		{
			_isSelectionStartDragging = true;
			_lastSelectionStartIndex = _selectionStartIndex;
			_lastSelectionEndIndex = _selectionEndIndex;
			_chartDragOrigin = args.ClientX;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgSelectionHandleStart).ConfigureAwait(true);
			}
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
				await SetSelectionFromDrag(index, _selectionEndIndex).ConfigureAwait(true);
			}
		}
	}

	private async Task OnSelectionStartPointerUp(PointerEventArgs args)
	{
		if (_isSelectionStartDragging)
		{
			_isSelectionStartDragging = false;
			if (MinDateTime != DateTime.MinValue)
			{
				await OnSelectionChangeEnd().ConfigureAwait(true);
			}
		}
	}

	public void PanTo(DateTime dateTime, TimelinePositions position = TimelinePositions.Center)
	{
		if (dateTime < MinDateTime || dateTime > (MaxDateTime ?? DateTime.Now))
		{
			dateTime = MaxDateTime ?? DateTime.Now;
		}
		var maxOffset = Scale.PeriodsBetween(RoundedMinDateTime, RoundedMaxDateTime) - _viewportColumns;
		if (maxOffset <= 0)
		{
			_columnOffset = 0;
		}
		else
		{
			var newOffset = 0;
			if (position == TimelinePositions.Center)
			{
				newOffset = Scale.PeriodsBetween(RoundedMinDateTime, dateTime) - (_viewportColumns / 2);
			}
			else if (position == TimelinePositions.End)
			{
				newOffset = Scale.PeriodsBetween(RoundedMinDateTime, Scale.PeriodEnd(dateTime)) - _viewportColumns;
			}
			if (newOffset >= 0 && newOffset <= maxOffset)
			{
				//Console.WriteLine($"Old Offset = {_columnOffset}, New Offset = {newOffset}, TotalColumns = {_totalColumns}");
				_columnOffset = newOffset;
			}
		}

		// update pan handle x
		_panHandleX = (_columnOffset / (double)_totalColumns) * (double)_canvasWidth;
	}

	public async Task RefreshAsync()
	{
		if (DataProvider != null && MinDateTime != DateTime.MinValue)
		{
			// cancel previous query?
			if (_refreshCancellationToken != null)
			{
				_refreshCancellationToken.Cancel();
				_refreshCancellationToken = null;
			}

			// either fetch all data points for scale, or just the current viewport
			_loading = true;
			var start = Options.General.FetchAll ? RoundedMinDateTime : Scale.AddPeriods(RoundedMinDateTime, _columnOffset);
			var end = Options.General.FetchAll ? RoundedMaxDateTime : Scale.PeriodEnd(Scale.AddPeriods(RoundedMinDateTime, _columnOffset + _viewportColumns));
			_refreshCancellationToken = new CancellationTokenSource();
			var points = await DataProvider(start, end, Scale, _refreshCancellationToken.Token).ConfigureAwait(true);
			foreach (var point in points)
			{
				//point.PeriodIndex =  point.StartTime.TotalPeriodsSince(MinDateTime, Scale);
				point.PeriodIndex = Scale.PeriodsBetween(RoundedMinDateTime, point.StartTime);
				if (!_dataPoints.ContainsKey(point.PeriodIndex))
				{
					_dataPoints.Add(point.PeriodIndex, point);
				}
			}
			_loading = false;
			await Refreshed.InvokeAsync(null).ConfigureAwait(true);
		}
		StateHasChanged();
	}

	public async Task Reset()
	{
		await Clear().ConfigureAwait(true);
		await ClearSelection().ConfigureAwait(true);
	}

	public DateTime RoundedMaxDateTime => Scale.PeriodEnd(MaxDateTime ?? DateTime.Now);

	public DateTime RoundedMinDateTime
	{
		get
		{
			return _totalColumns < _viewportColumns && Options.General.RightAlign
				? Scale.AddPeriods(Scale.PeriodStart(MinDateTime), _totalColumns - _viewportColumns)
				: Scale.PeriodStart(MinDateTime);
		}
	}

	private double SelectionStartX => (Math.Min(_selectionStartIndex, _selectionEndIndex) - _columnOffset) * Options.Bar.Width;

	private double SelectionEndX => ((Math.Max(_selectionStartIndex, _selectionEndIndex) - _columnOffset) * Options.Bar.Width) + Options.Bar.Width;

	public void SetDates(DateTime min, DateTime max)
	{
		MinDateTime = min;
		MaxDateTime = max;
	}

	public async Task SetScale(TimelineScale scale, bool forceRefresh = false, DateTime? dateTime = null, TimelinePositions reposition = TimelinePositions.Center)
	{
		if (scale != _previousScale || forceRefresh)
		{
			var previousCenter = _previousScale.AddPeriods(_previousScale.PeriodStart(RoundedMinDateTime), _columnOffset + (_viewportColumns / 2));
			var scaleChanged = scale != _previousScale;
			var previousScale = _previousScale;
			var zoomChange = Comparer<TimelineScale>.Default.Compare(scale, previousScale);

			// should we restrict zoom out?
			var restrictCheck = scaleChanged && Options.General.RestrictZoomOut
				&& (scale.UnitType > _previousScale.UnitType || (scale.UnitType == _previousScale.UnitType && scale.UnitCount > _previousScale.UnitCount));

			// change scale
			_previousScale = scale;
			Scale = scale;

			// calculate total number of columns for new scale
			//  note: can't use RoundedMinDateTime as relies on _totalColumns
			_totalColumns = Scale.PeriodsBetween(Scale.PeriodStart(MinDateTime), RoundedMaxDateTime);
			_viewportColumns = _canvasWidth > 0 ? (int)Math.Floor(_canvasWidth / (double)Options.Bar.Width) : 0;

			// do not allow user to zoom out past full window of data
			if (restrictCheck && (_viewportColumns == 0 || _totalColumns < _viewportColumns))
			{
				Scale = _previousScale = previousScale;
				return;
			}

			// clear display if scale has changed
			if (scaleChanged)
			{
				await ScaleChanged.InvokeAsync(Scale).ConfigureAwait(true);
				_dataPoints.Clear();
			}

			// calculate visible columns
			if (_canvasWidth > 0)
			{
				// calculate pan handle width
				_panHandleWidth = Math.Min(((double)_viewportColumns / (double)(_totalColumns + 1)) * _canvasWidth, _canvasWidth);
				//Console.WriteLine($"_panHandleWidth = {_panHandleWidth}, _viewportColumns = {_viewportColumns}, _totalColumns = {_totalColumns}");
				if (_canvasWidth > 0 && (_panHandleX + _panHandleWidth > _canvasWidth))
				{
					_panHandleX = _canvasWidth - _panHandleWidth;
				}
				_columnOffset = (int)Math.Floor((_panHandleX / (double)_canvasWidth) * _totalColumns);
			}

			// re-calc selection in new scale - snap to earlier / later
			if (_selectionRange != null)
			{
				var snappedStart = Scale.PeriodStart(_selectionRange.StartTime);
				var snappedEnd = Scale.PeriodEnd(_selectionRange.EndTime.AddMilliseconds(-1));
				_selectionStartIndex = Scale.PeriodsBetween(RoundedMinDateTime, snappedStart);
				_selectionEndIndex = _selectionStartIndex + Scale.PeriodsBetween(snappedStart, snappedEnd) - 1;
			}

			// re-position viewport?
			PanTo(dateTime ?? previousCenter, reposition);

			// refresh data for new scale?
			await RefreshAsync().ConfigureAwait(true);

			// mark state as changed
			StateHasChanged();
		}
	}

	public async Task SetSelection(DateTime start, DateTime end)
	{
		// quit if no change
		if (_selectionRange?.StartTime == start && _selectionRange?.EndTime == end)
		{
			return;
		}

		// validate range
		if (start < RoundedMinDateTime)
		{
			start = RoundedMaxDateTime;
		}
		if (end > RoundedMaxDateTime)
		{
			end = RoundedMaxDateTime;
		}
		if (!Options.General.AllowDisableSelection)
		{
			if (DisableAfter != DateTime.MinValue && (end >= DisableAfter))
			{
				end = DisableAfter;
			}
			if (DisableBefore != DateTime.MinValue && (start < DisableBefore))
			{
				start = DisableBefore;
			}
		}

		// update selection range indexes
		_selectionStartIndex = Scale.PeriodsBetween(RoundedMinDateTime, start);
		if (_selectionStartIndex < 0)
		{
			_selectionStartIndex = 0;
		}
		_selectionEndIndex = Scale.PeriodsBetween(RoundedMinDateTime, end) - 1;

		// ensure selection is not beyond max datetime
		var maxIndex = Scale.PeriodsBetween(RoundedMinDateTime, RoundedMaxDateTime);
		if (_selectionEndIndex > maxIndex)
		{
			_selectionEndIndex = maxIndex;
		}

		// notify if selection has changed
		if (_selectionRange is null || start != _selectionRange.StartTime || end != _selectionRange.EndTime)
		{
			_selectionRange = new TimeRange { StartTime = start, EndTime = end };
			await SelectionChanged.InvokeAsync(_selectionRange).ConfigureAwait(true);
		}
	}

	private async Task SetSelectionFromDrag(int startIndex, int endIndex)
	{
		if (startIndex == _selectionStartIndex && endIndex == _selectionEndIndex)
		{
			return;
		}

		// if one end of selection is disabled then force selection start/end
		if (Options.Selection.Enabled)
		{
			if (Options.Selection.CanChangeStart && !Options.Selection.CanChangeEnd)
			{
				if (_totalColumns < _viewportColumns)
				{
					endIndex = (Options.General.RightAlign ? _viewportColumns : _totalColumns) - 1;
				}
				else
				{
					endIndex = _totalColumns - 1;
				}
			}
			else if (!Options.Selection.CanChangeStart && Options.Selection.CanChangeEnd)
			{
				startIndex = 0;
			}
		}

		_selectionStartIndex = startIndex;
		_selectionEndIndex = endIndex;

		if (startIndex > -1 && endIndex > -1)
		{
			// calculate time period and sort into chronological order
			var startTime = startIndex <= endIndex
				? Scale.AddPeriods(RoundedMinDateTime, startIndex)
				: Scale.PeriodEnd(Scale.AddPeriods(RoundedMinDateTime, startIndex));
			var endTime = startIndex <= endIndex
				? Scale.PeriodEnd(Scale.AddPeriods(RoundedMinDateTime, endIndex))
				: Scale.PeriodStart(Scale.AddPeriods(RoundedMinDateTime, endIndex));
			if (startTime > endTime)
			{
				var tmp = startTime;
				startTime = endTime;
				endTime = tmp;
			}

			// limit selection range to enabled range?
			if (!Options.General.AllowDisableSelection)
			{
				if (DisableAfter != DateTime.MinValue && (endTime >= DisableAfter))
				{
					endTime = DisableAfter;
					_selectionEndIndex = Scale.PeriodsBetween(RoundedMinDateTime, endTime) - 1;
				}
				if (DisableBefore != DateTime.MinValue && (startTime < DisableBefore))
				{
					startTime = DisableBefore;
					if (_isChartDragging)
					{
						_selectionEndIndex = Scale.PeriodsBetween(RoundedMinDateTime, startTime);
					}
					else
					{
						_selectionStartIndex = Scale.PeriodsBetween(RoundedMinDateTime, startTime);
					}
				}
			}

			// notify if selection has changed
			if (_selectionRange is null || startTime != _selectionRange.StartTime || endTime != _selectionRange.EndTime)
			{
				_selectionRange = new TimeRange { StartTime = startTime, EndTime = endTime };
				await SelectionChanged.InvokeAsync(_selectionRange).ConfigureAwait(true);
			}

			StateHasChanged();
		}
	}

	public async Task ZoomInAsync()
	{
		var scale = Options.General.Scales.FirstOrDefault(x => x.Name == Scale.Name);
		if (scale != null)
		{
			var idx = Options.General.Scales.ToList().IndexOf(scale);
			if (idx > 0)
			{
				await SetScale(Options.General.Scales[idx - 1]);
			}
		}
	}

	public async Task ZoomOutAsync()
	{
		var scale = Options.General.Scales.FirstOrDefault(x => x.Name == Scale.Name);
		if (scale != null)
		{
			var idx = Options.General.Scales.ToList().IndexOf(scale);
			if (idx < Options.General.Scales.Length - 1)
			{
				await SetScale(Options.General.Scales[idx + 1]);
			}
		}
	}

	public async Task ZoomToAsync(DateTime date1, DateTime date2, TimelinePositions position = TimelinePositions.Center)
	{
		if (_canvasWidth > 0)
		{
			var scale = GetScaleToFit(date1, date2);
			if (scale != null)
			{
				await SetScale(scale, true, date2, position).ConfigureAwait(true);
			}
		}
	}


	public async Task ZoomToEndAsync()
	{
		if (_canvasWidth > 0)
		{
			var scale = GetScaleToFit(MinDateTime, MaxDateTime ?? DateTime.Now);
			if (scale != null)
			{
				await SetScale(scale, true, MaxDateTime, TimelinePositions.End).ConfigureAwait(true);
			}
		}
	}

	public async Task ZoomToSelectionAsync()
	{
		if (_canvasWidth > 0 && _selectionRange != null)
		{
			var scale = GetScaleToFit(_selectionRange.StartTime, _selectionRange.EndTime);
			if (scale != null)
			{
				await SetScale(scale, false, _selectionRange.EndTime, TimelinePositions.End).ConfigureAwait(true);
			}
		}
	}

	public async Task ZoomToStartAsync()
	{
		if (_canvasWidth > 0)
		{
			var scale = GetScaleToFit();
			if (scale != null)
			{
				await SetScale(scale, true, MinDateTime, TimelinePositions.Start).ConfigureAwait(true);
			}
		}
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

		public static string ArrowPath(double x, double boundsHeight, double boundsWidth, double padding, bool faceLeft)
		{
			var cy = boundsHeight / 2;
			var w = boundsWidth - (2 * padding);
			var sb = new System.Text.StringBuilder();
			if (faceLeft)
			{
				sb.Append("M ").Append(x + padding).Append(' ').Append(Math.Round(cy));
				sb.Append("l ").Append(w).Append(" -").Append(w);
				sb.Append("l 0 ").Append(2 * w);
			}
			else
			{
				sb.Append("M ").Append(x + (boundsWidth - padding)).Append(' ').Append(Math.Round(cy));
				sb.Append("l -").Append(w).Append(" -").Append(w);
				sb.Append("l 0 ").Append(2 * w);
			}
			sb.Append("Z");
			return sb.ToString();
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
