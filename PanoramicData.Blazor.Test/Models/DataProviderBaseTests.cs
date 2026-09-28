using AwesomeAssertions;
using PanoramicData.Blazor.Attributes;
using PanoramicData.Blazor.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="DataProviderBase{T}"/>.</summary>
public class DataProviderBaseTests
{
	private static readonly Item[] _items =
	[
		new() { Id = 1, Name = "Alpha", Score = 10, Owner = new Owner { Name = "Ann" } },
		new() { Id = 2, Name = "Beta", Score = 20, Owner = null },
		new() { Id = 3, Name = "Gamma", Score = 30, Owner = new Owner { Name = "Ann" } },
		new() { Id = 4, Name = "Delta", Score = 20, Owner = new Owner { Name = "Bob" } }
	];

	private readonly Provider _provider = new();

	private int[] Ids(Filter filter) => [.. _items.AsQueryable().Where(_provider.ApplyFilter((Expression<Func<Item, bool>>?)null, filter)).Select(i => i.Id)];

	/// <summary>The unimplemented members of the base class throw until a subclass overrides them.</summary>
	[Fact]
	public async Task DefaultMembers_ThrowNotImplemented()
	{
		var bare = new BareProvider();
		var token = TestContext.Current.CancellationToken;

		await ((Func<Task>)(() => bare.CreateAsync(new Item(), token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => bare.DeleteAsync(new Item(), token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => bare.UpdateAsync(new Item(), new Dictionary<string, object?>(), token))).Should().ThrowAsync<NotImplementedException>();
		await ((Func<Task>)(() => bare.GetDataAsync(new DataRequest<Item>(), token))).Should().ThrowAsync<NotImplementedException>();
	}

	/// <summary>Applying a delta sets each named property and ignores names that are not properties.</summary>
	[Fact]
	public void ApplyDelta_SetsProperties()
	{
		var item = new Item { Name = "old" };

		_provider.Apply(item, new Dictionary<string, object?> { ["Name"] = "new", ["Score"] = 5, ["Missing"] = 1 });

		item.Name.Should().Be("new");
		item.Score.Should().Be(5);
	}

	/// <summary>A delta value of the wrong type is reported as an argument error naming the property.</summary>
	[Fact]
	public void ApplyDelta_WrongType_ThrowsArgumentException()
	{
		var act = () => _provider.Apply(new Item(), new Dictionary<string, object?> { ["Score"] = "ten" });

		act.Should().Throw<ArgumentException>().WithMessage("Error applying delta to Score:*").WithInnerException<ArgumentException>();
	}

	/// <summary>Distinct values are the sorted, de-duplicated, non-null results of the field, skipping items where it throws.</summary>
	[Fact]
	public async Task GetDistinctValuesAsync_ReturnsSortedDistinctValues()
	{
		(await _provider.GetDistinctValuesAsync(new DataRequest<Item>(), x => x.Score)).Should().Equal(10, 20, 30);
		(await _provider.GetDistinctValuesAsync(new DataRequest<Item>(), x => x.Owner!.Name)).Should().Equal("Ann", "Bob");
	}

	/// <summary>Null items in the data are skipped when collecting distinct values.</summary>
	[Fact]
	public async Task GetDistinctValuesAsync_SkipsNullItems()
	{
		var provider = new Provider { IncludeNullItem = true };

		(await provider.GetDistinctValuesAsync(new DataRequest<Item>(), x => x.Name)).Should().Equal("Alpha", "Beta", "Delta", "Gamma");
	}

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

	private sealed class Owner
	{
		public string Name { get; set; } = string.Empty;
	}

	private sealed class Item
	{
		[Display(ShortName = "ident")]
		public int Id { get; set; }

		public string Name { get; set; } = string.Empty;

		[FilterKey("pts")]
		public int Score { get; set; }

		public Owner? Owner { get; set; }
	}

	private sealed class BareProvider : DataProviderBase<Item>
	{
	}

	private sealed class Provider : DataProviderBase<Item>
	{
		public bool IncludeNullItem { get; init; }

		public override Task<DataResponse<Item>> GetDataAsync(DataRequest<Item> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			List<Item> items = IncludeNullItem ? [.. _items, null!] : [.. _items];
			return Task.FromResult(new DataResponse<Item>(items, items.Count));
		}

		public void Apply(Item item, IDictionary<string, object?> delta) => ApplyDelta(item, delta);
	}
}
