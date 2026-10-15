namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Date and time zone arithmetic for <see cref="PDCalendar{TItem}"/>: which days each view shows, where a
/// day starts and ends in a given time zone, and how navigation moves between periods.
/// </summary>
/// <remarks>
/// This class deliberately contains no Blazor types, so that the calendar's arithmetic, which is the
/// part most likely to be wrong around daylight saving changes, can be tested without a renderer.
/// </remarks>
public static class CalendarMath
{
	/// <summary>
	/// The number of days in a week.
	/// </summary>
	public static int DaysPerWeek { get; } = 7;

	/// <summary>
	/// Gets the first day of the week containing a date.
	/// </summary>
	/// <param name="date">The date.</param>
	/// <param name="firstDayOfWeek">The day on which weeks start.</param>
	/// <returns>The date of the start of the week, on or before <paramref name="date"/>.</returns>
	public static DateOnly GetStartOfWeek(DateOnly date, DayOfWeek firstDayOfWeek)
	{
		var difference = ((int)date.DayOfWeek - (int)firstDayOfWeek + DaysPerWeek) % DaysPerWeek;
		return date.AddDays(-difference);
	}

	/// <summary>
	/// Gets the seven days of the week containing a date.
	/// </summary>
	/// <param name="date">The date.</param>
	/// <param name="firstDayOfWeek">The day on which weeks start.</param>
	/// <returns>The days of the week, in order.</returns>
	public static IReadOnlyList<DateOnly> GetWeekDates(DateOnly date, DayOfWeek firstDayOfWeek)
	{
		var start = GetStartOfWeek(date, firstDayOfWeek);
		return [.. Enumerable.Range(0, DaysPerWeek).Select(start.AddDays)];
	}

	/// <summary>
	/// Gets the whole weeks that cover the month containing a date: four, five or six rows of seven days,
	/// including the days of the previous and next months that complete the first and last weeks.
	/// </summary>
	/// <param name="date">Any date in the month.</param>
	/// <param name="firstDayOfWeek">The day on which weeks start.</param>
	/// <returns>The weeks, each of seven days in order.</returns>
	public static IReadOnlyList<IReadOnlyList<DateOnly>> GetMonthWeeks(DateOnly date, DayOfWeek firstDayOfWeek)
	{
		var firstOfMonth = new DateOnly(date.Year, date.Month, 1);
		var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
		var weeks = new List<IReadOnlyList<DateOnly>>();

		for (var weekStart = GetStartOfWeek(firstOfMonth, firstDayOfWeek); weekStart <= lastOfMonth; weekStart = weekStart.AddDays(DaysPerWeek))
		{
			weeks.Add(GetWeekDates(weekStart, firstDayOfWeek));
		}

		return weeks;
	}

	/// <summary>
	/// Gets every day shown by a view.
	/// </summary>
	/// <param name="view">The view.</param>
	/// <param name="date">The focused date.</param>
	/// <param name="firstDayOfWeek">The day on which weeks start.</param>
	/// <param name="agendaDays">The number of days the agenda view lists.</param>
	/// <returns>The days, in order.</returns>
	public static IReadOnlyList<DateOnly> GetVisibleDates(CalendarView view, DateOnly date, DayOfWeek firstDayOfWeek, int agendaDays)
	{
		return view switch
		{
			CalendarView.Month => [.. GetMonthWeeks(date, firstDayOfWeek).SelectMany(week => week)],
			CalendarView.Week => GetWeekDates(date, firstDayOfWeek),
			_ => [.. Enumerable.Range(0, Math.Max(1, agendaDays)).Select(date.AddDays)]
		};
	}

