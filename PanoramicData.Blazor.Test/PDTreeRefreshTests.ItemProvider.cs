using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The items, the data provider and the tree rendering shared by the <see cref="PDTree{TItem}"/> refresh tests.
/// </summary>
public partial class PDTreeRefreshTests
{
	private readonly ItemProvider _provider = new();

	/// <summary>An item in the tree under test.</summary>
	/// <param name="Id">Key.</param>
	/// <param name="ParentId">Parent key, or null at the top level.</param>
	/// <param name="Name">Display text.</param>
	public sealed record Item(string Id, string? ParentId, string Name);

	/// <summary>
	/// A provider that returns every item, or with load on demand only the children of the key in
	/// <see cref="DataRequest{TItem}.SearchText"/>, which is how <see cref="PDTree{TItem}"/> asks for them.
	/// </summary>
	private sealed class ItemProvider : DataProviderBase<Item>
	{
		public bool LoadOnDemand { get; set; }

		public List<Item> Items { get; } =
		[
			new("a", null, "Alpha"),
			new("a1", "a", "Alpha one"),
			new("a1x", "a1", "Alpha one x"),
			new("b", null, "Bravo"),
			new("b1", "b", "Bravo one")
		];

		public override Task<DataResponse<Item>> GetDataAsync(DataRequest<Item> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var items = LoadOnDemand
				? Items.Where(i => (i.ParentId ?? string.Empty) == (request.SearchText ?? string.Empty)).ToList()
				: [.. Items];
			return Task.FromResult(new DataResponse<Item>(items, items.Count));
		}

	}

	private IRenderedComponent<PDTree<Item>> RenderItemTree(bool loadOnDemand = false, bool raiseSelectionChangeOnRefresh = false)
	{
		_provider.LoadOnDemand = loadOnDemand;
		var tree = Render<PDTree<Item>>(parameters => parameters
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.KeyField, item => item.Id)
			.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty)
			.Add(p => p.TextField, item => item.Name)
			.Add(p => p.LoadOnDemand, loadOnDemand)
			.Add(p => p.AllowSelection, true)
			.Add(p => p.RaiseSelectionChangeOnRefresh, raiseSelectionChangeOnRefresh)
			.Add(p => p.SelectionChange, node => _selectionChanges.Add(node)));

		tree.WaitForAssertion(() => tree.Instance.RootNode.Nodes.Should().NotBeNullOrEmpty());
		return tree;
	}
}
