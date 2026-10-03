using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTree{TItem}.RefreshAsync"/> refreshes the tree in place (issue #152). Most tests set
/// <see cref="PDTree{TItem}.RaiseSelectionChangeOnRefresh"/> to false; the default is pinned separately.
/// </summary>
/// <remarks>
/// Refresh used to collapse each node on the selected path, discard its children and reload them, then
/// re-select a new node object. That flickered, collapsed expanded branches under the path, raised
/// <see cref="PDTree{TItem}.SelectionChange"/> on every refresh even though nothing changed, and did nothing
/// at all when no node was selected. It now merges the fetched items into the existing nodes by key.
/// </remarks>
public class PDTreeRefreshTests : BunitContext
{
	private readonly ItemProvider _provider = new();
	private readonly List<TreeNode<Item>?> _selectionChanges = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDTreeRefreshTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
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

	private static TreeNode<Item> Node(IRenderedComponent<PDTree<Item>> tree, string key)
		=> tree.Instance.RootNode.Find(key) ?? throw new InvalidOperationException($"No node '{key}'.");

	private async Task ExpandAsync(IRenderedComponent<PDTree<Item>> tree, params string[] keys)
	{
		foreach (var key in keys)
		{
			var node = Node(tree, key);
			if (!node.IsExpanded)
			{
				await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(node));
			}
		}
	}

	private async Task SelectAsync(IRenderedComponent<PDTree<Item>> tree, string key)
	{
		await tree.InvokeAsync(() => tree.Instance.SelectNode(Node(tree, key), false));
		_selectionChanges.Clear();
	}

	private static Task RefreshAsync(IRenderedComponent<PDTree<Item>> tree)
		=> tree.InvokeAsync(() => tree.Instance.RefreshAsync());

	/// <summary>A refresh over unchanged data changes nothing the user can see and announces no selection change.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Refresh_OfUnchangedData_KeepsTheSelectedNodeAndExpansion_AndRaisesNoSelectionChange(bool loadOnDemand)
	{
		var tree = RenderItemTree(loadOnDemand);
		await ExpandAsync(tree, "a", "a1", "b");
		await SelectAsync(tree, "a1x");
		var selectedBefore = tree.Instance.SelectedNode;

		await RefreshAsync(tree);

		tree.Instance.SelectedNode.Should().BeSameAs(selectedBefore);
		_selectionChanges.Should().BeEmpty();
		Node(tree, "a").IsExpanded.Should().BeTrue();
		Node(tree, "a1").IsExpanded.Should().BeTrue();
		Node(tree, "b").IsExpanded.Should().BeTrue("an expanded branch beside the selected path must stay expanded");
	}

	/// <summary>A node added under an expanded parent appears, and the parent stays expanded.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Refresh_ShowsAnAddedNode_WithoutCollapsingItsParent(bool loadOnDemand)
	{
		var tree = RenderItemTree(loadOnDemand);
		await ExpandAsync(tree, "b");

		_provider.Items.Add(new Item("b2", "b", "Bravo two"));
		await RefreshAsync(tree);

		Node(tree, "b").IsExpanded.Should().BeTrue();
		Node(tree, "b").Nodes!.Select(n => n.Key).Should().Equal("b1", "b2");
		tree.Markup.Should().Contain("Bravo two");
	}

	/// <summary>A node removed from the data disappears from the tree.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Refresh_RemovesANodeThatHasGone(bool loadOnDemand)
	{
		var tree = RenderItemTree(loadOnDemand);
		await ExpandAsync(tree, "b");

		_provider.Items.RemoveAll(i => i.Id == "b1");
		await RefreshAsync(tree);

		tree.Instance.RootNode.Find("b1").Should().BeNull();
		tree.Markup.Should().NotContain("Bravo one");
	}

	/// <summary>A changed item updates its node's text in place.</summary>
	[Fact]
	public async Task Refresh_UpdatesAChangedNodesText_InPlace()
	{
		var tree = RenderItemTree();
		var before = Node(tree, "b");

		_provider.Items[_provider.Items.FindIndex(i => i.Id == "b")] = new Item("b", null, "Bravo renamed");
		await RefreshAsync(tree);

		Node(tree, "b").Should().BeSameAs(before);
		Node(tree, "b").Text.Should().Be("Bravo renamed");
		Node(tree, "b").Data!.Name.Should().Be("Bravo renamed");
	}

	/// <summary>An item whose parent changed is moved, as the same node object, under its new parent.</summary>
	[Fact]
	public async Task Refresh_MovesANodeWhoseParentChanged_KeepingTheNode()
	{
		var tree = RenderItemTree();
		var before = Node(tree, "a1x");

		_provider.Items[_provider.Items.FindIndex(i => i.Id == "a1x")] = new Item("a1x", "b", "Alpha one x");
		await RefreshAsync(tree);

		Node(tree, "a1x").Should().BeSameAs(before);
		before.ParentNode.Should().BeSameAs(Node(tree, "b"));
		Node(tree, "b").Nodes!.Select(n => n.Key).Should().Equal("a1x", "b1");
		Node(tree, "a1").Nodes.Should().BeEmpty();
	}

	/// <summary>A refresh that returns an item whose parent is not in the tree is refused.</summary>
	[Fact]
	public async Task Refresh_WithAnItemWhoseParentIsMissing_Throws()
	{
		var tree = RenderItemTree();

		_provider.Items.Add(new Item("orphan", "missing", "Orphan"));
		var act = () => RefreshAsync(tree);

		(await act.Should().ThrowAsync<PDTreeException>())
			.WithMessage("A parent item with key 'missing' could not be found");
	}

	/// <summary>A refresh that returns an item without a key is refused.</summary>
	[Fact]
	public async Task Refresh_WithAnItemWithoutAKey_Throws()
	{
		var tree = RenderItemTree();

		_provider.Items.Add(new Item(string.Empty, null, "Nameless"));
		var act = () => RefreshAsync(tree);

		(await act.Should().ThrowAsync<PDTreeException>()).WithMessage("Items must supply a key value.");
	}

	/// <summary>Removing the selected node selects its nearest surviving ancestor, announced once.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Refresh_WhenTheSelectedNodeHasGone_SelectsItsNearestSurvivingAncestor(bool loadOnDemand)
	{
		var tree = RenderItemTree(loadOnDemand);
		await ExpandAsync(tree, "a", "a1");
		await SelectAsync(tree, "a1x");

		_provider.Items.RemoveAll(i => i.Id == "a1x");
		await RefreshAsync(tree);

		tree.Instance.SelectedNode.Should().BeSameAs(Node(tree, "a1"));
		_selectionChanges.Should().ContainSingle().Which.Should().BeSameAs(Node(tree, "a1"));
	}

	/// <summary>With nothing selected, a refresh still refreshes. It used to do nothing at all.</summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Refresh_WithNothingSelected_StillRefreshes(bool loadOnDemand)
	{
		var tree = RenderItemTree(loadOnDemand);
		tree.Instance.SelectedNode.Should().BeNull();

		_provider.Items.Add(new Item("c", null, "Charlie"));
		await RefreshAsync(tree);

		tree.Instance.RootNode.Find("c").Should().NotBeNull();
		tree.Markup.Should().Contain("Charlie");
		_selectionChanges.Should().BeEmpty();
	}

	/// <summary>
	/// By default a refresh still re-announces a selected node that survives it, as earlier versions did, so a
	/// consumer that reloads a detail view on <see cref="PDTree{TItem}.SelectionChange"/> keeps working. It is now
	/// announced once, with the same node object, and the tree is not collapsed.
	/// </summary>
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Refresh_ByDefault_ReannouncesTheSurvivingSelection_Once(bool loadOnDemand)
	{
		var tree = RenderItemTree(loadOnDemand, raiseSelectionChangeOnRefresh: true);
		await ExpandAsync(tree, "a", "a1", "b");
		await SelectAsync(tree, "a1x");
		var selectedBefore = tree.Instance.SelectedNode;

		await RefreshAsync(tree);

		tree.Instance.SelectedNode.Should().BeSameAs(selectedBefore);
		_selectionChanges.Should().ContainSingle().Which.Should().BeSameAs(selectedBefore);
		Node(tree, "b").IsExpanded.Should().BeTrue();
	}

	/// <summary>Re-announcing the selection on refresh is the default, for compatibility.</summary>
	[Fact]
	public void RaiseSelectionChangeOnRefresh_IsOnByDefault()
		=> new PDTree<Item>().RaiseSelectionChangeOnRefresh.Should().BeTrue();

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
}