	/// <summary>
	/// Gets the span of time shown by a view, from the start of its first day to the start of the day after its last.
	/// </summary>
	/// <param name="view">The view.</param>
	/// <param name="date">The focused date.</param>
	/// <param name="firstDayOfWeek">The day on which weeks start.</param>
	/// <param name="agendaDays">The number of days the agenda view lists.</param>
	/// <param name="timeZone">The time zone that decides where days start.</param>
	/// <returns>The inclusive start and exclusive end of the range.</returns>
	public static (DateTimeOffset From, DateTimeOffset To) GetVisibleRange(
		CalendarView view,
		DateOnly date,
		DayOfWeek firstDayOfWeek,
		int agendaDays,
		TimeZoneInfo timeZone)
	{
		var dates = GetVisibleDates(view, date, firstDayOfWeek, agendaDays);
		return (GetStartOfDay(dates[0], timeZone), GetStartOfDay(dates[^1].AddDays(1), timeZone));
	}

	/// <summary>
	/// Moves a date forward or back by whole periods of a view: months, weeks or agenda pages.
	/// </summary>
	/// <param name="view">The view whose period is used.</param>
	/// <param name="date">The date to move.</param>
	/// <param name="periods">The number of periods to move; negative to go back.</param>
	/// <param name="agendaDays">The number of days the agenda view lists.</param>
	/// <returns>The moved date. Moving by months keeps the day of the month where it can, else uses the last day of the month.</returns>
	public static DateOnly Navigate(CalendarView view, DateOnly date, int periods, int agendaDays)
	{
		return view switch
		{
			CalendarView.Month => date.AddMonths(periods),
			CalendarView.Week => date.AddDays(periods * DaysPerWeek),
			_ => date.AddDays(periods * Math.Max(1, agendaDays))
		};
	}

	/// <summary>
	/// Limits a date to optional bounds.
	/// </summary>
	/// <param name="date">The date.</param>
	/// <param name="minDate">The earliest allowed date, if any.</param>
	/// <param name="maxDate">The latest allowed date, if any.</param>
	/// <returns>The date, moved inside the bounds if it was outside them.</returns>
	public static DateOnly Clamp(DateOnly date, DateOnly? minDate, DateOnly? maxDate)
	{
		if (maxDate.HasValue && date > maxDate.Value)
		{
			date = maxDate.Value;
		}

		if (minDate.HasValue && date < minDate.Value)
		{
			date = minDate.Value;
		}

		return date;
	}

