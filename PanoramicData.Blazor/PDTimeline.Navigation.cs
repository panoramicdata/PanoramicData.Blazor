namespace PanoramicData.Blazor;

/// <summary>
/// Scale, zoom, pan and data refresh.
/// </summary>
public partial class PDTimeline
{
	private int _totalColumns;
	private int _viewportColumns;
	private TimelineScale _previousScale = TimelineScale.Years;
	private bool _scaleApplied;
	private CancellationTokenSource? _refreshCancellationToken;
	private DateTime _lastQueryEnd = DateTime.MinValue;
	private DateTime _lastQueryStart = DateTime.MinValue;
	private TimelineScale _lastQueryScale = TimelineScale.Years;

	/// <summary>
	/// Determines whether zoom-in is currently possible.
	/// </summary>
	/// <returns>True when a finer scale is available and the component is enabled.</returns>
	public bool CanZoomIn() => IsEnabled && GetScaleIndex() > 0;

	/// <summary>
	/// Determines whether zoom-out is currently possible.
	/// </summary>
	/// <returns>True when a broader scale is available and allowed.</returns>
	public bool CanZoomOut()
	{
		var idx = GetScaleIndex();
		if (!IsEnabled || idx < 0 || idx >= Options.General.Scales.Length - 1)
		{
			return false;
		}

		if (!Options.General.RestrictZoomOut)
		{
			return true;
		}

		// calculate total number of columns for new scale
		var newScale = Options.General.Scales[idx + 1];
		var totalColumns = newScale.PeriodsBetween(RoundedMinDateTime, RoundedMaxDateTime);
		var viewportColumns = CanvasColumns;
		return viewportColumns > 0 && viewportColumns <= totalColumns;
	}

	/// <summary>
	/// Gets the position of the current <see cref="Scale"/> in the configured scales, matched by name.
	/// </summary>
	/// <returns>The index of the scale, or -1 when it is not configured.</returns>
	private int GetScaleIndex() => Array.FindIndex(Options.General.Scales, x => x.Name == Scale.Name);

	/// <summary>
	/// Returns the smallest configured scale that fits the whole timeline in the viewport.
	/// </summary>
	/// <returns>The fitting scale, or the first configured scale when none fit.</returns>
	public TimelineScale? GetScaleToFit() => GetScaleToFit(null, null);

	/// <summary>
	/// Returns the smallest configured scale that fits the range from the provided date to the end of the
	/// timeline in the viewport.
	/// </summary>
	/// <param name="date1">Start date. Null for the rounded minimum date.</param>
	/// <returns>The fitting scale, or the first configured scale when none fit.</returns>
	public TimelineScale? GetScaleToFit(DateTime? date1) => GetScaleToFit(date1, null);

	/// <summary>
	/// Returns the smallest configured scale that fits the provided date range in the viewport.
	/// </summary>
	/// <param name="date1">Start date. Null for the rounded minimum date.</param>
	/// <param name="date2">End date. Null for the rounded maximum date.</param>
	/// <returns>The fitting scale, or the first configured scale when none fit.</returns>
	public TimelineScale? GetScaleToFit(DateTime? date1, DateTime? date2)
	{
		var start = date1 ?? RoundedMinDateTime;
		var end = date2 ?? RoundedMaxDateTime;

		var viewportColumns = CanvasColumns;
		for (var i = 0; i < Options.General.Scales.Length - 1; i++)
		{
			var newScale = Options.General.Scales[i];
			var totalColumns = newScale.PeriodsBetween(start, end);
			if (totalColumns > 0 && totalColumns <= viewportColumns)
			{
				return newScale;
			}
		}

		return Options.General.Scales.FirstOrDefault();
	}

	/// <summary>
	/// Pans the viewport to the specified date using centered positioning.
	/// </summary>
	/// <param name="dateTime">Target date to pan to.</param>
	public void PanTo(DateTime dateTime)
		=> PanTo(dateTime, TimelinePositions.Center);

	/// <summary>
	/// Pans the viewport so the specified date appears at the requested position.
	/// </summary>
	/// <param name="dateTime">Target date to pan to.</param>
	/// <param name="position">Position within the viewport for the target date.</param>
	public void PanTo(DateTime dateTime, TimelinePositions position)
	{
		var lastDateTime = MaxDateTime ?? CurrentDateTime;
		if (dateTime < MinDateTime || dateTime > lastDateTime)
		{
			dateTime = lastDateTime;
		}

		var maxOffset = Scale.PeriodsBetween(RoundedMinDateTime, RoundedMaxDateTime) - _viewportColumns;
		if (maxOffset <= 0)
		{
			_columnOffset = 0;
		}
		else
		{
			var newOffset = GetPanOffset(dateTime, position, maxOffset);
			if (newOffset >= 0 && newOffset <= maxOffset)
			{
				_columnOffset = newOffset;
			}
		}

		// update pan handle x
		_panHandleX = (_columnOffset / (double)_totalColumns) * (double)_canvasWidth;
	}

