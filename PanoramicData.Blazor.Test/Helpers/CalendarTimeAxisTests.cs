using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="CalendarTimeAxis"/>.</summary>
public class CalendarTimeAxisTests
{
	private const double RowHeight = 40;

	private static CalendarTimeAxis Week(int year, int month, int day, int firstHour = 0, int lastHour = 24)
		=> CalendarTimeAxis.Create(
			CalendarMath.GetWeekDates(new DateOnly(year, month, day), DayOfWeek.Monday),
			CalendarTestZones.London,
			firstHour,
			lastHour);

	private static DateTimeOffset Bst(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0, TimeSpan.FromHours(1));

	/// <summary>An ordinary week has 24 rows with an hour for every day.</summary>
	[Fact]
	public void Create_OrdinaryWeek_HasTwentyFourRows()
	{
		var axis = Week(2026, 10, 15);

		axis.Dates.Should().HaveCount(7);
		axis.Rows.Should().HaveCount(24);
		axis.Rows.Should().OnlyContain(row => row.Occurrence == 0 && !row.IsAmbiguous);
		axis.Rows.Select(row => row.Hour).Should().Equal(Enumerable.Range(0, 24));
		Enumerable.Range(0, 7).Should().OnlyContain(dayIndex => Enumerable.Range(0, 24).All(rowIndex => axis.GetSlot(dayIndex, rowIndex) != null));
	}

	/// <summary>The week the clocks go back gets a row for the repeated hour, which only that day uses.</summary>
	[Fact]
	public void Create_AutumnWeek_AddsARowForTheRepeatedHour()
	{
		var axis = Week(2026, 10, 25);

		axis.Rows.Should().HaveCount(25);
		axis.Rows[1].Should().Be(new CalendarAxisRow(1, 0, true));
		axis.Rows[2].Should().Be(new CalendarAxisRow(1, 1, true));
		axis.Rows[3].IsAmbiguous.Should().BeFalse();

		// Monday 19th has no second 01:00; Sunday 25th does, in GMT
		axis.GetSlot(0, 2).Should().BeNull();
		var repeated = axis.GetSlot(6, 2)!;
		repeated.Offset.Should().Be(TimeSpan.Zero);
		repeated.Start.Should().Be(new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.Zero));
	}

	/// <summary>The week the clocks go forward keeps 24 rows, but the skipped hour has no slot on that day.</summary>
	[Fact]
	public void Create_SpringWeek_LeavesTheSkippedHourEmpty()
	{
		var axis = Week(2026, 3, 29);

		axis.Rows.Should().HaveCount(24);
		axis.GetSlot(6, 1).Should().BeNull();
		axis.GetSlot(5, 1).Should().NotBeNull();
		axis.GetSlot(6, 2)!.Hour.Should().Be(2);
	}

	/// <summary>Only the hours from the first visible hour up to the last are shown, with out of range values clamped.</summary>
	[Theory]
	[InlineData(8, 18, 8, 10)]
	[InlineData(-5, 3, 0, 3)]
	[InlineData(30, 40, 23, 1)]
	[InlineData(10, 2, 10, 1)]
	public void Create_LimitsTheRowsToTheVisibleHours(int firstHour, int lastHour, int expectedFirst, int expectedCount)
	{
		var axis = Week(2026, 10, 15, firstHour, lastHour);

		axis.Rows.Should().HaveCount(expectedCount);
		axis.Rows[0].Hour.Should().Be(expectedFirst);
	}

	/// <summary>Positions are proportional to elapsed time and clamped to the shown rows.</summary>
	[Fact]
	public void GetOffset_IsProportionalToElapsedTime()
	{
		var axis = Week(2026, 10, 15);
		axis.GetOffset(3, Bst(15, 9, 30), RowHeight).Should().Be(9.5 * RowHeight);
		axis.GetOffset(3, Bst(15, 0), RowHeight).Should().Be(0);
		axis.GetOffset(3, Bst(16, 0), RowHeight).Should().Be(24 * RowHeight);

		var working = Week(2026, 10, 15, 8, 18);
		working.GetOffset(3, Bst(15, 9, 30), RowHeight).Should().Be(1.5 * RowHeight);
		working.GetOffset(3, Bst(15, 6), RowHeight).Should().Be(0);
		working.GetOffset(3, Bst(15, 20), RowHeight).Should().Be(10 * RowHeight);
	}

	/// <summary>On other days of the autumn week an item running through the repeated hour's row is drawn across it.</summary>
	[Fact]
	public void GetOffset_SkipsOverRowsTheDayDoesNotHave()
	{
		var axis = Week(2026, 10, 25);

		axis.GetOffset(0, Bst(19, 1, 30), RowHeight).Should().Be(1.5 * RowHeight);
		axis.GetOffset(0, Bst(19, 2), RowHeight).Should().Be(3 * RowHeight);

		// on the 25th, half past one in GMT is the second 01:30
		axis.GetOffset(6, new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero), RowHeight).Should().Be(2.5 * RowHeight);
		axis.GetOffset(6, new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero), RowHeight).Should().Be(25 * RowHeight);
	}

	/// <summary>An instant is found in the row that contains it, or not at all when it is not shown.</summary>
	[Fact]
	public void FindRow_FindsTheContainingRow()
	{
		var axis = Week(2026, 10, 15, 8, 18);

		axis.FindRow(3, Bst(15, 9, 30)).Should().Be(1);
		axis.FindRow(3, Bst(15, 7)).Should().Be(-1);
		axis.FindRow(3, Bst(15, 18)).Should().Be(-1);
	}

	/// <summary>The bounds of a column are the start of its first shown hour and the end of its last.</summary>
	[Fact]
	public void GetBounds_CoverTheShownHours()
	{
		var axis = Week(2026, 10, 15, 8, 18);

		axis.GetBounds(3).Should().Be((Bst(15, 8), Bst(15, 18)));
	}

	/// <summary>A column that shows no time at all has no bounds.</summary>
	[Fact]
	public void GetBounds_WhenTheDayHasNoShownHour_IsNull()
	{
		// only 01:00 is shown, and 29 March 2026 has no 01:00
		var axis = Week(2026, 3, 29, 1, 2);

		axis.GetBounds(6).Should().BeNull();
		axis.GetBounds(5).Should().NotBeNull();
	}

	/// <summary>The days and time zone are required.</summary>
	[Fact]
	public void Create_RequiresDatesAndTimeZone()
	{
		FluentActions.Invoking(() => CalendarTimeAxis.Create(null!, TimeZoneInfo.Utc, 0, 24)).Should().Throw<ArgumentNullException>();
		FluentActions.Invoking(() => CalendarTimeAxis.Create([], null!, 0, 24)).Should().Throw<ArgumentNullException>();
	}
}
