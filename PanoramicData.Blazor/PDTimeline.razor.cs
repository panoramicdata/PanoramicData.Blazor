namespace PanoramicData.Blazor;

public partial class PDTimeline : IAsyncDisposable, IEnablable
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
	private bool _isPotentialDrag; // Track if pointer is down but not yet dragging
	private bool _isDraggingSelection; // Track if dragging entire selection with modifier key
	private int _dragSelectionStartOffset; // Original start index when drag began
	private int _dragSelectionEndOffset; // Original end index when drag began
	private double _chartDragStartX;
	private const double _dragThreshold = 5.0; // pixels
	private int _selectionStartIndex = -1;
	private int _selectionEndIndex = -1;
	private int _lastSelectionStartIndex;
	private int _lastSelectionEndIndex;
	private TimeRange? _selectionRange;
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
	private readonly Dictionary<int, DataPoint> _dataPoints = [];

	private DateTime _lastQueryEnd = DateTime.MinValue;
	private DateTime _lastQueryStart = DateTime.MinValue;
	private TimelineScale _lastQueryScale = TimelineScale.Years;

	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Gets or sets the date and time after which the timeline is disabled.
	/// </summary>
	[Parameter]
	public DateTime DisableAfter { get; set; }

	/// <summary>
	/// Gets or sets the date and time before which the timeline is disabled.
	/// </summary>
	[Parameter]
	public DateTime DisableBefore { get; set; }

	/// <summary>
	/// An event callback that is invoked when the component has been initialized.
	/// </summary>
	[Parameter]
	public EventCallback Initialized { get; set; }

	/// <summary>
	/// Gets or sets whether the timeline is enabled.
	/// </summary>
	[Parameter]
	public bool IsEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the current scale of the timeline.
	/// </summary>
	[Parameter]
	public TimelineScale Scale { get; set; } = TimelineScale.Years;

	/// <summary>
	/// An event callback that is invoked when the timeline scale changes.
	/// </summary>
	[Parameter]
	public EventCallback<TimelineScale> ScaleChanged { get; set; }

	/// <summary>
	/// An event callback that is invoked when the timeline has been refreshed.
	/// </summary>
	[Parameter]
	public EventCallback Refreshed { get; set; }

	/// <summary>
	/// An event callback that is invoked when the time selection changes.
	/// </summary>
	[Parameter]
	public EventCallback<TimeRange?> SelectionChanged { get; set; }

	/// <summary>
	/// An event callback that is invoked when the time selection change is complete.
	/// </summary>
	[Parameter]
	public EventCallback SelectionChangeEnd { get; set; }

	/// <summary>
	/// A delegate that provides data points to the timeline.
	/// </summary>
	[Parameter]
	public DataProviderDelegate? DataProvider { get; set; }

	/// <summary>
	/// Gets or sets the unique identifier for the component.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-timeline-{++_seq}";

	/// <summary>
	/// Gets or sets whether a new maximum date/time is available.
	/// </summary>
	[Parameter]
	public bool NewMaxDateTimeAvailable { get; set; }

	/// <summary>
	/// Gets or sets whether a new minimum date/time is available.
	/// </summary>
	[Parameter]
	public bool NewMinDateTimeAvailable { get; set; }

	/// <summary>
	/// Gets or sets the maximum date and time of the timeline.
	/// </summary>
	[Parameter]
	public DateTime? MaxDateTime { get; set; }

	/// <summary>
	/// Gets or sets the minimum date and time of the timeline.
	/// </summary>
	[Parameter]
	public DateTime MinDateTime { get; set; }

	/// <summary>
	/// Gets or sets the options for the timeline.
	/// </summary>
	[Parameter]
	public TimelineOptions Options { get; set; } = new TimelineOptions();

	/// <summary>
	/// An event callback that is invoked to update the maximum date.
	/// </summary>
	[Parameter]
	public EventCallback UpdateMaxDate { get; set; }

	/// <summary>
	/// An event callback that is invoked to update the minimum date.
	/// </summary>
	[Parameter]
	public EventCallback UpdateMinDate { get; set; }

	/// <summary>
	/// A function to transform the Y value of data points.
	/// </summary>
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

	public async Task Clear(bool clearSelection = true)
	{
		_dataPoints.Clear();
		if (clearSelection)
		{
			await ClearSelection().ConfigureAwait(true);
		}
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

	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_module != null)
			{
				await _module.InvokeVoidAsync("dispose", Id).ConfigureAwait(true);
				await _module.DisposeAsync().ConfigureAwait(true);
			}

			_objRef?.Dispose();
		}
		catch
		{
		}
	}

	private int GetColumnIndexAtPoint(double clientX)
	{
		// Note: This method calculates using cached _canvasX.
		var index = _columnOffset + (int)Math.Floor((clientX - _canvasX) / Options.Bar.Width);
		return index < 0 ? 0 : index;
	}

	public TimelineScale? GetScaleToFit(DateTime? date1 = null, DateTime? date2 = null)
	{
		date1 ??= RoundedMinDateTime;
		date2 ??= RoundedMaxDateTime;

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

		DataPoint[] tempArray = [.. points.Where(x => x != null && x.SeriesValues.Length > 0)];
		if (tempArray.Length != 0)
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
			if (_dataPoints.TryGetValue(key, out DataPoint? value))
			{
				points[i] = value;
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

		return [.. points];
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

		_columnOffset = (int)Math.Floor(_panHandleX / _canvasWidth * _totalColumns);
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender && JSRuntime is not null)
		{
			try
			{
				_objRef = DotNetObjectReference.Create(this);
				_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDTimeline.razor.js").ConfigureAwait(true);
				if (_module != null)
				{
					await _module.InvokeVoidAsync("initialize", Id, Options, _objRef).ConfigureAwait(true);
				}

				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl);
				if (_commonModule != null)
				{
					_canvasHeight = (int)await _commonModule.InvokeAsync<double>("getHeight", _svgPlotElement).ConfigureAwait(true);
					_canvasWidth = (int)await _commonModule.InvokeAsync<double>("getWidth", _svgPlotElement).ConfigureAwait(true);
					_canvasX = (int)await _commonModule.InvokeAsync<double>("getX", _svgPlotElement).ConfigureAwait(true);
				}

				if (Options.General.AutoRefresh)
				{
					await SetScale(Scale, true);
				}
				// notify app
				await Initialized.InvokeAsync().ConfigureAwait(true);
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	private async Task OnChartPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && Options.Selection.Enabled && !_isChartDragging
			&& !_isSelectionStartDragging && !_isSelectionEndDragging
			&& MinDateTime != DateTime.MinValue)
		{
			// Refresh canvas position before starting drag in case timeline moved
			if (_commonModule != null)
			{
				_canvasX = (int)await _commonModule.InvokeAsync<double>("getX", _svgPlotElement).ConfigureAwait(true);
			}

			// check start time is enabled
			var index = GetColumnIndexAtPoint(args.ClientX);
			
			var startTime = Scale.AddPeriods(RoundedMinDateTime, index);
			if ((DisableBefore != DateTime.MinValue && startTime < DisableBefore)
				|| (DisableAfter != DateTime.MinValue && startTime >= DisableAfter))
			{
				return;
			}

			// Check if Shift key is pressed and clicking within existing selection
			// Ensure indices are in correct order (min to max) for proper hit detection
			var minIndex = Math.Min(_selectionStartIndex, _selectionEndIndex);
			var maxIndex = Math.Max(_selectionStartIndex, _selectionEndIndex);
			
			if (args.ShiftKey && _selectionRange != null && 
			    index >= minIndex && index <= maxIndex)
			{
				// Start dragging the entire selection
				_isDraggingSelection = true;
				_dragSelectionStartOffset = minIndex;
				_dragSelectionEndOffset = maxIndex;
				_chartDragStartX = args.ClientX;
			}
			else
			{
				// Store initial position and mark as potential drag
				// Always start a new selection if Shift wasn't used to initiate move
				_isPotentialDrag = true;
				_chartDragStartX = args.ClientX;
				_selectionStartIndex = index;
				_selectionEndIndex = index;
			}
			
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgPlotElement).ConfigureAwait(true);
			}
		}
	}

	private void OnChartPointerMove(PointerEventArgs args)
	{
		// Handle dragging entire selection
		if (_isDraggingSelection)
		{
			var currentIndex = GetColumnIndexAtPoint(args.ClientX);
			var originalIndex = GetColumnIndexAtPoint(_chartDragStartX);
			var offset = currentIndex - originalIndex;
			
			// Calculate new positions
			var newStartIndex = _dragSelectionStartOffset + offset;
			var newEndIndex = _dragSelectionEndOffset + offset;
			
			// Get the selection width
			var selectionWidth = _dragSelectionEndOffset - _dragSelectionStartOffset;
			
			// Constrain to valid range (0 to _totalColumns - 1)
			var maxIndex = _totalColumns - 1;
			if (newStartIndex < 0)
			{
				newStartIndex = 0;
				newEndIndex = selectionWidth;
			}
			else if (newEndIndex > maxIndex)
			{
				newEndIndex = maxIndex;
				newStartIndex = Math.Max(0, maxIndex - selectionWidth);
			}
			
			// Apply DisableBefore and DisableAfter constraints
			if (DisableBefore != DateTime.MinValue || DisableAfter != DateTime.MinValue)
			{
				// Convert indices to DateTime to check against disable boundaries
				var newStartTime = Scale.AddPeriods(RoundedMinDateTime, newStartIndex);
				var newEndTime = Scale.PeriodEnd(Scale.AddPeriods(RoundedMinDateTime, newEndIndex));
				
				// Check if selection would start before DisableBefore
				if (DisableBefore != DateTime.MinValue && newStartTime < DisableBefore)
				{
					// Constrain start to DisableBefore
					newStartIndex = Scale.PeriodsBetween(RoundedMinDateTime, DisableBefore);
					newEndIndex = newStartIndex + selectionWidth;
					
					// Make sure end doesn't exceed bounds
					if (newEndIndex > maxIndex)
					{
						newEndIndex = maxIndex;
						newStartIndex = Math.Max(0, maxIndex - selectionWidth);
					}
				}
				
				// Check if selection would end after DisableAfter
				if (DisableAfter != DateTime.MinValue && newEndTime > DisableAfter)
				{
					// Constrain end to DisableAfter
					newEndIndex = Scale.PeriodsBetween(RoundedMinDateTime, DisableAfter) - 1;
					newStartIndex = Math.Max(0, newEndIndex - selectionWidth);
					
					// Make sure start doesn't go negative
					if (newStartIndex < 0)
					{
						newStartIndex = 0;
						newEndIndex = selectionWidth;
					}
				}
			}
			
			_ = SetSelectionFromDrag(newStartIndex, newEndIndex).ConfigureAwait(true);
			return;
		}
		
		// Check if we should start dragging based on movement threshold
		if (_isPotentialDrag && !_isChartDragging)
		{
			var distance = Math.Abs(args.ClientX - _chartDragStartX);
			if (distance >= _dragThreshold)
			{
				_isChartDragging = true;
				_isPotentialDrag = false;
			}
		}
		
		if (_isChartDragging)
		{
			// Use cached canvas position from drag start to avoid JS interop on every move
			var index = GetColumnIndexAtPoint(args.ClientX);
			
			// Update selection based on drag direction
			_ = SetSelectionFromDrag(_selectionStartIndex, index).ConfigureAwait(true);
		}
	}

	private async Task OnChartPointerUp(PointerEventArgs args)
	{
		if (_isPotentialDrag || _isChartDragging || _isDraggingSelection)
		{
			// If we never started dragging (distance < threshold), treat as single click
			if (_isPotentialDrag && !_isChartDragging)
			{
				var index = GetColumnIndexAtPoint(args.ClientX);
				await SetSelectionFromDrag(index, index).ConfigureAwait(true);
			}
			
			// Notify selection change complete for both single clicks and drags
			if (MinDateTime != DateTime.MinValue)
			{
				await OnSelectionChangeEnd().ConfigureAwait(true);
			}
			
			// Reset drag state but keep selection indices
			_isChartDragging = false;
			_isPotentialDrag = false;
			_isDraggingSelection = false;
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
			_canvasX = (int)(await _commonModule.InvokeAsync<double>("getX", _svgPlotElement).ConfigureAwait(true));
			_canvasWidth = (int)await _commonModule.InvokeAsync<double>("getWidth", _svgPlotElement).ConfigureAwait(true);
		}

		await SetScale(Scale, true).ConfigureAwait(true);
		await InvokeAsync(() => StateHasChanged()).ConfigureAwait(true);
	}

	[JSInvokable("PanoramicData.Blazor.PDTimeline.IsPointInSelection")]
	public bool IsPointInSelection(double clientX)
	{
		// No selection exists
		if (_selectionRange == null || _selectionStartIndex == -1 || _selectionEndIndex == -1)
		{
			return false;
		}

		// Calculate which column index the point is over
		var index = GetColumnIndexAtPoint(clientX);
		
		// Ensure we check against the correct range regardless of index order
		var minIndex = Math.Min(_selectionStartIndex, _selectionEndIndex);
		var maxIndex = Math.Max(_selectionStartIndex, _selectionEndIndex);
		
		// Check if the index is within the selection range
		return index >= minIndex && index <= maxIndex;
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
		if (IsEnabled && Options.Selection.CanChangeEnd && !_isSelectionStartDragging)
		{
			_isSelectionEndDragging = true;

			// Refresh canvas position before starting drag in case timeline moved
			if (_commonModule != null)
			{
				_canvasX = (int)await _commonModule.InvokeAsync<double>("getX", _svgPlotElement).ConfigureAwait(true);
			}

			_lastSelectionStartIndex = _selectionStartIndex;
			_lastSelectionEndIndex = _selectionEndIndex;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgSelectionHandleEnd).ConfigureAwait(true);
			}
		}
	}

	private void OnSelectionEndPointerMove(PointerEventArgs args)
	{
		if (_isSelectionEndDragging)
		{
			// Use cached canvas position from drag start to avoid JS interop on every move
			var index = GetColumnIndexAtPoint(args.ClientX);
			
			if (index >= _selectionStartIndex)
			{
				_ = SetSelectionFromDrag(_selectionStartIndex, index).ConfigureAwait(true);
			}
		}
	}

	private async Task OnSelectionEndPointerUp(PointerEventArgs args)
	{
		if (_isSelectionEndDragging)
		{
			_isSelectionEndDragging = false;
			// Don't reset _canvasXAtDragStart - it will be overwritten on next pointer down
			if (MinDateTime != DateTime.MinValue)
			{
				await OnSelectionChangeEnd().ConfigureAwait(true);
			}
		}
	}

	private async Task OnSelectionStartPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && Options.Selection.CanChangeStart && !_isSelectionEndDragging)
		{
			_isSelectionStartDragging = true;

			// Refresh canvas position before starting drag in case timeline moved
			if (_commonModule != null)
			{
				_canvasX = (int)await _commonModule.InvokeAsync<double>("getX", _svgPlotElement).ConfigureAwait(true);
			}

			_lastSelectionStartIndex = _selectionStartIndex;
			_lastSelectionEndIndex = _selectionEndIndex;
			if (_commonModule != null)
			{
				await _commonModule.InvokeVoidAsync("setPointerCapture", args.PointerId, _svgSelectionHandleStart).ConfigureAwait(true);
			}
		}
	}

	private void OnSelectionStartPointerMove(PointerEventArgs args)
	{
		if (_isSelectionStartDragging)
		{
			var index = GetColumnIndexAtPoint(args.ClientX);
			
			if (index <= _selectionEndIndex)
			{
				_ = SetSelectionFromDrag(index, _selectionEndIndex).ConfigureAwait(true);
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

	public void PanTo(DateTime dateTime)
		=> PanTo(dateTime, TimelinePositions.Center);

	public void PanTo(DateTime dateTime, TimelinePositions position)
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

	public async Task RefreshAsync(bool force = false)
	{
		if (DataProvider != null && MinDateTime != DateTime.MinValue)
		{
			var start = Options.General.FetchAll ? RoundedMinDateTime : Scale.AddPeriods(RoundedMinDateTime, _columnOffset);
			var end = Options.General.FetchAll ? RoundedMaxDateTime : Scale.PeriodEnd(Scale.AddPeriods(RoundedMinDateTime, _columnOffset + _viewportColumns));

			// only proceed if query is different to last one
			if (force || start != _lastQueryStart || end != _lastQueryEnd || Scale != _lastQueryScale)
			{
				_lastQueryEnd = end;
				_lastQueryStart = start;
				_lastQueryScale = Scale;

				// cancel previous query?
				_refreshCancellationToken?.Cancel();
				_refreshCancellationToken = null;

				// either fetch all data points for scale, or just the current viewport
				_loading = true;
				_refreshCancellationToken = new CancellationTokenSource();
				var points = await DataProvider(start, end, Scale, _refreshCancellationToken.Token).ConfigureAwait(true);
				foreach (var point in points)
				{
					//point.PeriodIndex =  point.StartTime.TotalPeriodsSince(MinDateTime, Scale);
					point.PeriodIndex = Scale.PeriodsBetween(RoundedMinDateTime, point.StartTime);
					_dataPoints.TryAdd(point.PeriodIndex, point);
				}

				_loading = false;
				await Refreshed.InvokeAsync(null).ConfigureAwait(true);
			}
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

			if (scaleChanged || forceRefresh)
			{
				_dataPoints.Clear();
			}

			if (scaleChanged)
			{
				await ScaleChanged.InvokeAsync(Scale).ConfigureAwait(true);
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
			await RefreshAsync(forceRefresh).ConfigureAwait(true);

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
				(endTime, startTime) = (startTime, endTime);
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

	public async Task ZoomToAsync(DateTime date1, DateTime date2)
		=> await ZoomToAsync(date1, date2, TimelinePositions.Center);

	public async Task ZoomToAsync(DateTime date1, DateTime date2, TimelinePositions position)
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

	public async Task ZoomToSelectionAsync(bool forceRefresh = false)
	{
		if (_canvasWidth > 0 && _selectionRange != null)
		{
			var scale = GetScaleToFit(_selectionRange.StartTime, _selectionRange.EndTime);
			if (scale != null)
			{
				await SetScale(scale, forceRefresh, _selectionRange.EndTime, TimelinePositions.End).ConfigureAwait(true);
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
				await SetScale(scale, true, MinDateTime, TimelinePositions.Start). ConfigureAwait(true);
			}
		}
	}

	public void Disable()
	{
		IsEnabled = false;
		StateHasChanged();
	}

	public void Enable()
	{
		IsEnabled = true;
		StateHasChanged();
	}

	public void SetEnabled(bool isEnabled)
	{
		IsEnabled = isEnabled;
		StateHasChanged();
	}

	public static class Utilities
	{
		public static string DescribeArc(double x, double y, double radius, double startAngle, double endAngle)
		{
			var sp = PolarToCartesian(x, y, radius, endAngle);
			var ep = PolarToCartesian(x, y, radius, startAngle);
			var arcSweep = endAngle - startAngle <= 180 ? "0" : "1";
			var d = string.Join(" ", [
				"M",
				sp.x.ToString("0.00", CultureInfo.InvariantCulture),
				sp.y.ToString("0.00", CultureInfo.InvariantCulture),
				"A",
				radius.ToString(CultureInfo.InvariantCulture),
				radius.ToString(CultureInfo.InvariantCulture),
				"0",
				arcSweep,
				"0",
				ep.x.ToString("0.00", CultureInfo.InvariantCulture),
				ep.y.ToString("0.00", CultureInfo.InvariantCulture)
			]);
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
			var sb = new StringBuilder();
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

			sb.Append('Z');
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
