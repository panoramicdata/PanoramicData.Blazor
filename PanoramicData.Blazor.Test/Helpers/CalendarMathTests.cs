using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;
using System.Globalization;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="CalendarMath"/>.</summary>
public class CalendarMathTests
{
	private static readonly CultureInfo _culture = CultureInfo.InvariantCulture;

	/// <summary>The start of the week depends on the configured first day of the week.</summary>
	[Theory]
	[InlineData(DayOfWeek.Monday, 12)]
	[InlineData(DayOfWeek.Sunday, 11)]
	[InlineData(DayOfWeek.Saturday, 10)]
	[InlineData(DayOfWeek.Thursday, 15)]
	[InlineData(DayOfWeek.Friday, 9)]
	public void GetStartOfWeek_UsesTheFirstDayOfWeek(DayOfWeek firstDayOfWeek, int expectedDay)
	{
		// 15 October 2026 is a Thursday
		CalendarMath.GetStartOfWeek(new DateOnly(2026, 10, 15), firstDayOfWeek)
			.Should().Be(new DateOnly(2026, 10, expectedDay));
	}

	/// <summary>A week is seven consecutive days from the start of the week.</summary>
	[Fact]
	public void GetWeekDates_ReturnsSevenConsecutiveDays()
	{
		var dates = CalendarMath.GetWeekDates(new DateOnly(2026, 10, 15), DayOfWeek.Monday);

		dates.Should().HaveCount(7);
		dates[0].Should().Be(new DateOnly(2026, 10, 12));
		dates[6].Should().Be(new DateOnly(2026, 10, 18));
		dates.Zip(dates.Skip(1)).Should().OnlyContain(pair => pair.Second.DayNumber - pair.First.DayNumber == 1);
	}

	/// <summary>The month grid is made of whole weeks covering the month, with four, five or six rows.</summary>
	[Theory]
	[InlineData(2026, 10, DayOfWeek.Monday, 5, "2026-09-28", "2026-11-01")]
	[InlineData(2026, 10, DayOfWeek.Sunday, 5, "2026-09-27", "2026-10-31")]
	[InlineData(2026, 2, DayOfWeek.Sunday, 4, "2026-02-01", "2026-02-28")]
	[InlineData(2026, 2, DayOfWeek.Monday, 5, "2026-01-26", "2026-03-01")]
	[InlineData(2026, 8, DayOfWeek.Monday, 6, "2026-07-27", "2026-09-06")]
	public void GetMonthWeeks_CoversTheMonthInWholeWeeks(int year, int month, DayOfWeek firstDayOfWeek, int weeks, string first, string last)
	{
		var grid = CalendarMath.GetMonthWeeks(new DateOnly(year, month, 15), firstDayOfWeek);

		grid.Should().HaveCount(weeks);
		grid.Should().OnlyContain(week => week.Count == 7 && week[0].DayOfWeek == firstDayOfWeek);
		grid[0][0].Should().Be(DateOnly.Parse(first, CultureInfo.InvariantCulture));
		grid[^1][^1].Should().Be(DateOnly.Parse(last, CultureInfo.InvariantCulture));
	}

	/// <summary>Each view shows its own set of days.</summary>
	[Fact]
	public void GetVisibleDates_DependsOnTheView()
	{
		var date = new DateOnly(2026, 10, 15);

		CalendarMath.GetVisibleDates(CalendarView.Month, date, DayOfWeek.Monday, 7).Should().HaveCount(35);
		CalendarMath.GetVisibleDates(CalendarView.Week, date, DayOfWeek.Monday, 7)[0].Should().Be(new DateOnly(2026, 10, 12));

		var agenda = CalendarMath.GetVisibleDates(CalendarView.Agenda, date, DayOfWeek.Monday, 3);
		agenda.Should().Equal(date, date.AddDays(1), date.AddDays(2));
	}

	/// <summary>An agenda length below one still shows the focused day.</summary>
	[Theory]
	[InlineData(0)]
	[InlineData(-4)]
	public void GetVisibleDates_AgendaShowsAtLeastOneDay(int agendaDays)
	{
		var date = new DateOnly(2026, 10, 15);

		CalendarMath.GetVisibleDates(CalendarView.Agenda, date, DayOfWeek.Monday, agendaDays).Should().Equal(date);
	}

