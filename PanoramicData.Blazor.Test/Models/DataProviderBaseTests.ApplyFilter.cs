using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>
/// Filter application tests for <see cref="DataProviderBase{T}"/>.
/// </summary>
public partial class DataProviderBaseTests
{
	/// <summary>An invalid filter is rejected by both forms of ApplyFilter.</summary>
	[Fact]
	public void ApplyFilter_InvalidFilter_Throws()
	{
		var filter = new Filter(FilterTypes.Equals, "Name", string.Empty);

		((Action)(() => _provider.ApplyFilter(_items.AsQueryable(), filter))).Should().Throw<InvalidOperationException>();
		((Action)(() => _provider.ApplyFilter((Expression<Func<Item, bool>>?)null, filter))).Should().Throw<InvalidOperationException>();
	}

	/// <summary>Filtering a query uses the provider's key mappings.</summary>
	[Fact]
	public void ApplyFilter_Query_UsesKeyMappings()
	{
		_provider.KeyPropertyMappings["n"] = "Name";

		_provider.ApplyFilter(_items.AsQueryable(), new Filter(FilterTypes.Equals, "n", "Beta")).Select(i => i.Id).Should().Equal(2);
	}

	/// <summary>A predicate resolves its property from explicit mappings, a FilterKey attribute, a short name, or the key itself.</summary>
	[Fact]
	public void ApplyFilter_Predicate_ResolvesProperty()
	{
		var mapped = new Filter(FilterTypes.Equals, "n", "Gamma");
		_items.AsQueryable().Where(_provider.ApplyFilter(null, mapped, new Dictionary<string, string> { ["n"] = "Name" })).Should().ContainSingle();
		mapped.PropertyName.Should().Be("Name");

		Ids(new Filter(FilterTypes.Equals, "pts", "30")).Should().Equal(3);
		Ids(new Filter(FilterTypes.Equals, "ident", "4")).Should().Equal(4);
		Ids(new Filter(FilterTypes.Equals, "name", "Alpha")).Should().Equal(1);
	}

	/// <summary>A new predicate is combined with an existing one so both must hold.</summary>
	[Fact]
	public void ApplyFilter_Predicate_CombinesWithExisting()
	{
		Expression<Func<Item, bool>> existing = x => x.Score == 20;

		var predicate = _provider.ApplyFilter(existing, new Filter(FilterTypes.StartsWith, "Name", "D"));

		_items.AsQueryable().Where(predicate).Select(i => i.Id).Should().Equal(4);
	}

	/// <summary>Each non-date operator selects the expected items.</summary>
	[Theory]
	[InlineData(FilterTypes.Contains, "Name", "lph", new[] { 1 })]
	[InlineData(FilterTypes.DoesNotContain, "Name", "a", new int[0])]
	[InlineData(FilterTypes.DoesNotEqual, "Score", "20", new[] { 1, 3 })]
	[InlineData(FilterTypes.EndsWith, "Name", "ta", new[] { 2, 4 })]
	[InlineData(FilterTypes.StartsWith, "Name", "G", new[] { 3 })]
	[InlineData(FilterTypes.GreaterThan, "Score", "20", new[] { 3 })]
	[InlineData(FilterTypes.GreaterThanOrEqual, "Score", "20", new[] { 2, 3, 4 })]
	[InlineData(FilterTypes.LessThan, "Score", "20", new[] { 1 })]
	[InlineData(FilterTypes.LessThanOrEqual, "Score", "20", new[] { 1, 2, 4 })]
	[InlineData(FilterTypes.In, "Name", "Alpha|Delta", new[] { 1, 4 })]
	[InlineData(FilterTypes.NotIn, "Name", "Alpha|Delta", new[] { 2, 3 })]
	[InlineData(FilterTypes.Equals, "Name", "\"Beta\"", new[] { 2 })]
	public void ApplyFilter_Operators_SelectItems(FilterTypes type, string key, string value, int[] expected)
	{
		Ids(new Filter(type, key, value)).Should().Equal(expected);
	}

	/// <summary>A numeric range includes both ends.</summary>
	[Fact]
	public void ApplyFilter_NumericRange_IsInclusive()
	{
		Ids(new Filter(FilterTypes.Range, "Score", "15", "30")).Should().Equal(2, 3, 4);
	}

	/// <summary>The null and empty operators test for null and empty values.</summary>
	[Fact]
	public void ApplyFilter_NullAndEmpty()
	{
		Ids(new Filter(FilterTypes.IsNull, "Owner", string.Empty)).Should().Equal(2);
		Ids(new Filter(FilterTypes.IsNotNull, "Owner", string.Empty)).Should().Equal(1, 3, 4);
		Ids(new Filter(FilterTypes.IsEmpty, "Name", string.Empty)).Should().BeEmpty();
		Ids(new Filter(FilterTypes.IsNotEmpty, "Name", string.Empty)).Should().Equal(1, 2, 3, 4);
	}

	/// <summary>An undefined operator falls back to equality.</summary>
	[Fact]
	public void ApplyFilter_UndefinedOperator_IsEquality()
	{
		Ids(new Filter((FilterTypes)99, "Name", "Gamma")).Should().Equal(3);
	}

	/// <summary>Applying several filters to a query skips invalid and excluded ones.</summary>
	[Fact]
	public void ApplyFilters_Query_SkipsInvalidAndExcluded()
	{
		Filter[] filters =
		[
			new Filter(FilterTypes.Equals, "Score", "20"),
			new Filter(FilterTypes.StartsWith, "Name", "D"),
			new Filter(FilterTypes.Equals, "Id", string.Empty)
		];

		_provider.ApplyFilters(_items.AsQueryable(), filters).Select(i => i.Id).Should().Equal(4);
		_provider.ApplyFilters(_items.AsQueryable(), filters, "Name").Select(i => i.Id).Should().Equal(2, 4);
	}

	/// <summary>Building a predicate from several filters combines them, or matches everything when none apply.</summary>
	[Fact]
	public void ApplyFilters_Predicate_CombinesOrMatchesAll()
	{
		Filter[] filters = [new Filter(FilterTypes.Equals, "Score", "20"), new Filter(FilterTypes.StartsWith, "Name", "B")];

		_items.AsQueryable().Where(_provider.ApplyFilters(filters)).Select(i => i.Id).Should().Equal(2);
		_items.AsQueryable().Where(_provider.ApplyFilters(filters, "Score", "Name")).Should().HaveCount(4);
		_items.AsQueryable().Where(_provider.ApplyFilters([])).Should().HaveCount(4);
	}
}
