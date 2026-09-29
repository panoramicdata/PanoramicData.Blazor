using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the public behaviour of <see cref="PDTree{TItem}"/>: loading, selection, expansion, editing,
/// keyboard and mouse handling. Refresh is covered separately by <see cref="PDTreeRefreshTests"/>.
/// </summary>
public partial class PDTreeTests : BunitContext
{
	private readonly ItemProvider _provider = new();
	private readonly List<TreeNode<Item>> _selections = [];
	private readonly List<Exception> _exceptions = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDTreeTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDTree<Item>> RenderItemTree(Action<ComponentParameterCollectionBuilder<PDTree<Item>>>? configure = null, bool allowSelection = true)
	{
		var tree = Render<PDTree<Item>>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, _provider)
				.Add(p => p.KeyField, item => item.Id)
				.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty)
				.Add(p => p.TextField, item => item.Name)
				.Add(p => p.AllowSelection, allowSelection)
				.Add(p => p.SelectionChange, node => _selections.Add(node))
				.Add(p => p.ExceptionHandler, ex => _exceptions.Add(ex));
			configure?.Invoke(parameters);
		});
		tree.WaitForAssertion(() => tree.Instance.RootNode.Nodes.Should().NotBeNull());
		return tree;
	}

	private static TreeNode<Item> Node(IRenderedComponent<PDTree<Item>> tree, string key)
		=> tree.Instance.RootNode.Find(key) ?? throw new InvalidOperationException($"No node '{key}'.");

	private static Task SelectAsync(IRenderedComponent<PDTree<Item>> tree, string key)
		=> tree.InvokeAsync(() => tree.Instance.SelectNode(Node(tree, key), false));

	private static void Key(IRenderedComponent<PDTree<Item>> tree, string code)
		=> tree.Find("div.pdtree").KeyDown(new KeyboardEventArgs { Code = code });

	/// <summary>A missing key field is refused.</summary>
	[Fact]
	public void MissingKeyField_Throws()
	{
		var act = () => Render<PDTree<Item>>(parameters => parameters
			.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty));

		act.Should().Throw<PDTreeException>().WithMessage("KeyField*");
	}

	/// <summary>A missing parent key field is refused.</summary>
	[Fact]
	public void MissingParentKeyField_Throws()
	{
		var act = () => Render<PDTree<Item>>(parameters => parameters.Add(p => p.KeyField, item => item.Id));

		act.Should().Throw<PDTreeException>().WithMessage("ParentKeyField*");
	}

	/// <summary>The first render loads the data, sorts siblings by text, and raises ItemsLoaded, NodeUpdated and Ready.</summary>
	[Fact]
	public void FirstRender_LoadsSortedNodes_AndRaisesTheLifecycleEvents()
	{
		var loaded = 0;
		var ready = 0;
		var updated = new List<TreeNode<Item>>();
		var tree = RenderItemTree(p => p
			.Add(x => x.ItemsLoaded, (List<Item> items) => loaded = items.Count)
			.Add(x => x.NodeUpdated, (TreeNode<Item> n) => updated.Add(n))
			.Add(x => x.Ready, () => ready++));

		tree.Instance.RootNode.Nodes!.Select(n => n.Text).Should().Equal("Alpha", "Bravo");
		loaded.Should().Be(_provider.Items.Count);
		ready.Should().Be(1);
		updated.Should().ContainSingle().Which.Should().BeSameAs(tree.Instance.RootNode);
		tree.Instance.Id.Should().StartWith("pd-tree-");
		Node(tree, "a1").Level.Should().Be(2);
		Node(tree, "a1").ParentNode.Should().BeSameAs(Node(tree, "a"));
	}

	/// <summary>ItemsLoaded may add items before they are turned into nodes.</summary>
	[Fact]
	public void ItemsLoaded_CanAddItems()
	{
		var tree = RenderItemTree(p => p.Add(x => x.ItemsLoaded, (List<Item> items) => items.Add(new Item("c", null, "Charlie"))));

		tree.Instance.RootNode.Nodes!.Select(n => n.Key).Should().Equal("a", "b", "c");
	}

	/// <summary>A custom sort orders siblings, and the icon function is given each node's level.</summary>
	[Fact]
	public void SortAndIconCssClass_AreApplied()
	{
		var tree = RenderItemTree(p => p
			.Add(x => x.Sort, (x, y) => string.CompareOrdinal(y.Name, x.Name))
			.Add(x => x.IconCssClass, (Item _, int level) => $"level-{level}"));

		tree.Instance.RootNode.Nodes!.Select(n => n.Key).Should().Equal("b", "a");
		Node(tree, "a").IconCssClass.Should().Be("level-1");
		Node(tree, "a1x").IconCssClass.Should().Be("level-3");
	}

	/// <summary>Without a text field a node's text is the item's own string form.</summary>
	[Fact]
	public void NoTextField_UsesTheItemsToString()
	{
		var tree = Render<PDTree<Item>>(parameters => parameters
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.KeyField, item => item.Id)
			.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty));
		tree.WaitForAssertion(() => tree.Instance.RootNode.Nodes.Should().NotBeNull());

		Node(tree, "a").Text.Should().Be(_provider.Items[0].ToString());
	}

	/// <summary>A provider that throws is reported through ExceptionHandler and leaves the tree empty.</summary>
	[Fact]
	public void ProviderFailure_IsReportedThroughExceptionHandler()
	{
		_provider.FailWith(new InvalidOperationException("boom"));

		var tree = Render<PDTree<Item>>(parameters => parameters
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.KeyField, item => item.Id)
			.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty)
			.Add(p => p.ExceptionHandler, ex => _exceptions.Add(ex)));

		tree.WaitForAssertion(() => _exceptions.Should().ContainSingle().Which.Message.Should().Be("boom"));
		tree.FindAll(".pdtreenode").Should().BeEmpty();
	}

	/// <summary>With no data provider the tree loads empty.</summary>
	[Fact]
	public void NoDataProvider_LoadsEmpty()
	{
		var ready = 0;
		var tree = Render<PDTree<Item>>(parameters => parameters
			.Add(p => p.KeyField, item => item.Id)
			.Add(p => p.ParentKeyField, item => item.ParentId ?? string.Empty)
			.Add(p => p.Ready, () => ready++));

		tree.WaitForAssertion(() => ready.Should().Be(1));
		tree.FindAll(".pdtreenode").Should().BeEmpty();
	}

	/// <summary>With ShowRoot false a single top-level node is hidden and its children shown instead.</summary>
	[Fact]
	public void ShowRootFalse_WithASingleRoot_ShowsItsChildren()
	{
		_provider.Items.RemoveAll(i => i.Id.StartsWith('b'));

		var tree = RenderItemTree(p => p.Add(x => x.ShowRoot, false));

		tree.WaitForAssertion(() => tree.Markup.Should().Contain("Alpha one"));
		tree.Markup.Should().NotContain(">Alpha<");
	}

	/// <summary>ExpandAll and CollapseAll set every branch's expanded state; Search finds the first match.</summary>
	[Fact]
	public void ExpandAll_CollapseAll_AndSearch()
	{
		var tree = RenderItemTree();

		tree.Instance.ExpandAll();
		Node(tree, "a").IsExpanded.Should().BeTrue();
		Node(tree, "a1").IsExpanded.Should().BeTrue();
		Node(tree, "a1x").IsExpanded.Should().BeFalse("a leaf is never expanded");

		tree.Instance.CollapseAll();
		Node(tree, "a").IsExpanded.Should().BeFalse();

		tree.Instance.Search(n => n.Text.StartsWith("Bravo o", StringComparison.Ordinal))!.Key.Should().Be("b1");
		tree.Instance.Search(n => n.Text == "missing").Should().BeNull();
	}

	/// <summary>ExpandAllAsync expands only the branches the predicate allows and raises NodeExpanded for each.</summary>
	[Fact]
	public async Task ExpandAllAsync_HonoursThePredicate()
	{
		var expanded = new List<string>();
		var tree = RenderItemTree(p => p
			.Add(x => x.ExpandOnExpandAll, n => n.Key != "b")
			.Add(x => x.NodeExpanded, (TreeNode<Item> n) => expanded.Add(n.Key)));

		await tree.InvokeAsync(tree.Instance.ExpandAllAsync);

		Node(tree, "a").IsExpanded.Should().BeTrue();
		Node(tree, "a1").IsExpanded.Should().BeTrue();
		Node(tree, "b").IsExpanded.Should().BeFalse();
		expanded.Should().Contain(["a", "a1"]).And.NotContain("b");
	}

	/// <summary>Selecting a node marks it selected, expands its ancestors and raises SelectionChange.</summary>
	[Fact]
	public async Task SelectNode_SelectsAndExpandsAncestors()
	{
		var tree = RenderItemTree();

		await SelectAsync(tree, "a1x");
		await SelectAsync(tree, "b1");

		Node(tree, "a1x").IsSelected.Should().BeFalse();
		Node(tree, "b1").IsSelected.Should().BeTrue();
		Node(tree, "b").IsExpanded.Should().BeTrue();
		tree.Instance.SelectedNode!.Key.Should().Be("b1");
		_selections.Select(n => n.Key).Should().Equal("a1x", "b1");
		tree.Find(".pdtreenode_content.selected").TextContent.Should().Contain("Bravo one");
	}

	/// <summary>With selection disabled nothing is selected.</summary>
	[Fact]
	public async Task SelectNode_WhenSelectionDisabled_DoesNothing()
	{
		var tree = RenderItemTree(allowSelection: false);

		await SelectAsync(tree, "a");

		tree.Instance.SelectedNode.Should().BeNull();
		_selections.Should().BeEmpty();
	}

	/// <summary>BeforeSelectionChange can cancel a selection, and is told the old and new nodes.</summary>
	[Fact]
	public async Task BeforeSelectionChange_CanCancel()
	{
		var seen = new List<(string? New, string? Old)>();
		var tree = RenderItemTree(p => p.Add(x => x.BeforeSelectionChange, (TreeBeforeSelectionChangeEventArgs<Item> e) =>
		{
			seen.Add((e.NewNode?.Key, e.OldNode?.Key));
			e.Cancel = e.NewNode?.Key == "b";
		}));

		await SelectAsync(tree, "a");
		await SelectAsync(tree, "b");

		tree.Instance.SelectedNode!.Key.Should().Be("a");
		seen.Should().Equal(("a", null), ("b", "a"));
	}

	/// <summary>An item in the tree under test, optionally a web link.</summary>
	public sealed class Item(string id, string? parentId, string name) : IWebLink
	{
		/// <summary>Gets the key.</summary>
		public string Id { get; } = id;

		/// <summary>Gets the parent key, or null at the top level.</summary>
		public string? ParentId { get; } = parentId;

		/// <summary>Gets the display text.</summary>
		public string Name { get; } = name;

		/// <inheritdoc />
		public string Target { get; set; } = string.Empty;

		/// <inheritdoc />
		public string Url { get; set; } = string.Empty;

		/// <inheritdoc />
		public override string ToString() => $"Item {Id}";
	}

	/// <summary>
	/// Returns every item, or with load on demand only the children of the key in
	/// <see cref="DataRequest{TItem}.SearchText"/>, and records each request.
	/// </summary>
	private sealed class ItemProvider : DataProviderBase<Item>
	{
		private bool _loadOnDemand;

		/// <summary>Answers each request with only the children of the requested key, as load on demand asks.</summary>
		public void UseLoadOnDemand() => _loadOnDemand = true;

		private Exception? _failure;

		/// <summary>Makes every later request throw <paramref name="failure"/>.</summary>
		public void FailWith(Exception failure) => _failure = failure;

		public List<string?> Requests { get; } = [];

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
			Requests.Add(request.SearchText);
			if (_failure != null)
			{
				throw _failure;
			}

			var items = _loadOnDemand
				? Items.Where(i => (i.ParentId ?? string.Empty) == (request.SearchText ?? string.Empty)).ToList()
				: [.. Items];
			return Task.FromResult(new DataResponse<Item>(items, items.Count));
		}
	}
}
