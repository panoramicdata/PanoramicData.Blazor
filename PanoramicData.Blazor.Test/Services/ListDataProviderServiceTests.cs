using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="ListDataProviderService{TItem}"/>.</summary>
public class ListDataProviderServiceTests
{
	private static CancellationToken Token => TestContext.Current.CancellationToken;

	/// <summary>The parameterless constructor starts with an empty list.</summary>
	[Fact]
	public void DefaultConstructor_IsEmpty()
	{
		new ListDataProviderService<Item>().List.Should().BeEmpty();
	}

	/// <summary>The list constructor uses the supplied list itself, not a copy.</summary>
	[Fact]
	public void ListConstructor_UsesSuppliedList()
	{
		List<Item> items = [new Item()];

		new ListDataProviderService<Item>(items).List.Should().BeSameAs(items);
	}

	/// <summary>Getting data returns every item and the total count.</summary>
	[Fact]
	public async Task GetDataAsync_ReturnsAllItems()
	{
		var service = new ListDataProviderService<Item>([new Item { Name = "a" }, new Item { Name = "b" }]);

		var response = await service.GetDataAsync(new DataRequest<Item>(), Token);

		response.Items.Select(i => i.Name).Should().Equal("a", "b");
		response.TotalCount.Should().Be(2);
	}

	/// <summary>Create appends by default and inserts at an index when one is given.</summary>
	[Fact]
	public async Task CreateAsync_AppendsOrInserts()
	{
		var service = new ListDataProviderService<Item>();

		(await service.CreateAsync(new Item { Name = "a" }, Token)).Success.Should().BeTrue();
		(await service.CreateAsync(new Item { Name = "c" }, Token)).Success.Should().BeTrue();
		(await service.CreateAsync(new Item { Name = "b" }, 1, Token)).Success.Should().BeTrue();

		service.List.Select(i => i.Name).Should().Equal("a", "b", "c");
	}

	/// <summary>Creating at an index beyond the end reports the failure rather than throwing.</summary>
	[Fact]
	public async Task CreateAsync_InvalidIndex_ReportsError()
	{
		var service = new ListDataProviderService<Item>();

		var response = await service.CreateAsync(new Item(), 5, Token);

		response.Success.Should().BeFalse();
		response.ErrorMessage.Should().NotBeEmpty();
		service.List.Should().BeEmpty();
	}

	/// <summary>Delete removes a present item and reports a missing one as not found.</summary>
	[Fact]
	public async Task DeleteAsync_RemovesOrReportsNotFound()
	{
		var item = new Item();
		var service = new ListDataProviderService<Item>([item]);

		(await service.DeleteAsync(item, Token)).Success.Should().BeTrue();
		service.List.Should().BeEmpty();

		var missing = await service.DeleteAsync(item, Token);
		missing.Success.Should().BeFalse();
		missing.ErrorMessage.Should().Be("Not found");
	}

	/// <summary>Update applies each changed property to the item.</summary>
	[Fact]
	public async Task UpdateAsync_AppliesDelta()
	{
		var item = new Item { Name = "old", Age = 1 };
		var service = new ListDataProviderService<Item>([item]);

		var response = await service.UpdateAsync(item, new Dictionary<string, object?> { ["Name"] = "new", ["Age"] = 2 }, Token);

		response.Success.Should().BeTrue();
		item.Name.Should().Be("new");
		item.Age.Should().Be(2);
	}

	/// <summary>Update reports an item that is not in the list.</summary>
	[Fact]
	public async Task UpdateAsync_MissingItem_ReportsNotFound()
	{
		var service = new ListDataProviderService<Item>();

		var response = await service.UpdateAsync(new Item(), new Dictionary<string, object?> { ["Name"] = "x" }, Token);

		response.ErrorMessage.Should().Be("Not found");
	}

	/// <summary>Update reports unknown and read-only properties by name.</summary>
	[Fact]
	public async Task UpdateAsync_BadProperty_ReportsIt()
	{
		var item = new Item();
		var service = new ListDataProviderService<Item>([item]);

		(await service.UpdateAsync(item, new Dictionary<string, object?> { ["Nope"] = 1 }, Token))
			.ErrorMessage.Should().Be("Property Nope not found");
		(await service.UpdateAsync(item, new Dictionary<string, object?> { ["Upper"] = "X" }, Token))
			.ErrorMessage.Should().Be("Property Upper can not be written too");
	}

	/// <summary>A value of the wrong type is reported rather than thrown.</summary>
	[Fact]
	public async Task UpdateAsync_WrongValueType_ReportsError()
	{
		var item = new Item();
		var service = new ListDataProviderService<Item>([item]);

		var response = await service.UpdateAsync(item, new Dictionary<string, object?> { ["Age"] = "ten" }, Token);

		response.Success.Should().BeFalse();
		response.ErrorMessage.Should().NotBeEmpty();
	}

	private sealed class Item
	{
		public string Name { get; set; } = string.Empty;

		public int Age { get; set; }

		public string Upper => Name.ToUpperInvariant();
	}
}
