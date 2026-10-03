namespace PanoramicData.Blazor;

/// <summary>
/// Pointer and wheel handling for the chart, the selection handles and the pan track.
/// </summary>
public partial class PDTimeline
{
	private int GetColumnIndexAtPoint(double clientX)
	{
		// Note: This method calculates using cached _canvasX.
		var index = _columnOffset + (int)Math.Floor((clientX - _canvasX) / Options.Bar.Width);
		return index < 0 ? 0 : index;
	}

	/// <summary>
	/// Refreshes the cached canvas position before a drag starts, in case the timeline has moved.
	/// </summary>
	private async Task RefreshCanvasXAsync()
	{
		if (_commonModule != null)
		{
			_canvasX = (int)await _commonModule.InvokeAsync<double>("getX", SvgPlotElement).ConfigureAwait(true);
		}
	}

	private async Task SetPointerCaptureAsync(long pointerId, ElementReference element)
	{
		if (_commonModule != null)
		{
			await _commonModule.InvokeVoidAsync("setPointerCapture", pointerId, element).ConfigureAwait(true);
		}
	}

	private bool CanStartChartSelection
		=> IsEnabled && Options.Selection.Enabled && !_isChartDragging
			&& !_isSelectionStartDragging && !_isSelectionEndDragging
			&& MinDateTime != DateTime.MinValue;

	private bool IsColumnStartDisabled(int index)
	{
		var startTime = Scale.AddPeriods(RoundedMinDateTime, index);
		return (DisableBefore != DateTime.MinValue && startTime < DisableBefore)
			|| (DisableAfter != DateTime.MinValue && startTime >= DisableAfter);
	}

	private async Task OnChartPointerDown(PointerEventArgs args)
	{
		if (!CanStartChartSelection)
		{
			return;
		}

		await SuspendFollowNowAsync().ConfigureAwait(true);
		await RefreshCanvasXAsync().ConfigureAwait(true);

		// check start time is enabled
		var index = GetColumnIndexAtPoint(args.ClientX);
		if (IsColumnStartDisabled(index))
		{
			return;
		}

		_chartDragStartX = args.ClientX;

		// Check if Shift key is pressed and clicking within existing selection
		if (args.ShiftKey && IsColumnInSelection(index))
		{
			// Start dragging the entire selection
			_isDraggingSelection = true;
			_dragSelectionStartOffset = Math.Min(_selectionStartIndex, _selectionEndIndex);
			_dragSelectionEndOffset = Math.Max(_selectionStartIndex, _selectionEndIndex);
		}
		else
		{
			// Mark as potential drag, storing the initial index for use in drag operations
			_isPotentialDrag = true;
			_selectionStartIndex = index;
			// Reset end index to prevent visual artifact of extended selection
			_selectionEndIndex = -1;
		}

		await SetPointerCaptureAsync(args.PointerId, SvgPlotElement).ConfigureAwait(true);
	}

	private void OnChartPointerMove(PointerEventArgs args)
	{
		// Handle dragging entire selection
		if (_isDraggingSelection)
		{
			MoveSelection(args.ClientX);
			return;
		}

		// Check if we should start dragging based on movement threshold
		if (_isPotentialDrag && !_isChartDragging && Math.Abs(args.ClientX - _chartDragStartX) >= _dragThreshold)
		{
			_isChartDragging = true;
			_isPotentialDrag = false;
		}

		if (_isChartDragging)
		{
			// Use cached canvas position from drag start to avoid JS interop on every move
			var index = GetColumnIndexAtPoint(args.ClientX);

			// Update selection based on drag direction
			_ = SetSelectionFromDrag(_selectionStartIndex, index).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Moves the whole selection by the columns the pointer has moved, keeping its width and keeping it within
	/// the timeline and out of the disabled ranges.
	/// </summary>
	private void MoveSelection(double clientX)
	{
		var offset = GetColumnIndexAtPoint(clientX) - GetColumnIndexAtPoint(_chartDragStartX);
		var selectionWidth = _dragSelectionEndOffset - _dragSelectionStartOffset;
		var (newStartIndex, newEndIndex) = ClampMovedSelection(_dragSelectionStartOffset + offset, _dragSelectionEndOffset + offset, selectionWidth);
		(newStartIndex, newEndIndex) = KeepMovedSelectionEnabled(newStartIndex, newEndIndex, selectionWidth);
		_ = SetSelectionFromDrag(newStartIndex, newEndIndex).ConfigureAwait(true);
	}

	/// <summary>
	/// Constrains a moved selection to the valid columns (0 to _totalColumns - 1).
	/// </summary>
	private (int Start, int End) ClampMovedSelection(int startIndex, int endIndex, int selectionWidth)
	{
		var maxIndex = _totalColumns - 1;
		if (startIndex < 0)
		{
			return (0, selectionWidth);
		}

		if (endIndex > maxIndex)
		{
			return (Math.Max(0, maxIndex - selectionWidth), maxIndex);
		}

		return (startIndex, endIndex);
	}

	/// <summary>
	/// Applies the DisableBefore and DisableAfter constraints to a moved selection.
	/// </summary>
	private (int Start, int End) KeepMovedSelectionEnabled(int startIndex, int endIndex, int selectionWidth)
	{
		// Convert indices to DateTime to check against disable boundaries
		var startTime = Scale.AddPeriods(RoundedMinDateTime, startIndex);
		var endTime = Scale.PeriodEnd(Scale.AddPeriods(RoundedMinDateTime, endIndex));

		// Constrain start to DisableBefore, making sure the end doesn't exceed bounds
		if (DisableBefore != DateTime.MinValue && startTime < DisableBefore)
		{
			var disabledBeforeIndex = Scale.PeriodsBetween(RoundedMinDateTime, DisableBefore);
			(startIndex, endIndex) = ClampMovedSelection(disabledBeforeIndex, disabledBeforeIndex + selectionWidth, selectionWidth);
		}

		// Constrain end to DisableAfter
		if (DisableAfter != DateTime.MinValue && endTime > DisableAfter)
		{
			endIndex = Scale.PeriodsBetween(RoundedMinDateTime, DisableAfter) - 1;
			startIndex = Math.Max(0, endIndex - selectionWidth);
		}

		return (startIndex, endIndex);
	}

	private async Task OnChartPointerUp(PointerEventArgs args)
	{
		if (!_isPotentialDrag && !_isChartDragging && !_isDraggingSelection)
		{
			return;
		}

		// If we never started dragging (distance < threshold), treat as single click
		if (_isPotentialDrag && !_isChartDragging)
		{
			var index = GetColumnIndexAtPoint(args.ClientX);
			await SetSelectionFromDrag(index, index).ConfigureAwait(true);
		}

		// Notify selection change complete for both single clicks and drags
		await EndSelectionChangeAsync().ConfigureAwait(true);

		// Reset drag state but keep selection indices
		_isChartDragging = false;
		_isPotentialDrag = false;
		_isDraggingSelection = false;
	}

	private async Task EndSelectionChangeAsync()
	{
		if (MinDateTime != DateTime.MinValue)
		{
			await OnSelectionChangeEnd().ConfigureAwait(true);
		}
	}

	private async Task OnMouseWheel(WheelEventArgs args)
	{
		if (!IsEnabled || !args.CtrlKey)
		{
			return;
		}

		await SuspendFollowNowAsync().ConfigureAwait(true);
		var index = GetScaleIndex();
		if (args.DeltaY < 0)
		{
			// zoom in
			if (index > 0)
			{
				await SetScale(Options.General.Scales[index - 1]).ConfigureAwait(true);
			}
		}
		else if (index < Options.General.Scales.Length - 1)
		{
			// zoom out
			await SetScale(Options.General.Scales[index + 1]).ConfigureAwait(true);
		}
	}

	private async Task OnPanPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && !_isPanDragging)
		{
			await SuspendFollowNowAsync().ConfigureAwait(true);
			_panDragOrigin = args.ClientX;
			await SetPointerCaptureAsync(args.PointerId, SvgPanElement).ConfigureAwait(true);
		}
	}

