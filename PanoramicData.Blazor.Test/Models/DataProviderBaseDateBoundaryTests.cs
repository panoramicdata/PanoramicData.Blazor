using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>
/// Tests that the date boundaries built by <see cref="DataProviderBase{T}"/> fall exactly where the filter value
/// says, whatever the time zone of the machine running the query (#193), and that year, month and hour values
/// filter the whole period they name (#197).
/// </summary>
/// <remarks>
/// The events sit either side of midnight on purpose: before #193 every boundary moved by the machine's UTC
/// offset, which only shows at day boundaries and only on a machine outside UTC. CI runs these on Linux in UTC,
/// so they are also run locally (BST) and in a container with <c>TZ=America/New_York</c>.
/// </remarks>
public class DataProviderBaseDateBoundaryTests
{
	private static readonly DateTime[] _whens =
	[
		new(2024, 5, 14, 23, 0, 0, DateTimeKind.Unspecified),
		new(2024, 5, 15, 9, 30, 0, DateTimeKind.Unspecified),
		new(2024, 5, 15, 18, 0, 0, DateTimeKind.Unspecified),
		new(2024, 5, 16, 0, 0, 0, DateTimeKind.Unspecified)
	];

	private static readonly Event[] _events = [.. _whens.Select((when, i) => new Event { Id = i + 1, When = when, MaybeWhen = when, Details = new Details { When = when } })];

	private static int[] Ids<TItem>(IEnumerable<TItem> items, Filter filter, Func<TItem, int> id)
	{
		var provider = new Provider<TItem>();
		return [.. items.AsQueryable().Where(provider.ApplyFilter((Expression<Func<TItem, bool>>?)null, filter)).Select(id)];
	}

	private static int[] EventIds(Filter filter) => Ids(_events, filter, e => e.Id);

	/// <summary>Each operator on a day-precision value selects exactly the events inside or outside that day (#193).</summary>
	/// <param name="type">The filter operator.</param>
	/// <param name="expected">The ids expected to match.</param>
	[Theory]
	[InlineData(FilterTypes.Equals, new[] { 2, 3 })]
	[InlineData(FilterTypes.DoesNotEqual, new[] { 1, 4 })]
	[InlineData(FilterTypes.GreaterThan, new[] { 4 })]
	[InlineData(FilterTypes.GreaterThanOrEqual, new[] { 2, 3, 4 })]
	[InlineData(FilterTypes.LessThan, new[] { 1 })]
	[InlineData(FilterTypes.LessThanOrEqual, new[] { 1, 2, 3 })]
	[InlineData(FilterTypes.In, new[] { 2, 3 })]
	[InlineData(FilterTypes.NotIn, new[] { 1, 4 })]
	public void DayValue_BoundsAtMidnight(FilterTypes type, int[] expected)
	{
		EventIds(new Filter(type, "When", "2024-05-15")).Should().Equal(expected);
	}

	/// <summary>A nullable date property is bounded the same way.</summary>
	/// <param name="type">The filter operator.</param>
	/// <param name="expected">The ids expected to match.</param>
	[Theory]
	[InlineData(FilterTypes.Equals, new[] { 2, 3 })]
	[InlineData(FilterTypes.GreaterThan, new[] { 4 })]
	[InlineData(FilterTypes.LessThan, new[] { 1 })]
	public void NullableProperty_BoundsAtMidnight(FilterTypes type, int[] expected)
	{
		EventIds(new Filter(type, "MaybeWhen", "2024-05-15")).Should().Equal(expected);
	}

	/// <summary>A property reached through a path is bounded the same way.</summary>
	[Fact]
	public void NestedProperty_BoundsAtMidnight()
	{
		EventIds(new Filter(FilterTypes.Equals, "Details", "2024-05-15") { PropertyName = "Details.When" }).Should().Equal(2, 3);
	}

	/// <summary>A range covers whole days at each end.</summary>
	[Fact]
	public void Range_BoundsAtMidnight()
	{
		EventIds(new Filter(FilterTypes.Range, "When", "2024-05-15", "2024-05-15")).Should().Equal(2, 3);
		EventIds(new Filter(FilterTypes.Range, "When", "2024-05-14", "2024-05-15")).Should().Equal(1, 2, 3);
	}

	/// <summary>A minute-precision value matches only that minute.</summary>
	[Fact]
	public void MinuteValue_MatchesThatMinute()
	{
		EventIds(new Filter(FilterTypes.Equals, "When", "2024-05-15 09:30")).Should().Equal(2);
		EventIds(new Filter(FilterTypes.Equals, "When", "2024-05-16 00:00")).Should().Equal(4);
	}

