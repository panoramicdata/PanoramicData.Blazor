using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Models;

/// <summary>
/// The hour axis of a week view: a set of rows shared by several day columns, and the mapping from each
/// row and day to the real hour of elapsed time it represents.
/// </summary>
/// <remarks>
/// Rows are keyed by local hour and occurrence, so every column lines up even across a daylight saving
/// change. On the day the clocks go forward the skipped hour has no slot in that column; on the day they
/// go back the repeated hour gets a row of its own, which the other days leave empty. Positions are
/// computed from elapsed time, so an item is drawn exactly as long as it really lasts.
/// </remarks>
public sealed class CalendarTimeAxis
{
	private readonly Dictionary<(int Hour, int Occurrence), CalendarHourSlot>[] _slots;

	internal CalendarTimeAxis(
		IReadOnlyList<DateOnly> dates,
		IReadOnlyList<CalendarAxisRow> rows,
		Dictionary<(int Hour, int Occurrence), CalendarHourSlot>[] slots)
	{
		Dates = dates;
		Rows = rows;
		_slots = slots;
	}

	/// <summary>
	/// Gets the days shown as columns, in order.
	/// </summary>
	public IReadOnlyList<DateOnly> Dates { get; }

	/// <summary>
	/// Gets the rows of the axis, in display order.
	/// </summary>
	public IReadOnlyList<CalendarAxisRow> Rows { get; }

	/// <summary>
	/// Builds the axis for the given days.
	/// </summary>
	/// <param name="dates">The days shown as columns.</param>
	/// <param name="timeZone">The time zone that decides where each day starts and ends.</param>
	/// <param name="firstHour">The first local hour shown, from 0 to 23.</param>
	/// <param name="lastHour">The local hour at which the axis stops, from 1 to 24, exclusive.</param>
	/// <returns>The axis.</returns>
	public static CalendarTimeAxis Create(IReadOnlyList<DateOnly> dates, TimeZoneInfo timeZone, int firstHour, int lastHour)
	{
		ArgumentNullException.ThrowIfNull(dates);
		ArgumentNullException.ThrowIfNull(timeZone);

		firstHour = Math.Clamp(firstHour, 0, 23);
		lastHour = Math.Clamp(lastHour, firstHour + 1, 24);

		var slots = new Dictionary<(int Hour, int Occurrence), CalendarHourSlot>[dates.Count];
		var keys = new HashSet<(int Hour, int Occurrence)>();
		var ambiguousHours = new HashSet<int>();

		for (var dayIndex = 0; dayIndex < dates.Count; dayIndex++)
		{
			slots[dayIndex] = [];
			foreach (var slot in CalendarMath.GetHourSlots(dates[dayIndex], timeZone))
			{
				if (slot.Hour < firstHour || slot.Hour >= lastHour)
				{
					continue;
				}

				slots[dayIndex][(slot.Hour, slot.Occurrence)] = slot;
				keys.Add((slot.Hour, slot.Occurrence));
				if (slot.Occurrence > 0)
				{
					ambiguousHours.Add(slot.Hour);
				}
			}
		}

		var rows = keys
			.OrderBy(key => key.Hour)
			.ThenBy(key => key.Occurrence)
			.Select(key => new CalendarAxisRow(key.Hour, key.Occurrence, ambiguousHours.Contains(key.Hour)))
			.ToList();

		return new CalendarTimeAxis(dates, rows, slots);
	}

	/// <summary>
	/// Gets the hour slot shown in a row of a day column, or null where that hour does not occur on that day.
	/// </summary>
	/// <param name="dayIndex">The index of the day column.</param>
	/// <param name="rowIndex">The index of the row.</param>
	/// <returns>The slot, or null.</returns>
	public CalendarHourSlot? GetSlot(int dayIndex, int rowIndex)
	{
		var row = Rows[rowIndex];
		return _slots[dayIndex].TryGetValue((row.Hour, row.Occurrence), out var slot) ? slot : null;
	}

	/// <summary>
	/// Finds the row of a day column whose slot contains an instant.
	/// </summary>
	/// <param name="dayIndex">The index of the day column.</param>
	/// <param name="instant">The instant to find.</param>
	/// <returns>The row index, or -1 when the instant is not shown in that column.</returns>
	public int FindRow(int dayIndex, DateTimeOffset instant)
	{
		for (var rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
		{
			var slot = GetSlot(dayIndex, rowIndex);
			if (slot is not null && instant >= slot.Start && instant < slot.End)
			{
				return rowIndex;
			}
		}

		return -1;
	}

	/// <summary>
	/// Gets the earliest and latest instants shown in a day column.
	/// </summary>
	/// <param name="dayIndex">The index of the day column.</param>
	/// <returns>The start of the first shown slot and the end of the last, or null when the column shows no time at all.</returns>
	public (DateTimeOffset Start, DateTimeOffset End)? GetBounds(int dayIndex)
	{
		var shown = _slots[dayIndex].Values;
		if (shown.Count == 0)
		{
			return null;
		}

		return (shown.Min(slot => slot.Start), shown.Max(slot => slot.End));
	}

	/// <summary>
	/// Gets the vertical position of an instant within a day column.
	/// </summary>
	/// <param name="dayIndex">The index of the day column.</param>
	/// <param name="instant">The instant to position.</param>
	/// <param name="rowHeight">The height of one row.</param>
	/// <returns>The distance from the top of the column, clamped to the shown rows.</returns>
	/// <remarks>
	/// A row that does not occur on this day (the other days' copy of a repeated hour, say) is only
	/// counted once the instant has passed the time the rows either side of it represent, so an item
	/// running through it is drawn across it without a break.
	/// </remarks>
	public double GetOffset(int dayIndex, DateTimeOffset instant, double rowHeight)
	{
		var offset = 0d;
		var pendingEmptyRows = 0;

		for (var rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
		{
			var slot = GetSlot(dayIndex, rowIndex);
			if (slot is null)
			{
				pendingEmptyRows++;
				continue;
			}

			if (instant < slot.Start)
			{
				break;
			}

			offset += pendingEmptyRows * rowHeight;
			pendingEmptyRows = 0;

			if (instant >= slot.End)
			{
				offset += rowHeight;
				continue;
			}

			offset += (instant - slot.Start).Ticks / (double)slot.Duration.Ticks * rowHeight;
			break;
		}

		return offset;
	}
}
