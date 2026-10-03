namespace PanoramicData.Blazor;

/// <summary>
/// Time range selection.
/// </summary>
public partial class PDTimeline
{
	private double SelectionStartX => (Math.Min(_selectionStartIndex, _selectionEndIndex) - _columnOffset) * Options.Bar.Width;

	private double SelectionEndX => ((Math.Max(_selectionStartIndex, _selectionEndIndex) - _columnOffset) * Options.Bar.Width) + Options.Bar.Width;

	/// <summary>
	/// Clears the current selection and raises selection-changed with null.
	/// </summary>
	public async Task ClearSelection()
	{
		if (_selectionRange != null)
		{
			_selectionRange = null;
			_selectionStartIndex = _selectionEndIndex = -1;
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Gets the current selection range.
	/// </summary>
	/// <returns>The selected range, or null when no selection exists.</returns>
	public TimeRange? GetSelection()
	{
		return _selectionRange;
	}

	/// <summary>
	/// Determines whether a client X coordinate lies within the current selection.
	/// </summary>
	/// <param name="clientX">Client X coordinate.</param>
	/// <returns>True when the point is within the selected range.</returns>
	[JSInvokable("PanoramicData.Blazor.PDTimeline.IsPointInSelection")]
	public bool IsPointInSelection(double clientX)
	{
		// No selection exists
		if (_selectionStartIndex == -1 || _selectionEndIndex == -1)
		{
			return false;
		}

		// Calculate which column index the point is over
		return IsColumnInSelection(GetColumnIndexAtPoint(clientX));
	}

	/// <summary>
	/// Determines whether a column lies within the current selection, whichever way round its indices are.
	/// </summary>
	private bool IsColumnInSelection(int index)
		=> _selectionRange != null
			&& index >= Math.Min(_selectionStartIndex, _selectionEndIndex)
			&& index <= Math.Max(_selectionStartIndex, _selectionEndIndex);

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

	/// <summary>
	/// Sets the current selection range.
	/// </summary>
	/// <param name="start">Selection start.</param>
	/// <param name="end">Selection end.</param>
	public async Task SetSelection(DateTime start, DateTime end)
	{
		// quit if no change
		if (_selectionRange?.StartTime == start && _selectionRange?.EndTime == end)
		{
			return;
		}

		// validate range
		(start, end) = ClampToTimeline(start, end);
		if (!Options.General.AllowDisableSelection)
		{
			(start, end) = ClampToEnabledRange(start, end);
		}

		// update selection range indexes
		_selectionStartIndex = Math.Max(0, Scale.PeriodsBetween(RoundedMinDateTime, start));

		// ensure selection is not beyond max datetime
		var maxIndex = Scale.PeriodsBetween(RoundedMinDateTime, RoundedMaxDateTime);
		_selectionEndIndex = Math.Min(Scale.PeriodsBetween(RoundedMinDateTime, end) - 1, maxIndex);

		await NotifySelectionAsync(start, end).ConfigureAwait(true);
	}

	private (DateTime Start, DateTime End) ClampToTimeline(DateTime start, DateTime end)
	{
		if (start < RoundedMinDateTime)
		{
			start = RoundedMinDateTime;
		}

		if (end > RoundedMaxDateTime)
		{
			end = RoundedMaxDateTime;
		}

		return (start, end);
	}

	private (DateTime Start, DateTime End) ClampToEnabledRange(DateTime start, DateTime end)
	{
		if (DisableAfter != DateTime.MinValue && end >= DisableAfter)
		{
			end = DisableAfter;
		}

		if (DisableBefore != DateTime.MinValue && start < DisableBefore)
		{
			start = DisableBefore;
		}

		return (start, end);
	}

	/// <summary>
	/// Stores the selection and raises <see cref="SelectionChanged"/>, if it differs from the current one.
	/// </summary>
	private async Task NotifySelectionAsync(DateTime start, DateTime end)
	{
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
		(startIndex, endIndex) = ApplyFixedSelectionEnds(startIndex, endIndex);
		_selectionStartIndex = startIndex;
		_selectionEndIndex = endIndex;

		if (startIndex < 0 || endIndex < 0)
		{
			return;
		}

		// For single column selection (startIndex == endIndex), use period start and end
		if (startIndex == endIndex)
		{
			await SelectColumnAsync(startIndex).ConfigureAwait(true);
		}
		else
		{
			await SelectColumnsAsync(startIndex, endIndex).ConfigureAwait(true);
		}
	}

	private (int Start, int End) ApplyFixedSelectionEnds(int startIndex, int endIndex)
	{
		var selection = Options.Selection;
		if (!selection.Enabled)
		{
			return (startIndex, endIndex);
		}

		if (selection.CanChangeStart && !selection.CanChangeEnd)
		{
			return (startIndex, LastSelectableIndex);
		}

		if (!selection.CanChangeStart && selection.CanChangeEnd)
		{
			return (0, endIndex);
		}

		return (startIndex, endIndex);
	}

	private int LastSelectableIndex
		=> (_totalColumns < _viewportColumns && Options.General.RightAlign ? _viewportColumns : _totalColumns) - 1;

	private async Task SelectColumnAsync(int index)
	{
		var periodStart = Scale.AddPeriods(RoundedMinDateTime, index);
		var startTime = Scale.PeriodStart(periodStart);
		var endTime = Scale.PeriodEnd(periodStart);

		// limit selection range to enabled range?
		if (!Options.General.AllowDisableSelection)
		{
			if (DisableAfter != DateTime.MinValue && endTime > DisableAfter)
			{
				endTime = DisableAfter;
			}

			if (DisableBefore != DateTime.MinValue && startTime < DisableBefore)
			{
				startTime = DisableBefore;
			}

			// If the entire period is disabled, don't create selection
			if (startTime >= endTime)
			{
				return;
			}
		}

		await NotifySelectionAsync(startTime, endTime).ConfigureAwait(true);
		StateHasChanged();
	}

	private async Task SelectColumnsAsync(int startIndex, int endIndex)
	{
		// Multi-column selection - calculate time period and sort into chronological order
		var (startTime, endTime) = GetColumnsTimeRange(startIndex, endIndex);

		// limit selection range to enabled range?
		if (!Options.General.AllowDisableSelection)
		{
			(startTime, endTime) = ClipDraggedColumnsToEnabledRange(startTime, endTime);
		}

		await NotifySelectionAsync(startTime, endTime).ConfigureAwait(true);
		StateHasChanged();
	}

	private (DateTime Start, DateTime End) GetColumnsTimeRange(int startIndex, int endIndex)
	{
		var startPeriod = Scale.AddPeriods(RoundedMinDateTime, startIndex);
		var endPeriod = Scale.AddPeriods(RoundedMinDateTime, endIndex);
		return startIndex <= endIndex
			? (startPeriod, Scale.PeriodEnd(endPeriod))
			: (Scale.PeriodStart(endPeriod), Scale.PeriodEnd(startPeriod));
	}

	private (DateTime Start, DateTime End) ClipDraggedColumnsToEnabledRange(DateTime startTime, DateTime endTime)
	{
		if (DisableAfter != DateTime.MinValue && endTime >= DisableAfter)
		{
			endTime = DisableAfter;
			_selectionEndIndex = Scale.PeriodsBetween(RoundedMinDateTime, endTime) - 1;
		}

		if (DisableBefore != DateTime.MinValue && startTime < DisableBefore)
		{
			startTime = DisableBefore;
			var index = Scale.PeriodsBetween(RoundedMinDateTime, startTime);
			if (_isChartDragging)
			{
				_selectionEndIndex = index;
			}
			else
			{
				_selectionStartIndex = index;
			}
		}

		return (startTime, endTime);
	}
}