	/// <summary>An hour-precision value matches only that hour (#197).</summary>
	[Fact]
	public void HourValue_MatchesThatHour()
	{
		EventIds(new Filter(FilterTypes.Equals, "When", "2024-05-15 09")).Should().Equal(2);
		EventIds(new Filter(FilterTypes.GreaterThan, "When", "2024-05-15 09")).Should().Equal(3, 4);
	}

	/// <summary>A US-style date filters the day it names (#197).</summary>
	[Fact]
	public void UsDate_MatchesThatDay()
	{
		EventIds(new Filter(FilterTypes.Equals, "When", "05/15/2024")).Should().Equal(2, 3);
	}

	/// <summary>A year or a year and month on a date property filters the whole year or month (#197).</summary>
	/// <param name="type">The filter operator.</param>
	/// <param name="value">The filter value.</param>
	/// <param name="expected">The ids expected to match.</param>
	[Theory]
	[InlineData(FilterTypes.Equals, "2024-05", new[] { 2, 3 })]
	[InlineData(FilterTypes.DoesNotEqual, "2024-05", new[] { 1, 4, 5 })]
	[InlineData(FilterTypes.GreaterThan, "2024-05", new[] { 4, 5 })]
	[InlineData(FilterTypes.LessThanOrEqual, "2024-05", new[] { 1, 2, 3 })]
	[InlineData(FilterTypes.Equals, "2024", new[] { 1, 2, 3, 4 })]
	[InlineData(FilterTypes.GreaterThan, "2024", new[] { 5 })]
	[InlineData(FilterTypes.In, "2024-04|2024-06", new[] { 1, 4 })]
	public void YearAndMonthValues_FilterTheWholePeriod(FilterTypes type, string value, int[] expected)
	{
		DateTime[] whens =
		[
			new(2024, 4, 30, 23, 0, 0),
			new(2024, 5, 1, 0, 0, 0),
			new(2024, 5, 31, 23, 59, 59),
			new(2024, 6, 1, 0, 0, 0),
			new(2025, 1, 1, 0, 0, 0)
		];
		var events = whens.Select((when, i) => new Event { Id = i + 1, When = when });

		Ids(events, new Filter(type, "When", value), e => e.Id).Should().Equal(expected);
	}

	/// <summary>A four-digit number on a numeric property is still compared as a number, not a year.</summary>
	[Fact]
	public void FourDigitNumber_OnNumericProperty_IsNotAYear()
	{
		var items = new[] { new Numbered { Id = 1, Number = 2024 }, new Numbered { Id = 2, Number = 2025 } };

		Ids(items, new Filter(FilterTypes.Equals, "Number", "2024"), x => x.Id).Should().Equal(1);
		Ids(items, new Filter(FilterTypes.GreaterThan, "Number", "2024"), x => x.Id).Should().Equal(2);
	}

	/// <summary>A date on a <see cref="DateTimeOffset"/> property bounds at midnight UTC.</summary>
	/// <param name="type">The filter operator.</param>
	/// <param name="expected">The ids expected to match.</param>
	[Theory]
	[InlineData(FilterTypes.Equals, new[] { 2, 3 })]
	[InlineData(FilterTypes.GreaterThan, new[] { 4 })]
	[InlineData(FilterTypes.LessThan, new[] { 1 })]
	[InlineData(FilterTypes.NotIn, new[] { 1, 4 })]
	public void DateTimeOffsetProperty_BoundsAtMidnightUtc(FilterTypes type, int[] expected)
	{
		var items = new[]
		{
			new Stamped { Id = 1, At = new DateTimeOffset(2024, 5, 14, 23, 0, 0, TimeSpan.Zero) },
			new Stamped { Id = 2, At = new DateTimeOffset(2024, 5, 15, 9, 30, 0, TimeSpan.FromHours(2)) },
			new Stamped { Id = 3, At = new DateTimeOffset(2024, 5, 16, 1, 0, 0, TimeSpan.FromHours(2)) },
			new Stamped { Id = 4, At = new DateTimeOffset(2024, 5, 16, 0, 0, 0, TimeSpan.Zero) }
		};

		Ids(items, new Filter(type, "At", "2024-05-15"), x => x.Id).Should().Equal(expected);
	}

	private sealed class Event
	{
		public int Id { get; init; }

		public DateTime When { get; init; }

		public DateTime? MaybeWhen { get; init; }

		public Details Details { get; init; } = new();
	}

	private sealed class Details
	{
		public DateTime When { get; init; }
	}

	private sealed class Numbered
	{
		public int Id { get; init; }

		public int Number { get; init; }
	}

	private sealed class Stamped
	{
		public int Id { get; init; }

		public DateTimeOffset At { get; init; }
	}

	private sealed class Provider<TItem> : DataProviderBase<TItem>
	{
	}
}
