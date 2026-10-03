using AwesomeAssertions;
using PanoramicData.Blazor.Attributes;
using PanoramicData.Blazor.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="DataProviderBase{T}"/>.</summary>
public partial class DataProviderBaseTests
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
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();
			List<Item> items = IncludeNullItem ? [.. _items, null!] : [.. _items];
			return Task.FromResult(new DataResponse<Item>(items, items.Count));
		}

		public void Apply(Item item, IDictionary<string, object?> delta) => ApplyDelta(item, delta);
	}
}