	/// <summary>
	/// Gets the day, in a time zone, on which an instant falls.
	/// </summary>
	/// <param name="instant">The instant.</param>
	/// <param name="timeZone">The time zone.</param>
	/// <returns>The local date.</returns>
	public static DateOnly GetDate(DateTimeOffset instant, TimeZoneInfo timeZone)
	{
		ArgumentNullException.ThrowIfNull(timeZone);
		return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);
	}

	/// <summary>
	/// Converts a wall clock time in a time zone to an instant, resolving the times that daylight saving
	/// changes make missing or repeated.
	/// </summary>
	/// <param name="localDateTime">The wall clock time. Its kind is ignored.</param>
	/// <param name="timeZone">The time zone.</param>
	/// <returns>
	/// The instant. A time skipped when the clocks go forward is read with the offset in force before the
	/// change, so it lands just after the change, as a clock left unadjusted would show. A time repeated when
	/// the clocks go back resolves to its first, earlier, occurrence.
	/// </returns>
	public static DateTimeOffset ToInstant(DateTime localDateTime, TimeZoneInfo timeZone)
	{
		ArgumentNullException.ThrowIfNull(timeZone);

		var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

		if (timeZone.IsInvalidTime(local))
		{
			var offsetBefore = timeZone.GetUtcOffset(local.AddDays(-1));
			return TimeZoneInfo.ConvertTime(new DateTimeOffset(local, offsetBefore), timeZone);
		}

		if (timeZone.IsAmbiguousTime(local))
		{
			return new DateTimeOffset(local, timeZone.GetAmbiguousTimeOffsets(local).Max());
		}

		return new DateTimeOffset(local, timeZone.GetUtcOffset(local));
	}

	/// <summary>
	/// Gets the instant at which a day starts in a time zone.
	/// </summary>
	/// <param name="date">The day.</param>
	/// <param name="timeZone">The time zone.</param>
	/// <returns>The instant of local midnight, or of the first moment of the day where midnight itself is skipped.</returns>
	public static DateTimeOffset GetStartOfDay(DateOnly date, TimeZoneInfo timeZone)
		=> ToInstant(date.ToDateTime(TimeOnly.MinValue), timeZone);

	/// <summary>
	/// Gets the real elapsed length of a day in a time zone: 24 hours normally, 23 or 25 on the days the
	/// clocks change.
	/// </summary>
	/// <param name="date">The day.</param>
	/// <param name="timeZone">The time zone.</param>
	/// <returns>The length of the day.</returns>
	public static TimeSpan GetDayLength(DateOnly date, TimeZoneInfo timeZone)
		=> GetStartOfDay(date.AddDays(1), timeZone) - GetStartOfDay(date, timeZone);

	/// <summary>
	/// Divides a day into hours of real elapsed time.
	/// </summary>
	/// <param name="date">The day.</param>
	/// <param name="timeZone">The time zone.</param>
	/// <returns>One slot per elapsed hour, so 23 on the day the clocks go forward and 25 on the day they go back.</returns>
	public static IReadOnlyList<CalendarHourSlot> GetHourSlots(DateOnly date, TimeZoneInfo timeZone)
	{
		var dayStart = GetStartOfDay(date, timeZone);
		var dayEnd = GetStartOfDay(date.AddDays(1), timeZone);
		var slots = new List<CalendarHourSlot>();
		var seenHours = new Dictionary<int, int>();

		for (var start = dayStart; start < dayEnd; start = start.AddHours(1))
		{
			var end = start.AddHours(1) < dayEnd ? start.AddHours(1) : dayEnd;
			var local = TimeZoneInfo.ConvertTime(start, timeZone);
			var occurrence = seenHours.GetValueOrDefault(local.Hour);
			seenHours[local.Hour] = occurrence + 1;
			slots.Add(new CalendarHourSlot(start, end, local, occurrence));
		}

		return slots;
	}

	/// <summary>
	/// Gets the title describing the period a view shows, such as "October 2026" or "12 to 18 Oct 2026".
	/// </summary>
	/// <param name="view">The view.</param>
	/// <param name="date">The focused date.</param>
	/// <param name="firstDayOfWeek">The day on which weeks start.</param>
	/// <param name="agendaDays">The number of days the agenda view lists.</param>
	/// <param name="culture">The culture used for month names.</param>
	/// <returns>The title.</returns>
	public static string GetTitle(CalendarView view, DateOnly date, DayOfWeek firstDayOfWeek, int agendaDays, CultureInfo culture)
	{
		if (view == CalendarView.Month)
		{
			return date.ToString("MMMM yyyy", culture);
		}

		var dates = GetVisibleDates(view, date, firstDayOfWeek, agendaDays);
		return FormatDateRange(dates[0], dates[^1], culture);
	}

	/// <summary>
	/// Formats a range of days compactly, repeating the month and year only where they change, for example
	/// "12 to 18 Oct 2026", "28 Sep to 4 Oct 2026" or "29 Dec 2026 to 4 Jan 2027".
	/// </summary>
	/// <param name="first">The first day.</param>
	/// <param name="last">The last day.</param>
	/// <param name="culture">The culture used for month names.</param>
	/// <returns>The formatted range, or a single date when both days are the same.</returns>
	public static string FormatDateRange(DateOnly first, DateOnly last, CultureInfo culture)
	{
		if (first == last)
		{
			return last.ToString("d MMM yyyy", culture);
		}

		var startFormat = first.Year != last.Year
			? "d MMM yyyy"
			: first.Month != last.Month ? "d MMM" : "%d";

		return $"{first.ToString(startFormat, culture)} to {last.ToString("d MMM yyyy", culture)}";
	}
}