	private void OnPanPointerMove(PointerEventArgs args)
	{
		// initiate a drag operation?
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
		else if (IsEnabled)
		{
			refresh = PageViewport(args.ClientX - _canvasX);
		}

		if (refresh && !Options.General.FetchAll)
		{
			await RefreshAsync().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Moves the entire viewport along when the pan track is clicked beside the handle.
	/// </summary>
	/// <returns>True when the viewport moved.</returns>
	private bool PageViewport(double x)
	{
		if (x < _panHandleX)
		{
			MovePanHandle(_panHandleX - _panHandleWidth);
			return true;
		}

		if (x > (_panHandleX + _panHandleWidth))
		{
			MovePanHandle(_panHandleX + _panHandleWidth);
			return true;
		}

		return false;
	}

	private async Task BeginHandleDragAsync(long pointerId, ElementReference handle)
	{
		await RefreshCanvasXAsync().ConfigureAwait(true);
		_lastSelectionStartIndex = _selectionStartIndex;
		_lastSelectionEndIndex = _selectionEndIndex;
		await SetPointerCaptureAsync(pointerId, handle).ConfigureAwait(true);
	}

	private async Task OnSelectionEndPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && Options.Selection.CanChangeEnd && !_isSelectionStartDragging)
		{
			await SuspendFollowNowAsync().ConfigureAwait(true);
			_isSelectionEndDragging = true;
			await BeginHandleDragAsync(args.PointerId, SvgSelectionHandleEnd).ConfigureAwait(true);
		}
	}

	private void OnSelectionEndPointerMove(PointerEventArgs args)
	{
		if (_isSelectionEndDragging)
		{
			// Use cached canvas position from drag start to avoid JS interop on every move
			var index = GetColumnIndexAtPoint(args.ClientX);
			// Use Math.Min of stored indices so right-to-left selections don't block the end handle
			var fixedStart = Math.Min(_selectionStartIndex, _selectionEndIndex);
			if (index >= fixedStart)
			{
				_ = SetSelectionFromDrag(fixedStart, index).ConfigureAwait(true);
			}
		}
	}

	private async Task OnSelectionEndPointerUp()
	{
		if (_isSelectionEndDragging)
		{
			_isSelectionEndDragging = false;
			await EndSelectionChangeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnSelectionStartPointerDown(PointerEventArgs args)
	{
		if (IsEnabled && Options.Selection.CanChangeStart && !_isSelectionEndDragging)
		{
			await SuspendFollowNowAsync().ConfigureAwait(true);
			_isSelectionStartDragging = true;
			await BeginHandleDragAsync(args.PointerId, SvgSelectionHandleStart).ConfigureAwait(true);
		}
	}

	private void OnSelectionStartPointerMove(PointerEventArgs args)
	{
		if (_isSelectionStartDragging)
		{
			var index = GetColumnIndexAtPoint(args.ClientX);
			// Use Math.Max of stored indices so right-to-left selections don't block the start handle
			var fixedEnd = Math.Max(_selectionStartIndex, _selectionEndIndex);
			if (index <= fixedEnd)
			{
				_ = SetSelectionFromDrag(index, fixedEnd).ConfigureAwait(true);
			}
		}
	}

	private async Task OnSelectionStartPointerUp()
	{
		if (_isSelectionStartDragging)
		{
			_isSelectionStartDragging = false;
			await EndSelectionChangeAsync().ConfigureAwait(true);
		}
	}
}
