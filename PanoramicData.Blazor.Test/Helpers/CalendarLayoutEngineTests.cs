using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="CalendarLayoutEngine"/>.</summary>
public class CalendarLayoutEngineTests
{
	private static readonly TimeSpan _bst = TimeSpan.FromHours(1);
	private static readonly DateOnly _oct14 = new(2026, 10, 14);
	private static readonly DateOnly _oct15 = new(2026, 10, 15);
	private static readonly DateOnly _oct16 = new(2026, 10, 16);

	private sealed record Item(string Name, DateTimeOffset Start, DateTimeOffset End);

	private static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0, _bst);

	private static IReadOnlyList<CalendarSegment<Item>> Split(IEnumerable<Item>? items, params DateOnly[] dates)
		=> CalendarLayoutEngine.SplitByDay(items, i => i.Start, i => i.End, dates, CalendarTestZones.London);

	/// <summary>An item within one day produces one whole segment.</summary>
	[Fact]
	public void SplitByDay_ItemWithinADay_IsOneSegment()
	{
		var item = new Item("A", At(15, 9), At(15, 10));

		var segments = Split([item], _oct14, _oct15, _oct16);

		segments.Should().ContainSingle();
		segments[0].Should().Be(new CalendarSegment<Item>(item, _oct15, item.Start, item.End, false, false));
		segments[0].IsMultiDay.Should().BeFalse();
		segments[0].Column.Should().Be(0);
		segments[0].ColumnCount.Should().Be(1);
	}

	/// <summary>An item spanning midnight is split at midnight.</summary>
	[Fact]
	public void SplitByDay_ItemSpanningMidnight_IsSplitAtMidnight()
	{
		var item = new Item("Late show", At(14, 22), At(15, 2));

		var segments = Split([item], _oct14, _oct15, _oct16);

		segments.Should().HaveCount(2);
		segments[0].Date.Should().Be(_oct14);
		segments[0].End.Should().Be(At(15, 0));
		segments[0].ContinuesAfter.Should().BeTrue();
		segments[0].ContinuesBefore.Should().BeFalse();
		segments[1].Date.Should().Be(_oct15);
		segments[1].Start.Should().Be(At(15, 0));
		segments[1].ContinuesBefore.Should().BeTrue();
		segments[1].ContinuesAfter.Should().BeFalse();
		segments.Should().OnlyContain(segment => segment.IsMultiDay);
	}

	/// <summary>An item spanning several days covers each whole day in between.</summary>
	[Fact]
	public void SplitByDay_ItemSpanningDays_CoversEachDay()
	{
		var item = new Item("Festival", At(14, 12), At(16, 12));

		var segments = Split([item], _oct14, _oct15, _oct16);

		segments.Select(segment => segment.Date).Should().Equal(_oct14, _oct15, _oct16);
		segments[1].Start.Should().Be(At(15, 0));
		segments[1].End.Should().Be(At(16, 0));
		segments[1].ContinuesBefore.Should().BeTrue();
		segments[1].ContinuesAfter.Should().BeTrue();
	}

	/// <summary>An item ending exactly at midnight does not appear on the next day; one starting at midnight does.</summary>
	[Fact]
	public void SplitByDay_MidnightBoundaries_BelongToTheLaterDay()
	{
		var endsAtMidnight = new Item("Ends", At(14, 23), At(15, 0));
		var startsAtMidnight = new Item("Starts", At(15, 0), At(15, 1));
		var instantAtMidnight = new Item("Instant", At(15, 0), At(15, 0));

		var segments = Split([endsAtMidnight, startsAtMidnight, instantAtMidnight], _oct14, _oct15);

		segments.Where(segment => segment.Item == endsAtMidnight).Select(segment => segment.Date).Should().Equal(_oct14);
		segments.Where(segment => segment.Item == startsAtMidnight).Select(segment => segment.Date).Should().Equal(_oct15);
		segments.Where(segment => segment.Item == instantAtMidnight).Select(segment => segment.Date).Should().Equal(_oct15);
	}

	/// <summary>Items outside the days, or ending at their start, are left out.</summary>
	[Fact]
	public void SplitByDay_ItemsOutsideTheRange_AreLeftOut()
	{
		var before = new Item("Before", At(13, 9), At(13, 10));
		var endsAtStart = new Item("Ends at start", At(13, 22), At(15, 0));
		var after = new Item("After", At(16, 0), At(16, 1));
		var instantAtStart = new Item("Instant at start", At(15, 0), At(15, 0));

		var segments = Split([before, endsAtStart, after, instantAtStart], _oct15);

		segments.Select(segment => segment.Item).Should().Equal(instantAtStart);
	}

	/// <summary>An end before the start is treated as the start.</summary>
	[Fact]
	public void SplitByDay_EndBeforeStart_IsTreatedAsAnInstant()
	{
		var item = new Item("Backwards", At(15, 10), At(15, 8));

		var segment = Split([item], _oct15).Should().ContainSingle().Subject;

		segment.Start.Should().Be(At(15, 10));
		segment.End.Should().Be(At(15, 10));
	}

	/// <summary>Within a day segments are ordered by start, longer first when they start together.</summary>
	[Fact]
	public void SplitByDay_OrdersSegmentsByStartThenLongestFirst()
	{
		var late = new Item("Late", At(15, 18), At(15, 19));
		var shortEarly = new Item("Short", At(15, 9), At(15, 10));
		var longEarly = new Item("Long", At(15, 9), At(15, 12));

		Split([late, shortEarly, longEarly], _oct15).Select(segment => segment.Item).Should().Equal(longEarly, shortEarly, late);
	}

	/// <summary>No items, or no days, give no segments.</summary>
	[Fact]
	public void SplitByDay_NothingToSplit_GivesNothing()
	{
		Split(null, _oct15).Should().BeEmpty();
		Split([new Item("A", At(15, 9), At(15, 10))]).Should().BeEmpty();
	}

	/// <summary>The time zone decides which day an item falls on.</summary>
	[Fact]
	public void SplitByDay_TheTimeZoneDecidesTheDay()
	{
		var item = new Item("Midnight in London", new DateTimeOffset(2026, 10, 14, 23, 30, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 14, 23, 45, 0, TimeSpan.Zero));
		DateOnly[] dates = [_oct14, _oct15];

		CalendarLayoutEngine.SplitByDay([item], i => i.Start, i => i.End, dates, TimeZoneInfo.Utc).Single().Date.Should().Be(_oct14);
		CalendarLayoutEngine.SplitByDay([item], i => i.Start, i => i.End, dates, CalendarTestZones.London).Single().Date.Should().Be(_oct15);
	}

	/// <summary>Arguments that cannot be defaulted are required.</summary>
	[Fact]
	public void SplitByDay_RequiresSelectorsDatesAndTimeZone()
	{
		Item[] items = [];
		DateOnly[] dates = [_oct15];

		FluentActions.Invoking(() => CalendarLayoutEngine.SplitByDay(items, null!, i => i.End, dates, TimeZoneInfo.Utc)).Should().Throw<ArgumentNullException>();
		FluentActions.Invoking(() => CalendarLayoutEngine.SplitByDay(items, i => i.Start, null!, dates, TimeZoneInfo.Utc)).Should().Throw<ArgumentNullException>();
		FluentActions.Invoking(() => CalendarLayoutEngine.SplitByDay(items, i => i.Start, i => i.End, null!, TimeZoneInfo.Utc)).Should().Throw<ArgumentNullException>();
		FluentActions.Invoking(() => CalendarLayoutEngine.SplitByDay(items, i => i.Start, i => i.End, dates, null!)).Should().Throw<ArgumentNullException>();
		FluentActions.Invoking(() => CalendarLayoutEngine.PackColumns<Item>(null!, TimeSpan.Zero)).Should().Throw<ArgumentNullException>();
	}

	/// <summary>A lone item fills the column.</summary>
	[Fact]
	public void PackColumns_OneItem_FillsTheColumn()
	{
		var packed = Pack(("A", 9, 0, 10, 0));

		Columns(packed).Should().Equal(("A", 0, 1));
	}

	/// <summary>Two overlapping items sit side by side.</summary>
	[Fact]
	public void PackColumns_TwoOverlapping_SitSideBySide()
	{
		var packed = Pack(("A", 9, 0, 10, 0), ("B", 9, 30, 10, 30));

		Columns(packed).Should().Equal(("A", 0, 2), ("B", 1, 2));
	}

	/// <summary>Three mutually overlapping items take three columns.</summary>
	[Fact]
	public void PackColumns_ThreeOverlapping_TakeThreeColumns()
	{
		var packed = Pack(("A", 9, 0, 12, 0), ("B", 9, 30, 11, 0), ("C", 10, 0, 10, 30));

		Columns(packed).Should().Equal(("A", 0, 3), ("B", 1, 3), ("C", 2, 3));
	}

	/// <summary>In a chain of overlaps a column freed by an earlier item is reused, and the whole chain shares one column count.</summary>
	[Fact]
	public void PackColumns_ChainedOverlaps_ReuseFreedColumns()
	{
		var packed = Pack(("A", 9, 0, 10, 0), ("B", 9, 30, 10, 30), ("C", 10, 0, 11, 0), ("D", 10, 45, 11, 30));

		Columns(packed).Should().Equal(("A", 0, 2), ("B", 1, 2), ("C", 0, 2), ("D", 1, 2));
	}

	/// <summary>Items that only touch, and separate groups, each get the full width.</summary>
	[Fact]
	public void PackColumns_TouchingAndSeparateItems_AreNotNarrowed()
	{
		var packed = Pack(("A", 9, 0, 10, 0), ("B", 10, 0, 11, 0), ("C", 13, 0, 14, 0), ("D", 13, 30, 14, 30));

		Columns(packed).Should().Equal(("A", 0, 1), ("B", 0, 1), ("C", 0, 2), ("D", 1, 2));
	}

	/// <summary>Short items are treated as lasting the minimum duration, so they do not draw over one another.</summary>
	[Fact]
	public void PackColumns_ShortItems_UseTheMinimumDuration()
	{
		var instants = Pack(TimeSpan.FromMinutes(30), ("A", 10, 0, 10, 0), ("B", 10, 15, 10, 20));
		Columns(instants).Should().Equal(("A", 0, 2), ("B", 1, 2));

		var noMinimum = Pack(TimeSpan.FromMinutes(-5), ("A", 10, 0, 10, 0), ("B", 10, 15, 10, 20));
		Columns(noMinimum).Should().Equal(("A", 0, 1), ("B", 0, 1));
	}

	/// <summary>Nothing to pack gives nothing.</summary>
	[Fact]
	public void PackColumns_Empty_GivesNothing()
	{
		CalendarLayoutEngine.PackColumns(Array.Empty<CalendarSegment<Item>>(), TimeSpan.Zero).Should().BeEmpty();
	}

	private static IReadOnlyList<CalendarSegment<Item>> Pack(params (string Name, int StartHour, int StartMinute, int EndHour, int EndMinute)[] items)
		=> Pack(TimeSpan.Zero, items);

	private static IReadOnlyList<CalendarSegment<Item>> Pack(TimeSpan minimum, params (string Name, int StartHour, int StartMinute, int EndHour, int EndMinute)[] items)
	{
		// supply the segments in reverse to show the packer does its own ordering
		var segments = items
			.Reverse()
			.Select(i => new Item(i.Name, At(15, i.StartHour, i.StartMinute), At(15, i.EndHour, i.EndMinute)))
			.Select(i => new CalendarSegment<Item>(i, _oct15, i.Start, i.End, false, false));

		return CalendarLayoutEngine.PackColumns(segments, minimum);
	}

	private static IEnumerable<(string, int, int)> Columns(IEnumerable<CalendarSegment<Item>> segments)
		=> segments.Select(segment => (segment.Item.Name, segment.Column, segment.ColumnCount));
}
