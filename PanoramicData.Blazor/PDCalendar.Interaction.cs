using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// Mouse and keyboard interaction.
/// </summary>
public partial class PDCalendar<TItem>
{
	private async Task OnItemClickAsync(TItem item)
	{
		if (IsItemDisabled(item))
		{
			return;
		}

		var changed = !IsSelected(item);
		_selected = item;

		if (changed)
		{
			await SelectedItemChanged.InvokeAsync(item).ConfigureAwait(true);
		}

		await OnItemSelected.InvokeAsync(item).ConfigureAwait(true);
	}

	private async Task OnItemDoubleClickAsync(TItem item)
	{
		if (!IsItemDisabled(item))
		{
			await OnItemDoubleClick.InvokeAsync(item).ConfigureAwait(true);
		}
	}

	private async Task OnDayClickAsync(DateOnly date)
	{
		if (IsDateDisabled(date))
		{
			return;
		}

		await SetDateAsync(date).ConfigureAwait(true);
		await OnSlotSelected.InvokeAsync(CalendarMath.GetStartOfDay(date, TimeZone)).ConfigureAwait(true);
	}

	private async Task OnSlotClickAsync(int dayIndex, int rowIndex)
	{
		var date = _dates[dayIndex];
		var slot = _axis!.GetSlot(dayIndex, rowIndex);
		if (slot is null || IsDateDisabled(date))
		{
			return;
		}

		_focusedRow = rowIndex;
		await SetDateAsync(date).ConfigureAwait(true);
		await OnSlotSelected.InvokeAsync(slot.Start).ConfigureAwait(true);
	}

	private async Task OnKeyDownAsync(KeyboardEventArgs args)
	{
		var action = GetKeyAction(args.Key);

		// the keys the calendar handles must not also scroll the page
		_preventDefaultKey = action is not null;

		if (action is not null)
		{
			await action().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Gets what a key does, or null for a key the calendar does not handle.
	/// </summary>
	private Func<Task>? GetKeyAction(string key)
	{
		return key switch
		{
			"ArrowLeft" => () => MoveFocusAsync(-1),
			"ArrowRight" => () => MoveFocusAsync(1),
			"ArrowUp" => () => MoveFocusVerticallyAsync(-1),
			"ArrowDown" => () => MoveFocusVerticallyAsync(1),
			"PageUp" => () => NavigateAsync(-1),
			"PageDown" => () => NavigateAsync(1),
			"Home" => GoToTodayAsync,
			"Enter" or " " => SelectFocusedSlotAsync,
			_ => null
		};
	}

	/// <summary>
	/// Moves the focused date by a number of days, staying within the date bounds.
	/// </summary>
	private async Task MoveFocusAsync(int days)
	{
		var target = _date.AddDays(days);
		if (!IsDateDisabled(target))
		{
			await SetDateAsync(target).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Moves the focus up or down: by an hour in the week view, by a week in the month view and by a day in the agenda.
	/// </summary>
	private async Task MoveFocusVerticallyAsync(int direction)
	{
		switch (_view)
		{
			case CalendarView.Week:
				_focusedRow = Math.Clamp(_focusedRow + direction, 0, Math.Max(0, _axis!.Rows.Count - 1));
				break;
			case CalendarView.Month:
				await MoveFocusAsync(direction * CalendarMath.DaysPerWeek).ConfigureAwait(true);
				break;
			default:
				await MoveFocusAsync(direction).ConfigureAwait(true);
				break;
		}
	}

	private async Task SelectFocusedSlotAsync()
	{
		if (_view == CalendarView.Week)
		{
			// the focused date is always one of the week's seven columns
			await OnSlotClickAsync(_date.DayNumber - _dates[0].DayNumber, _focusedRow).ConfigureAwait(true);
			return;
		}

		await OnDayClickAsync(_date).ConfigureAwait(true);
	}

	private bool IsSelected(TItem item)
	{
		if (_selected is null)
		{
			return false;
		}

		return KeySelector is null
			? EqualityComparer<TItem>.Default.Equals(item, _selected)
			: Equals(KeySelector(item), KeySelector(_selected));
	}

	private bool IsItemDisabled(TItem item)
	{
		return IsDisabledSelector?.Invoke(item) == true;
	}
}
