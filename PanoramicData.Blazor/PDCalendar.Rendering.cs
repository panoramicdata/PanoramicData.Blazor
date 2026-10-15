using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// Text, classes and geometry used by the markup.
/// </summary>
public partial class PDCalendar<TItem>
{
	private string TitleId => $"{_id}-title";

	private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

	private string GetRootClass()
		=> $"pdcal pdcal-view-{_view.ToString().ToLowerInvariant()} {CssClass}".TrimEnd();

	private string CellId(DateOnly date) => $"{_id}-{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";

	private string SlotId(DateOnly date, int rowIndex) => $"{CellId(date)}-{rowIndex}";

	private string ActiveDescendantId
	{
		get
		{
			return _view == CalendarView.Week ? SlotId(_date, _focusedRow) : CellId(_date);
		}
	}

	private static string GetViewName(CalendarView view) => view.ToString();

	private string GetPeriodName()
	{
		return _view switch
		{
			CalendarView.Month => "month",
			CalendarView.Week => "week",
			_ => $"{Math.Max(1, AgendaDays)} days"
		};
	}

	private static string LongDate(DateOnly date) => date.ToString("dddd d MMMM yyyy", CultureInfo.CurrentCulture);

	private string GetDayClass(DateOnly date)
	{
		var isOutsideMonth = _view == CalendarView.Month && date.Month != _date.Month;
		var isShadedWeekend = ShadeWeekends && IsWeekend(date);

		return JoinClasses(
			"pdcal-day",
			(isOutsideMonth, "pdcal-day-outside"),
			(date == _today, "pdcal-today"),
			(isShadedWeekend, "pdcal-weekend"),
			(IsDateDisabled(date), "pdcal-day-disabled"),
			(date == _date, "pdcal-focused"),
			(true, DayCssClassSelector?.Invoke(date)));
	}

	/// <summary>
	/// Joins a base class with each optional class whose condition holds and which is not blank.
	/// </summary>
	private static string JoinClasses(string baseClass, params (bool Applies, string? CssClass)[] optional)
	{
		var classes = optional
			.Where(entry => entry.Applies && !string.IsNullOrWhiteSpace(entry.CssClass))
			.Select(entry => entry.CssClass!)
			.Prepend(baseClass);

		return string.Join(' ', classes);
	}

	private string GetDayLabel(DateOnly date)
	{
		var count = GetSegments(date).Count;
		var label = $"{LongDate(date)}, {count} {(count == 1 ? "item" : "items")}";
		return date == _today ? $"{label}, today" : label;
	}

	private string GetItemClass(CalendarSegment<TItem> segment)
	{
		return JoinClasses(
			"pdcal-item",
			(IsSelected(segment.Item), "pdcal-item-selected"),
			(IsItemDisabled(segment.Item), "pdcal-item-disabled"),
			(segment.ContinuesBefore, "pdcal-item-continues-before"),
			(segment.ContinuesAfter, "pdcal-item-continues-after"),
			(true, CssClassSelector?.Invoke(segment.Item)));
	}

	private string GetItemTitle(TItem item)
	{
		return TitleSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty;
	}

	private string FormatTime(DateTimeOffset instant)
		=> TimeZoneInfo.ConvertTime(instant, TimeZone).ToString(TimeFormat, CultureInfo.CurrentCulture);

	/// <summary>
	/// Describes the times of a segment within its day, noting where the item carries on from or to another day.
	/// </summary>
	private string GetTimeText(CalendarSegment<TItem> segment)
	{
		return (segment.ContinuesBefore, segment.ContinuesAfter) switch
		{
			(true, true) => "All day",
			(true, false) => $"Until {FormatTime(segment.End)}",
			(false, true) => $"From {FormatTime(segment.Start)}",
			_ when segment.Start == segment.End => FormatTime(segment.Start),
			_ => $"{FormatTime(segment.Start)} to {FormatTime(segment.End)}"
		};
	}

	private string GetItemLabel(CalendarSegment<TItem> segment)
	{
		var label = $"{GetItemTitle(segment.Item)}, {LongDate(segment.Date)}, {GetTimeText(segment)}";
		return IsItemDisabled(segment.Item) ? $"{label}, unavailable" : label;
	}

