namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Lays out calendar items: divides them into per-day segments in a time zone and arranges segments
/// that overlap in time into side by side columns.
/// </summary>
/// <remarks>
/// Like <see cref="CalendarMath"/>, this class contains no Blazor types so that it can be tested directly.
/// </remarks>
public static class CalendarLayoutEngine
{
	/// <summary>
	/// Divides items into the parts that fall on each of the given days.
	/// </summary>
	/// <typeparam name="TItem">The type of the items.</typeparam>
	/// <param name="items">The items.</param>
	/// <param name="startSelector">Returns the instant an item starts.</param>
	/// <param name="endSelector">Returns the instant an item ends. An end before the start is treated as the start.</param>
	/// <param name="dates">The days to fill.</param>
	/// <param name="timeZone">The time zone that decides where each day starts and ends.</param>
	/// <returns>
	/// The segments, grouped by day in the order of <paramref name="dates"/> and ordered within a day by start,
	/// with longer segments first where they start together. An item with no duration belongs to the day on
	/// which it occurs; an item that ends exactly at midnight does not appear on the following day.
	/// </returns>
	public static IReadOnlyList<CalendarSegment<TItem>> SplitByDay<TItem>(
		IEnumerable<TItem>? items,
		Func<TItem, DateTimeOffset> startSelector,
		Func<TItem, DateTimeOffset> endSelector,
		IReadOnlyList<DateOnly> dates,
		TimeZoneInfo timeZone)
	{
		ArgumentNullException.ThrowIfNull(startSelector);
		ArgumentNullException.ThrowIfNull(endSelector);
		ArgumentNullException.ThrowIfNull(dates);
		ArgumentNullException.ThrowIfNull(timeZone);

		if (items is null || dates.Count == 0)
		{
			return [];
		}

		var days = dates
			.Select(date => (Date: date, Start: CalendarMath.GetStartOfDay(date, timeZone), End: CalendarMath.GetStartOfDay(date.AddDays(1), timeZone)))
			.ToList();

		var rangeStart = days.Min(day => day.Start);
		var rangeEnd = days.Max(day => day.End);
		var byDay = days.Select(_ => new List<CalendarSegment<TItem>>()).ToList();

		foreach (var item in items)
		{
			var start = startSelector(item);
			var end = endSelector(item);
			if (end < start)
			{
				end = start;
			}

			// most items fall outside a short visible range, so reject them before visiting each day
			if (start >= rangeEnd || end < rangeStart || (end == rangeStart && end > start))
			{
				continue;
			}

			for (var dayIndex = 0; dayIndex < days.Count; dayIndex++)
			{
				var day = days[dayIndex];
				var overlaps = start < day.End && (end > day.Start || (start == end && start >= day.Start));
				if (overlaps)
				{
					byDay[dayIndex].Add(new CalendarSegment<TItem>(
						item,
						day.Date,
						start > day.Start ? start : day.Start,
						end < day.End ? end : day.End,
						start < day.Start,
						end > day.End));
				}
			}
		}

		return [.. byDay.SelectMany(segments => segments.OrderBy(segment => segment.Start).ThenByDescending(segment => segment.End))];
	}

	/// <summary>
	/// Arranges segments that overlap in time into side by side columns, using greedy column packing: each
	/// segment takes the leftmost column that is free when it starts, and every segment in a chain of
	/// overlaps shares that chain's column count so the chain lines up.
	/// </summary>
	/// <typeparam name="TItem">The type of the items.</typeparam>
	/// <param name="segments">The segments of one day column.</param>
	/// <param name="minimumDuration">
	/// The shortest time a segment is treated as lasting, so that very short or zero length segments, which are
	/// drawn at a minimum height, are still kept apart.
	/// </param>
	/// <returns>The segments in start order, each with its <see cref="CalendarSegment{TItem}.Column"/> and <see cref="CalendarSegment{TItem}.ColumnCount"/> set.</returns>
	public static IReadOnlyList<CalendarSegment<TItem>> PackColumns<TItem>(IEnumerable<CalendarSegment<TItem>> segments, TimeSpan minimumDuration)
	{
		ArgumentNullException.ThrowIfNull(segments);

		var ordered = segments
			.OrderBy(segment => segment.Start)
			.ThenByDescending(segment => segment.End)
			.ToList();

		var result = new List<CalendarSegment<TItem>>(ordered.Count);
		var group = new List<(CalendarSegment<TItem> Segment, int Column)>();
		var columnEnds = new List<DateTimeOffset>();
		var groupEnd = DateTimeOffset.MinValue;

		foreach (var segment in ordered)
		{
			var end = GetEffectiveEnd(segment, minimumDuration);

			if (group.Count > 0 && segment.Start >= groupEnd)
			{
				FlushGroup(group, columnEnds.Count, result);
				columnEnds.Clear();
			}

			var column = columnEnds.FindIndex(columnEnd => columnEnd <= segment.Start);
			if (column < 0)
			{
				column = columnEnds.Count;
				columnEnds.Add(end);
			}
			else
			{
				columnEnds[column] = end;
			}

			group.Add((segment, column));
			groupEnd = group.Count == 1 || end > groupEnd ? end : groupEnd;
		}

		FlushGroup(group, columnEnds.Count, result);
		return result;
	}

	private static DateTimeOffset GetEffectiveEnd<TItem>(CalendarSegment<TItem> segment, TimeSpan minimumDuration)
	{
		var minimumEnd = segment.Start + (minimumDuration > TimeSpan.Zero ? minimumDuration : TimeSpan.Zero);
		return segment.End > minimumEnd ? segment.End : minimumEnd;
	}

	private static void FlushGroup<TItem>(
		List<(CalendarSegment<TItem> Segment, int Column)> group,
		int columnCount,
		List<CalendarSegment<TItem>> result)
	{
		result.AddRange(group.Select(entry => entry.Segment with { Column = entry.Column, ColumnCount = columnCount }));
		group.Clear();
	}
}
