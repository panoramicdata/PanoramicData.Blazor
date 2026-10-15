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
		_preventDefaultKey = args.Key is "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown"
			or "PageUp" or "PageDown" or "Home" or "Enter" or " ";

		switch (args.Key)
		{
			case "ArrowLeft":
				await MoveFocusAsync(-1).ConfigureAwait(true);
				break;
			case "ArrowRight":
				await MoveFocusAsync(1).ConfigureAwait(true);
				break;
			case "ArrowUp":
				await MoveFocusVerticallyAsync(-1).ConfigureAwait(true);
				break;
			case "ArrowDown":
				await MoveFocusVerticallyAsync(1).ConfigureAwait(true);
				break;
			case "PageUp":
				await NavigateAsync(-1).ConfigureAwait(true);
				break;
			case "PageDown":
				await NavigateAsync(1).ConfigureAwait(true);
				break;
			case "Home":
				await GoToTodayAsync().ConfigureAwait(true);
				break;
			case "Enter" or " ":
				await SelectFocusedSlotAsync().ConfigureAwait(true);
				break;
		}
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

	private bool IsItemDisabled(TItem item) => IsDisabledSelector?.Invoke(item) == true;
}