	/// <summary>
	/// Gets the hover text for an item: the caller's tooltip, or the title with the item's whole time range.
	/// </summary>
	private string GetTooltip(CalendarSegment<TItem> segment)
	{
		var tooltip = TooltipSelector?.Invoke(segment.Item);
		if (!string.IsNullOrWhiteSpace(tooltip))
		{
			return tooltip;
		}

		var start = TimeZoneInfo.ConvertTime(StartSelector!(segment.Item), TimeZone);
		var end = TimeZoneInfo.ConvertTime(EndSelector!(segment.Item), TimeZone);
		var range = segment.IsMultiDay
			? $"{start.ToString("d MMM", CultureInfo.CurrentCulture)} {start.ToString(TimeFormat, CultureInfo.CurrentCulture)} to {end.ToString("d MMM", CultureInfo.CurrentCulture)} {end.ToString(TimeFormat, CultureInfo.CurrentCulture)}"
			: GetTimeText(segment);

		return $"{GetItemTitle(segment.Item)}, {range}";
	}

	private static string GetMoreLabel(DateOnly date, int count)
		=> $"Show all {count} items on {LongDate(date)}";

	/// <summary>
	/// Gets the label of an axis row from the first day that has the hour, adding the UTC offset where the
	/// hour occurs twice so that the two rows can be told apart.
	/// </summary>
	private string GetRowLabel(int rowIndex)
	{
		var axis = _axis!;

		// every row exists because at least one day has that hour
		var slot = Enumerable.Range(0, axis.Dates.Count)
			.Select(dayIndex => axis.GetSlot(dayIndex, rowIndex))
			.First(candidate => candidate is not null)!;

		var time = slot.LocalStart.ToString(TimeFormat, CultureInfo.CurrentCulture);
		return axis.Rows[rowIndex].IsAmbiguous ? $"{time} {FormatOffset(slot.Offset)}" : time;
	}

	private static string FormatOffset(TimeSpan offset)
	{
		var sign = offset < TimeSpan.Zero ? "-" : "+";
		return $"UTC{sign}{offset.Duration().ToString(@"hh\:mm", CultureInfo.InvariantCulture)}";
	}

	private string GetSlotLabel(DateOnly date, CalendarHourSlot? slot)
	{
		return slot is null
			? $"{LongDate(date)}, this hour does not occur on this day"
			: $"{LongDate(date)}, {FormatTime(slot.Start)}";
	}

	private string GetSlotClass(DateOnly date, CalendarHourSlot? slot, int rowIndex)
	{
		var isShadedWeekend = ShadeWeekends && IsWeekend(date);
		var isFocused = date == _date && rowIndex == _focusedRow;

		return JoinClasses(
			"pdcal-slot",
			(slot is null, "pdcal-slot-none"),
			(isShadedWeekend, "pdcal-weekend"),
			(IsDateDisabled(date), "pdcal-day-disabled"),
			(isFocused, "pdcal-focused"));
	}

	/// <summary>
	/// Gets a note for a day whose length is not 24 hours, such as "23 hours" when the clocks go forward.
	/// </summary>
	private string? GetDayLengthNote(DateOnly date)
	{
		var length = CalendarMath.GetDayLength(date, TimeZone);
		return length == CalendarDefaults.StandardDay ? null : $"{F(length.TotalHours)} hours";
	}

	/// <summary>
	/// Gets the position of a segment in its week view day column, or null when it lies outside the visible hours.
	/// </summary>
	private string? GetWeekItemStyle(int dayIndex, CalendarSegment<TItem> segment)
	{
		var axis = _axis!;
		var bounds = axis.GetBounds(dayIndex);
		if (bounds is null)
		{
			return null;
		}

		var (visibleStart, visibleEnd) = bounds.Value;
		var visible = segment.Start < visibleEnd && (segment.End > visibleStart || segment.Start >= visibleStart);
		if (!visible)
		{
			return null;
		}

		var top = axis.GetOffset(dayIndex, segment.Start, SlotHeight);
		var height = Math.Max(axis.GetOffset(dayIndex, segment.End, SlotHeight) - top, MinItemHeight);
		var width = 100d / segment.ColumnCount;
		var left = segment.Column * width;

		return string.Create(
			CultureInfo.InvariantCulture,
			$"top:{top:0.##}px;height:{height:0.##}px;left:{left:0.###}%;width:{width:0.###}%");
	}

	/// <summary>
	/// Gets the position of the current time line in a day column, or null when the current time is not in that column.
	/// </summary>
	private double? GetNowOffset(int dayIndex)
	{
		if (!NowIndicator)
		{
			return null;
		}

		var now = Clock.GetUtcNow();
		var axis = _axis!;
		if (CalendarMath.GetDate(now, TimeZone) != axis.Dates[dayIndex] || axis.FindRow(dayIndex, now) < 0)
		{
			return null;
		}

		return axis.GetOffset(dayIndex, now, SlotHeight);
	}

	private string CanvasStyle
		=> string.Create(CultureInfo.InvariantCulture, $"--pdcal-slot-height:{SlotHeight:0.##}px;height:{_axis!.Rows.Count * SlotHeight:0.##}px");
}