	/// <summary>
	/// Calculates the column offset that puts the date at the requested position in the viewport.
	/// </summary>
	private int GetPanOffset(DateTime dateTime, TimelinePositions position, int maxOffset)
	{
		switch (position)
		{
			case TimelinePositions.Start:
				// as far left as the timeline allows: a date in the last viewport cannot be the first column
				return Math.Min(Scale.PeriodsBetween(RoundedMinDateTime, Scale.PeriodStart(dateTime)), maxOffset);
			case TimelinePositions.Center:
				return Scale.PeriodsBetween(RoundedMinDateTime, dateTime) - (_viewportColumns / 2);
			case TimelinePositions.End:
				return Scale.PeriodsBetween(RoundedMinDateTime, Scale.PeriodEnd(dateTime)) - _viewportColumns;
			default:
				return 0;
		}
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

	/// <summary>
	/// Refreshes data from the configured provider when the visible range has changed.
	/// </summary>
	public Task RefreshAsync() => RefreshAsync(false);

	/// <summary>
	/// Refreshes data from the configured provider.
	/// </summary>
	/// <param name="force">True to force a refresh even if query parameters are unchanged.</param>
	public async Task RefreshAsync(bool force)
	{
		if (DataProvider != null && MinDateTime != DateTime.MinValue)
		{
			await FetchDataAsync(DataProvider, force).ConfigureAwait(true);
		}

		StateHasChanged();
	}

	private async Task FetchDataAsync(DataProviderDelegate dataProvider, bool force)
	{
		var (start, end) = GetQueryRange();

		// only proceed if query is different to last one
		if (!force && start == _lastQueryStart && end == _lastQueryEnd && Scale == _lastQueryScale)
		{
			return;
		}

		_lastQueryEnd = end;
		_lastQueryStart = start;
		_lastQueryScale = Scale;

		// cancel previous query?
		_refreshCancellationToken?.Cancel();
		_refreshCancellationToken?.Dispose();
		_refreshCancellationToken = null;

		// either fetch all data points for scale, or just the current viewport
		_loading = true;
		_refreshCancellationToken = new CancellationTokenSource();
		var points = await dataProvider(start, end, Scale, _refreshCancellationToken.Token).ConfigureAwait(true);
		foreach (var point in points)
		{
			point.PeriodIndex = Scale.PeriodsBetween(RoundedMinDateTime, point.StartTime);
			_dataPoints.TryAdd(point.PeriodIndex, point);
		}

		_loading = false;
		await Refreshed.InvokeAsync(null).ConfigureAwait(true);
	}

	private (DateTime Start, DateTime End) GetQueryRange()
	{
		if (Options.General.FetchAll)
		{
			return (RoundedMinDateTime, RoundedMaxDateTime);
		}

		return (
			Scale.AddPeriods(RoundedMinDateTime, _columnOffset),
			Scale.PeriodEnd(Scale.AddPeriods(RoundedMinDateTime, _columnOffset + _viewportColumns)));
	}

	/// <summary>
	/// Sets the timeline scale, keeping the viewport centred, if it differs from the current one.
	/// </summary>
	/// <param name="scale">New scale to apply.</param>
	public Task SetScale(TimelineScale scale)
		=> SetScale(scale, false, null, TimelinePositions.Center, true);

	/// <summary>
	/// Sets the timeline scale, keeping the viewport centred.
	/// </summary>
	/// <param name="scale">New scale to apply.</param>
	/// <param name="forceRefresh">True to re-lay-out and force a data refresh even when the scale is unchanged.</param>
	public Task SetScale(TimelineScale scale, bool forceRefresh)
		=> SetScale(scale, forceRefresh, null, TimelinePositions.Center, true);

	/// <summary>
	/// Sets the timeline scale, keeping the viewport centred.
	/// </summary>
	/// <param name="scale">New scale to apply.</param>
	/// <param name="forceRefresh">True to re-lay-out the timeline even when the scale is unchanged.</param>
	/// <param name="refreshData">When false, a forced refresh re-lays-out the timeline without clearing the data cache or forcing a re-fetch; the unchanged-range guard in RefreshAsync still applies.</param>
	public Task SetScale(TimelineScale scale, bool forceRefresh, bool refreshData)
		=> SetScale(scale, forceRefresh, null, TimelinePositions.Center, refreshData);

	/// <summary>
	/// Sets the timeline scale and centres the viewport on a date.
	/// </summary>
	/// <param name="scale">New scale to apply.</param>
	/// <param name="forceRefresh">True to re-lay-out and force a data refresh even when the scale is unchanged.</param>
	/// <param name="dateTime">Focus date for repositioning; null keeps the current centre.</param>
	public Task SetScale(TimelineScale scale, bool forceRefresh, DateTime? dateTime)
		=> SetScale(scale, forceRefresh, dateTime, TimelinePositions.Center, true);

	/// <summary>
	/// Sets the timeline scale and positions the viewport on a date.
	/// </summary>
	/// <param name="scale">New scale to apply.</param>
	/// <param name="forceRefresh">True to re-lay-out and force a data refresh even when the scale is unchanged.</param>
	/// <param name="dateTime">Focus date for repositioning; null keeps the current centre.</param>
	/// <param name="reposition">Viewport position used when applying <paramref name="dateTime"/>.</param>
	public Task SetScale(TimelineScale scale, bool forceRefresh, DateTime? dateTime, TimelinePositions reposition)
		=> SetScale(scale, forceRefresh, dateTime, reposition, true);

	/// <summary>
	/// Sets timeline scale and recomputes viewport, selection, and data as needed.
	/// </summary>
	/// <param name="scale">New scale to apply.</param>
	/// <param name="forceRefresh">True to force data refresh.</param>
	/// <param name="dateTime">Optional focus date for repositioning.</param>
	/// <param name="reposition">Viewport position used when applying <paramref name="dateTime"/>.</param>
	/// <param name="refreshData">When false, a forced refresh re-lays-out the timeline without clearing the data cache or forcing a re-fetch; the unchanged-range guard in RefreshAsync still applies.</param>
	public async Task SetScale(TimelineScale scale, bool forceRefresh, DateTime? dateTime, TimelinePositions reposition, bool refreshData)
	{
		if (!ShouldApplyScale(scale, forceRefresh))
		{
			return;
		}

		var previousCenter = _previousScale.AddPeriods(_previousScale.PeriodStart(RoundedMinDateTime), _columnOffset + (_viewportColumns / 2));
		var scaleChanged = scale != _previousScale;
		var raiseScaleChanged = IsScaleChangeToAnnounce(scale, scaleChanged);
		if (!TryApplyScale(scale, scaleChanged))
		{
			return;
		}

		_scaleApplied = true;
		var refetch = forceRefresh && refreshData;
		if (scaleChanged || refetch)
		{
			_dataPoints.Clear();
		}

		if (raiseScaleChanged)
		{
			await ScaleChanged.InvokeAsync(Scale).ConfigureAwait(true);
		}

		UpdatePanHandleForScale();
		SnapSelectionToScale();

		// re-position viewport?
		PanTo(dateTime ?? previousCenter, reposition);

		// refresh data for new scale? (a pure resize with RefetchDataOnResize=false only re-lays-out - the
		// unchanged-range guard in RefreshAsync still re-fetches if the visible range genuinely changed)
		await RefreshAsync(refetch).ConfigureAwait(true);

		// mark state as changed
		StateHasChanged();
	}

	private bool ShouldApplyScale(TimelineScale scale, bool forceRefresh)
		=> MinDateTime != DateTime.MinValue && (scale != _previousScale || forceRefresh);

	/// <summary>
	/// The first layout applies the Scale parameter the consumer supplied: that is not a change to announce.
	/// </summary>
	private bool IsScaleChangeToAnnounce(TimelineScale scale, bool scaleChanged)
		=> scaleChanged && (_scaleApplied || !ReferenceEquals(scale, Scale));

	/// <summary>
	/// Switches to the scale and lays out its columns, unless zoom-out is restricted and the scale would show
	/// more than the whole timeline.
	/// </summary>
	/// <returns>False when the scale was refused and the previous one kept.</returns>
	private bool TryApplyScale(TimelineScale scale, bool scaleChanged)
	{
		var previousScale = _previousScale;

		// should we restrict zoom out?
		var restrictCheck = scaleChanged && Options.General.RestrictZoomOut && IsBroaderThan(scale, previousScale);

		// change scale
		_previousScale = scale;
		Scale = scale;

		// calculate total number of columns for new scale
		//  note: can't use RoundedMinDateTime as relies on _totalColumns
		_totalColumns = Scale.PeriodsBetween(Scale.PeriodStart(MinDateTime), RoundedMaxDateTime);
		_viewportColumns = CanvasColumns;

		// do not allow user to zoom out past full window of data
		if (restrictCheck && (_viewportColumns == 0 || _totalColumns < _viewportColumns))
		{
			Scale = _previousScale = previousScale;
			return false;
		}

		return true;
	}

	private static bool IsBroaderThan(TimelineScale scale, TimelineScale other)
		=> scale.UnitType > other.UnitType || (scale.UnitType == other.UnitType && scale.UnitCount > other.UnitCount);

	private void UpdatePanHandleForScale()
	{
		if (_canvasWidth <= 0)
		{
			return;
		}

		// calculate pan handle width
		_panHandleWidth = Math.Min(((double)_viewportColumns / (double)(_totalColumns + 1)) * _canvasWidth, _canvasWidth);
		if (_panHandleX + _panHandleWidth > _canvasWidth)
		{
			_panHandleX = _canvasWidth - _panHandleWidth;
		}

		_columnOffset = (int)Math.Floor((_panHandleX / (double)_canvasWidth) * _totalColumns);
	}

	/// <summary>
	/// Re-calculates the selection in the new scale, snapped to earlier / later periods.
	/// </summary>
	private void SnapSelectionToScale()
	{
		if (_selectionRange != null)
		{
			var snappedStart = Scale.PeriodStart(_selectionRange.StartTime);
			var snappedEnd = Scale.PeriodEnd(_selectionRange.EndTime.AddMilliseconds(-1));
			_selectionStartIndex = Scale.PeriodsBetween(RoundedMinDateTime, snappedStart);
			_selectionEndIndex = _selectionStartIndex + Scale.PeriodsBetween(snappedStart, snappedEnd) - 1;
		}
	}

	/// <summary>
	/// Zooms in by one configured scale step.
	/// </summary>
	public async Task ZoomInAsync()
	{
		await SuspendFollowNowAsync().ConfigureAwait(true);
		var idx = GetScaleIndex();
		if (idx > 0)
		{
			await SetScale(Options.General.Scales[idx - 1]).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Zooms out by one configured scale step.
	/// </summary>
	public async Task ZoomOutAsync()
	{
		await SuspendFollowNowAsync().ConfigureAwait(true);
		var idx = GetScaleIndex();
		if (idx >= 0 && idx < Options.General.Scales.Length - 1)
		{
			await SetScale(Options.General.Scales[idx + 1]).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Zooms to fit the specified range using centered positioning.
	/// </summary>
	/// <param name="date1">Start date.</param>
	/// <param name="date2">End date.</param>
	public async Task ZoomToAsync(DateTime date1, DateTime date2)
		=> await ZoomToAsync(date1, date2, TimelinePositions.Center).ConfigureAwait(true);

	/// <summary>
	/// Zooms to fit the specified range and repositions according to the requested position.
	/// </summary>
	/// <param name="date1">Start date.</param>
	/// <param name="date2">End date.</param>
	/// <param name="position">Target position for the end date.</param>
	public async Task ZoomToAsync(DateTime date1, DateTime date2, TimelinePositions position)
	{
		await SuspendFollowNowAsync().ConfigureAwait(true);
		await ZoomToFitAsync(date1, date2, true, date2, position).ConfigureAwait(true);
	}

	/// <summary>
	/// Zooms to a scale that fits the full timeline and positions at the end.
	/// </summary>
	public async Task ZoomToEndAsync()
	{
		await SuspendFollowNowAsync().ConfigureAwait(true);
		await ZoomToFitAsync(MinDateTime, MaxDateTime ?? CurrentDateTime, true, MaxDateTime, TimelinePositions.End).ConfigureAwait(true);
	}

	/// <summary>
	/// Zooms to a scale that fits the current selection.
	/// </summary>
	public Task ZoomToSelectionAsync() => ZoomToSelectionAsync(false);

	/// <summary>
	/// Zooms to a scale that fits the current selection.
	/// </summary>
	/// <param name="forceRefresh">True to force data refresh.</param>
	public async Task ZoomToSelectionAsync(bool forceRefresh)
	{
		await SuspendFollowNowAsync().ConfigureAwait(true);
		if (_selectionRange != null)
		{
			await ZoomToFitAsync(_selectionRange.StartTime, _selectionRange.EndTime, forceRefresh, _selectionRange.EndTime, TimelinePositions.End).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Zooms to a scale that fits the full timeline and positions at the start.
	/// </summary>
	public async Task ZoomToStartAsync()
	{
		await SuspendFollowNowAsync().ConfigureAwait(true);
		await ZoomToFitAsync(null, null, true, MinDateTime, TimelinePositions.Start).ConfigureAwait(true);
	}

	private async Task ZoomToFitAsync(DateTime? start, DateTime? end, bool forceRefresh, DateTime? focus, TimelinePositions position)
	{
		if (_canvasWidth > 0)
		{
			var scale = GetScaleToFit(start, end);
			if (scale != null)
			{
				await SetScale(scale, forceRefresh, focus, position).ConfigureAwait(true);
			}
		}
	}
}