	/// <summary>The visible range of a week that contains the autumn clock change lasts 169 hours.</summary>
	[Fact]
	public void GetVisibleRange_AcrossTheAutumnChange_IncludesTheExtraHour()
	{
		var (from, to) = CalendarMath.GetVisibleRange(CalendarView.Week, new DateOnly(2026, 10, 22), DayOfWeek.Monday, 7, CalendarTestZones.London);

		from.Should().Be(new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.FromHours(1)));
		to.Should().Be(new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero));
		(to - from).Should().Be(TimeSpan.FromHours(169));
	}

	/// <summary>The visible range of a month runs from the first grid day to the day after the last.</summary>
	[Fact]
	public void GetVisibleRange_ForAMonth_CoversTheWholeGrid()
	{
		var (from, to) = CalendarMath.GetVisibleRange(CalendarView.Month, new DateOnly(2026, 10, 1), DayOfWeek.Sunday, 7, TimeZoneInfo.Utc);

		from.Should().Be(new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero));
		to.Should().Be(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));
	}

	/// <summary>Navigation moves by months, weeks or agenda pages.</summary>
	[Theory]
	[InlineData(CalendarView.Month, "2026-01-31", 1, 7, "2026-02-28")]
	[InlineData(CalendarView.Month, "2026-03-15", -2, 7, "2026-01-15")]
	[InlineData(CalendarView.Week, "2026-10-15", -1, 7, "2026-10-08")]
	[InlineData(CalendarView.Week, "2026-10-15", 2, 7, "2026-10-29")]
	[InlineData(CalendarView.Agenda, "2026-10-15", 2, 3, "2026-10-21")]
	[InlineData(CalendarView.Agenda, "2026-10-15", -1, 0, "2026-10-14")]
	public void Navigate_MovesByWholePeriods(CalendarView view, string date, int periods, int agendaDays, string expected)
	{
		CalendarMath.Navigate(view, DateOnly.Parse(date, CultureInfo.InvariantCulture), periods, agendaDays)
			.Should().Be(DateOnly.Parse(expected, CultureInfo.InvariantCulture));
	}

	/// <summary>Clamping moves a date inside the optional bounds.</summary>
	[Fact]
	public void Clamp_KeepsTheDateWithinTheBounds()
	{
		var min = new DateOnly(2026, 10, 1);
		var max = new DateOnly(2026, 10, 31);

		CalendarMath.Clamp(new DateOnly(2026, 9, 1), min, max).Should().Be(min);
		CalendarMath.Clamp(new DateOnly(2026, 12, 1), min, max).Should().Be(max);
		CalendarMath.Clamp(new DateOnly(2026, 10, 15), min, max).Should().Be(new DateOnly(2026, 10, 15));
		CalendarMath.Clamp(new DateOnly(1999, 1, 1), null, null).Should().Be(new DateOnly(1999, 1, 1));
	}

	/// <summary>The day of an instant is decided in the given time zone.</summary>
	[Fact]
	public void GetDate_UsesTheTimeZone()
	{
		var instant = new DateTimeOffset(2026, 10, 14, 23, 30, 0, TimeSpan.Zero);

		CalendarMath.GetDate(instant, TimeZoneInfo.Utc).Should().Be(new DateOnly(2026, 10, 14));
		CalendarMath.GetDate(instant, CalendarTestZones.London).Should().Be(new DateOnly(2026, 10, 15));
	}

	/// <summary>An ordinary local time uses the offset in force.</summary>
	[Fact]
	public void ToInstant_OrdinaryTime_UsesTheOffsetInForce()
	{
		CalendarMath.ToInstant(new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc), CalendarTestZones.London)
			.Should().Be(new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.FromHours(1)));
	}

	/// <summary>A time skipped when the clocks go forward lands just after the change.</summary>
	[Fact]
	public void ToInstant_SkippedTime_MovesForward()
	{
		var instant = CalendarMath.ToInstant(new DateTime(2026, 3, 29, 1, 30, 0), CalendarTestZones.London);

		instant.UtcDateTime.Should().Be(new DateTime(2026, 3, 29, 1, 30, 0, DateTimeKind.Utc));
		instant.Offset.Should().Be(TimeSpan.FromHours(1));
		instant.Hour.Should().Be(2);
	}

	/// <summary>A time repeated when the clocks go back resolves to its first occurrence.</summary>
	[Fact]
	public void ToInstant_RepeatedTime_UsesTheFirstOccurrence()
	{
		CalendarMath.ToInstant(new DateTime(2026, 10, 25, 1, 30, 0), CalendarTestZones.London)
			.Should().Be(new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.FromHours(1)));
	}

	/// <summary>Where midnight is skipped the day starts at the first moment that exists.</summary>
	[Fact]
	public void GetStartOfDay_WhenMidnightIsSkipped_StartsAtOneInTheMorning()
	{
		var start = CalendarMath.GetStartOfDay(new DateOnly(2026, 9, 6), CalendarTestZones.MidnightForward);

		start.UtcDateTime.Should().Be(new DateTime(2026, 9, 6, 4, 0, 0, DateTimeKind.Utc));
		TimeZoneInfo.ConvertTime(start, CalendarTestZones.MidnightForward).Hour.Should().Be(1);
		CalendarMath.GetDayLength(new DateOnly(2026, 9, 6), CalendarTestZones.MidnightForward).Should().Be(TimeSpan.FromHours(23));
	}

	/// <summary>Where midnight happens twice the day starts at the first.</summary>
	[Fact]
	public void GetStartOfDay_WhenMidnightIsRepeated_StartsAtTheFirst()
	{
		var start = CalendarMath.GetStartOfDay(new DateOnly(2026, 4, 5), CalendarTestZones.MidnightRepeated);

		start.Should().Be(new DateTimeOffset(2026, 4, 5, 0, 0, 0, TimeSpan.FromHours(-3)));
		CalendarMath.GetDayLength(new DateOnly(2026, 4, 5), CalendarTestZones.MidnightRepeated).Should().Be(TimeSpan.FromHours(25));
	}

	/// <summary>Days are 23, 24 or 25 hours long around the clock changes.</summary>
	[Theory]
	[InlineData(2026, 3, 29, 23)]
	[InlineData(2026, 10, 15, 24)]
	[InlineData(2026, 10, 25, 25)]
	public void GetDayLength_FollowsTheClockChanges(int year, int month, int day, int hours)
	{
		CalendarMath.GetDayLength(new DateOnly(year, month, day), CalendarTestZones.London).Should().Be(TimeSpan.FromHours(hours));
	}

	/// <summary>The day the clocks go back has 25 slots, with the repeated hour told apart by occurrence and offset.</summary>
	[Fact]
	public void GetHourSlots_OnTheAutumnChange_RepeatsAnHour()
	{
		var slots = CalendarMath.GetHourSlots(new DateOnly(2026, 10, 25), CalendarTestZones.London);

		slots.Should().HaveCount(25);
		slots.Select(slot => slot.Hour).Take(4).Should().Equal(0, 1, 1, 2);
		slots[1].Occurrence.Should().Be(0);
		slots[1].Offset.Should().Be(TimeSpan.FromHours(1));
		slots[2].Occurrence.Should().Be(1);
		slots[2].Offset.Should().Be(TimeSpan.Zero);
		slots.Should().OnlyContain(slot => slot.Duration == TimeSpan.FromHours(1));
		slots[^1].End.Should().Be(new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero));
	}

	/// <summary>The day the clocks go forward has 23 slots and no 01:00.</summary>
	[Fact]
	public void GetHourSlots_OnTheSpringChange_SkipsAnHour()
	{
		var slots = CalendarMath.GetHourSlots(new DateOnly(2026, 3, 29), CalendarTestZones.London);

		slots.Should().HaveCount(23);
		slots.Select(slot => slot.Hour).Take(3).Should().Equal(0, 2, 3);
		slots.Should().OnlyContain(slot => slot.Occurrence == 0);
	}

	/// <summary>The title names the month, or the range of days with month and year only where they change.</summary>
	[Theory]
	[InlineData(CalendarView.Month, "2026-10-15", 7, "October 2026")]
	[InlineData(CalendarView.Week, "2026-10-15", 7, "12 to 18 Oct 2026")]
	[InlineData(CalendarView.Week, "2026-10-01", 7, "28 Sep to 4 Oct 2026")]
	[InlineData(CalendarView.Week, "2026-12-31", 7, "28 Dec 2026 to 3 Jan 2027")]
	[InlineData(CalendarView.Agenda, "2026-10-15", 1, "15 Oct 2026")]
	[InlineData(CalendarView.Agenda, "2026-10-15", 3, "15 to 17 Oct 2026")]
	public void GetTitle_DescribesThePeriod(CalendarView view, string date, int agendaDays, string expected)
	{
		CalendarMath.GetTitle(view, DateOnly.Parse(date, CultureInfo.InvariantCulture), DayOfWeek.Monday, agendaDays, _culture)
			.Should().Be(expected);
	}

	/// <summary>A missing time zone is rejected.</summary>
	[Fact]
	public void NullTimeZone_IsRejected()
	{
		FluentActions.Invoking(() => CalendarMath.GetDate(DateTimeOffset.UnixEpoch, null!)).Should().Throw<ArgumentNullException>();
		FluentActions.Invoking(() => CalendarMath.ToInstant(DateTime.UnixEpoch, null!)).Should().Throw<ArgumentNullException>();
	}
}
