using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for the date handling of <see cref="DataProviderBase{T}"/> predicates.</summary>
/// <remarks>
/// Every event is at midday, well away from the day boundaries. The boundaries the provider builds are
/// currently shifted by the machine's UTC offset (reported separately as a suspected defect), so values near
/// midnight would make these tests depend on the time zone of whoever runs them. Midday keeps them about which
/// days the operators select, which is what they are for.
/// </remarks>
public class DataProviderBaseDateFilterTests
{
	private static readonly Event[] _events =
	[
		new() { Id = 1, When = new DateTime(2024, 5, 14, 12, 0, 0, DateTimeKind.Unspecified) },
		new() { Id = 2, When = new DateTime(2024, 5, 15, 12, 0, 0, DateTimeKind.Unspecified) },
		new() { Id = 3, When = new DateTime(2024, 5, 16, 12, 0, 0, DateTimeKind.Unspecified) },
		new() { Id = 4, When = new DateTime(2024, 7, 1, 12, 0, 0, DateTimeKind.Unspecified) }
	];

	private readonly Provider _provider = new();

	private int[] Ids(Filter filter) => [.. _events.AsQueryable().Where(_provider.ApplyFilter((Expression<Func<Event, bool>>?)null, filter)).Select(e => e.Id)];

	/// <summary>A day-precision date operator treats the value as the whole day it names.</summary>
	[Theory]
	[InlineData(FilterTypes.Equals, new[] { 2 })]
	[InlineData(FilterTypes.DoesNotEqual, new[] { 1, 3, 4 })]
	[InlineData(FilterTypes.GreaterThan, new[] { 3, 4 })]
	[InlineData(FilterTypes.GreaterThanOrEqual, new[] { 2, 3, 4 })]
	[InlineData(FilterTypes.LessThan, new[] { 1 })]
	[InlineData(FilterTypes.LessThanOrEqual, new[] { 1, 2 })]
	[InlineData(FilterTypes.In, new[] { 2 })]
	[InlineData(FilterTypes.NotIn, new[] { 1, 3, 4 })]
	public void DayOperators_CoverTheWholeDay(FilterTypes type, int[] expected)
	{
		Ids(new Filter(type, "When", "2024-05-15")).Should().Equal(expected);
	}

	/// <summary>Minute and second precision values bound the comparison at that minute or second.</summary>
	[Theory]
	[InlineData(FilterTypes.LessThan, "2024-05-15 00:00", new[] { 1 })]
	[InlineData(FilterTypes.GreaterThan, "2024-05-15 00:00:00", new[] { 2, 3, 4 })]
	[InlineData(FilterTypes.Equals, "2024-05-15 00:00:00", new int[0])]
	public void FinerPrecision_BoundsAtThatInstant(FilterTypes type, string value, int[] expected)
	{
		Ids(new Filter(type, "When", value)).Should().Equal(expected);
	}

	/// <summary>A list of dates matches any of the days listed.</summary>
	[Fact]
	public void In_SeveralDates_MatchesEachDay()
	{
		Ids(new Filter(FilterTypes.In, "When", "2024-05-14|2024-07-01")).Should().Equal(1, 4);
		Ids(new Filter(FilterTypes.NotIn, "When", "2024-05-14|2024-07-01")).Should().Equal(2, 3);
	}

	/// <summary>A date range covers whole days at each end, whichever way round the ends are given.</summary>
	[Theory]
	[InlineData("2024-05-15", "2024-05-16")]
	[InlineData("2024-05-16", "2024-05-15")]
	public void Range_CoversWholeDays(string from, string to)
	{
		Ids(new Filter(FilterTypes.Range, "When", from, to)).Should().Equal(2, 3);
	}

	private sealed class Event
	{
		public int Id { get; set; }

		public DateTime When { get; set; }
	}

	private sealed class Provider : DataProviderBase<Event>
	{
		public override Task<DataResponse<Event>> GetDataAsync(DataRequest<Event> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(new DataResponse<Event>([.. _events], _events.Length));
		}
	}
}
