using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>
/// Tests for <see cref="DataProviderBase{T}"/> predicates whose compared member is not a single public property,
/// and for date values given to the millisecond.
/// </summary>
public class DataProviderBaseEdgeTests
{
	private static readonly Reading[] _readings =
	[
		new() { Id = 1, Label = "2024", When = new DateTime(2023, 8, 15, 21, 26, 6, DateTimeKind.Utc) },
		new() { Id = 2, Label = "other", When = new DateTime(2023, 8, 15, 21, 26, 8, DateTimeKind.Utc) }
	];

	private readonly Provider _provider = new();

	private int[] Ids(Filter filter) => [.. _readings.AsQueryable().Where(_provider.ApplyFilter((Expression<Func<Reading, bool>>?)null, filter)).Select(r => r.Id)];

	/// <summary>A filter on a member the type does not have is refused when the predicate is built.</summary>
	[Fact]
	public void MissingMember_IsRefused()
	{
		var act = () => Ids(new Filter(FilterTypes.Equals, "Missing", "2"));

		act.Should().Throw<System.Linq.Dynamic.Core.Exceptions.ParseException>().WithMessage("*Missing*");
	}

	/// <summary>
	/// A name matching two properties when case is ignored has no single type, so a four-digit value is compared
	/// as written rather than read as a year.
	/// </summary>
	[Fact]
	public void AmbiguousProperty_ValueIsNotReadAsDate()
		=> Ids(new Filter(FilterTypes.Equals, "Label", "2024")).Should().Equal(1);

	/// <summary>Before a time given to the millisecond means before that second.</summary>
	[Fact]
	public void LessThan_MillisecondValue_ComparesWithItsSecond()
		=> Ids(new Filter(FilterTypes.LessThan, "When", "2023-08-15 21:26:07.123")).Should().Equal(1);

	/// <summary>After a time given to the millisecond means from that second on.</summary>
	[Fact]
	public void GreaterThan_MillisecondValue_ComparesWithItsSecond()
		=> Ids(new Filter(FilterTypes.GreaterThan, "When", "2023-08-15 21:26:07.123")).Should().Equal(2);

	private sealed class Reading
	{
		public int Id { get; set; }

		public string Label { get; set; } = string.Empty;

		// differs from Label only by case, so a case-insensitive lookup of "Label" is ambiguous
		public string LABEL { get; set; } = string.Empty;

		public DateTime When { get; set; }
	}

	private sealed class Provider : DataProviderBase<Reading>
	{
	}
}
