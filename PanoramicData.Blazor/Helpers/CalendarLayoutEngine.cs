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
			.Select(date => new DayBounds(date, CalendarMath.GetStartOfDay(date, timeZone), CalendarMath.GetStartOfDay(date.AddDays(1), timeZone)))
			.ToList();

		// most items fall outside a short visible range, so they are rejected before visiting each day
		var range = new DayBounds(dates[0], days.Min(day => day.Start), days.Max(day => day.End));
		var byDay = days.Select(_ => new List<CalendarSegment<TItem>>()).ToList();

		foreach (var item in items)
		{
			AddSegments(item, startSelector(item), endSelector(item), range, days, byDay);
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

	/// <summary>
	/// Adds the segments of one item to the days it touches.
	/// </summary>
	private static void AddSegments<TItem>(
		TItem item,
		DateTimeOffset start,
		DateTimeOffset end,
		DayBounds range,
		List<DayBounds> days,
		List<List<CalendarSegment<TItem>>> byDay)
	{
		var finish = end < start ? start : end;
		if (!range.Touches(start, finish))
		{
			return;
		}

		for (var dayIndex = 0; dayIndex < days.Count; dayIndex++)
		{
			var day = days[dayIndex];
			if (day.Touches(start, finish))
			{
				byDay[dayIndex].Add(day.Clip(item, start, finish));
			}
		}
	}

	/// <summary>
	/// The span of one day, or of several, in a time zone.
	/// </summary>
	private readonly record struct DayBounds(DateOnly Date, DateTimeOffset Start, DateTimeOffset End)
	{
		/// <summary>
		/// Gets whether an item from start to end belongs in this span: it overlaps it, or has no duration and
		/// occurs within it. An item ending exactly at the start does not belong.
		/// </summary>
		public bool Touches(DateTimeOffset start, DateTimeOffset end)
		{
			if (start >= End)
			{
				return false;
			}

			return end > Start || (start == end && start >= Start);
		}

		/// <summary>
		/// Gets the part of an item that falls within this span.
		/// </summary>
		public CalendarSegment<TItem> Clip<TItem>(TItem item, DateTimeOffset start, DateTimeOffset end)
		{
			var continuesBefore = start < Start;
			var continuesAfter = end > End;
			return new CalendarSegment<TItem>(
				item,
				Date,
				continuesBefore ? Start : start,
				continuesAfter ? End : end,
				continuesBefore,
				continuesAfter);
		}
	}
}
