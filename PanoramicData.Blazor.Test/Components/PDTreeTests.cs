using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
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
public class PDTreeTests : BunitContext
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
		_provider.Failure = new InvalidOperationException("boom");

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

	/// <summary>Selecting the selected node again with auto edit begins an edit when editing is allowed.</summary>
	[Fact]
	public async Task SelectingTheSelectedNodeAgain_BeginsAnEdit()
	{
		var tree = RenderItemTree(p => p.Add(x => x.AllowEdit, true));
		await SelectAsync(tree, "a");

		await tree.InvokeAsync(() => tree.Instance.SelectNode(Node(tree, "a")));

		Node(tree, "a").IsEditing.Should().BeTrue();
		tree.Find("input.pdtreenode_edit").GetAttribute("value").Should().Be("Alpha");
		_selections.Should().ContainSingle();
	}

	/// <summary>BeginEdit is refused when editing is not allowed, and BeforeEdit can cancel it.</summary>
	[Fact]
	public async Task BeginEdit_HonoursAllowEditAndBeforeEdit()
	{
		var readOnly = RenderItemTree();
		await SelectAsync(readOnly, "a");
		await readOnly.InvokeAsync(readOnly.Instance.BeginEdit);
		Node(readOnly, "a").IsEditing.Should().BeFalse();

		var cancelling = RenderItemTree(p => p
			.Add(x => x.AllowEdit, true)
			.Add(x => x.BeforeEdit, (TreeNodeBeforeEditEventArgs<Item> e) => e.Cancel = true));
		await SelectAsync(cancelling, "a");
		await cancelling.InvokeAsync(cancelling.Instance.BeginEdit);
		Node(cancelling, "a").IsEditing.Should().BeFalse();
	}

	private async Task<IRenderedComponent<PDTree<Item>>> EditAsync(string key, string newText, Action<TreeNodeAfterEditEventArgs<Item>>? afterEdit = null)
	{
		var tree = RenderItemTree(p =>
		{
			p.Add(x => x.AllowEdit, true);
			if (afterEdit != null)
			{
				p.Add(x => x.AfterEdit, afterEdit);
			}
		});
		await SelectAsync(tree, key);
		await tree.InvokeAsync(tree.Instance.BeginEdit);
		tree.Find("input.pdtreenode_edit").Input(newText);
		return tree;
	}

	/// <summary>Committing an edit renames the node, re-sorts its siblings and reports the old and new text.</summary>
	[Fact]
	public async Task CommitEdit_RenamesAndResorts()
	{
		TreeNodeAfterEditEventArgs<Item>? args = null;
		var tree = await EditAsync("a", "Zulu", e => args = e);

		await tree.InvokeAsync(tree.Instance.CommitEdit);

		Node(tree, "a").Text.Should().Be("Zulu");
		Node(tree, "a").IsEditing.Should().BeFalse();
		tree.Instance.RootNode.Nodes!.Select(n => n.Key).Should().Equal("b", "a");
		args!.OldValue.Should().Be("Alpha");
		args.NewValue.Should().Be("Zulu");
	}

	/// <summary>AfterEdit may change the committed text, or cancel the edit.</summary>
	[Fact]
	public async Task AfterEdit_CanAlterOrCancel()
	{
		var altered = await EditAsync("a", "Zulu", e => e.NewValue = "Yankee");
		await altered.InvokeAsync(altered.Instance.CommitEdit);
		Node(altered, "a").Text.Should().Be("Yankee");

		var cancelled = await EditAsync("a", "Zulu", e => e.Cancel = true);
		await cancelled.InvokeAsync(cancelled.Instance.CommitEdit);
		Node(cancelled, "a").Text.Should().Be("Alpha");
		Node(cancelled, "a").IsEditing.Should().BeFalse();
	}

	/// <summary>Committing blank text cancels the edit and reports that a value is required.</summary>
	[Fact]
	public async Task CommitEdit_WithBlankText_IsRefused()
	{
		var tree = await EditAsync("a", "   ");

		await tree.InvokeAsync(tree.Instance.CommitEdit);

		Node(tree, "a").Text.Should().Be("Alpha");
		_exceptions.Should().ContainSingle().Which.Message.Should().Be("A value is required");
	}

	/// <summary>CancelEdit abandons the typed text.</summary>
	[Fact]
	public async Task CancelEdit_AbandonsTheText()
	{
		var tree = await EditAsync("a", "Zulu");

		await tree.InvokeAsync(tree.Instance.CancelEdit);

		Node(tree, "a").Text.Should().Be("Alpha");
		Node(tree, "a").IsEditing.Should().BeFalse();
	}

	/// <summary>Escape cancels an edit and Enter commits one, and every key is passed on through KeyDown.</summary>
	[Fact]
	public async Task EscapeAndEnter_CancelAndCommit()
	{
		var keys = new List<string>();
		var escaped = await EditAsync("a", "Zulu");
		escaped.Find("div.pdtree").KeyDown(new KeyboardEventArgs { Code = "Escape" });
		Node(escaped, "a").Text.Should().Be("Alpha");

		var entered = await EditAsync("b", "Echo");
		entered.Render(p => p.Add(x => x.KeyDown, (KeyboardEventArgs e) => keys.Add(e.Code)));
		Key(entered, "Enter");
		Node(entered, "b").Text.Should().Be("Echo");
		keys.Should().Equal("Enter");
	}

	/// <summary>Leaving the edit box commits the edit.</summary>
	[Fact]
	public async Task BlurringTheEditBox_CommitsTheEdit()
	{
		var tree = await EditAsync("a", "Zulu");

		tree.Find("input.pdtreenode_edit").Blur();

		Node(tree, "a").Text.Should().Be("Zulu");
	}

	/// <summary>F2 begins an edit of the selected node.</summary>
	[Fact]
	public async Task F2_BeginsAnEdit()
	{
		var tree = RenderItemTree(p => p.Add(x => x.AllowEdit, true));
		await SelectAsync(tree, "b");

		Key(tree, "F2");

		Node(tree, "b").IsEditing.Should().BeTrue();
	}

	/// <summary>The right and left arrows expand and collapse the selected node.</summary>
	[Fact]
	public async Task ArrowRightAndLeft_ExpandAndCollapse()
	{
		var tree = RenderItemTree();
		await SelectAsync(tree, "b");

		Key(tree, "ArrowRight");
		Node(tree, "b").IsExpanded.Should().BeTrue();
		Key(tree, "ArrowRight");
		Node(tree, "b").IsExpanded.Should().BeTrue("a second right arrow leaves it expanded");

		Key(tree, "ArrowLeft");
		Node(tree, "b").IsExpanded.Should().BeFalse();
		Key(tree, "ArrowLeft");
		Node(tree, "b").IsExpanded.Should().BeFalse();
	}

	/// <summary>The down and up arrows move the selection through the visible nodes.</summary>
	[Fact]
	public async Task ArrowDownAndUp_MoveTheSelection()
	{
		var tree = RenderItemTree();
		await SelectAsync(tree, "a");

		Key(tree, "ArrowDown");
		tree.Instance.SelectedNode!.Key.Should().Be("b", "a is collapsed, so the next visible node is b");

		Key(tree, "ArrowDown");
		tree.Instance.SelectedNode!.Key.Should().Be("b", "there is nothing after b");

		Key(tree, "ArrowUp");
		tree.Instance.SelectedNode!.Key.Should().Be("a");

		Key(tree, "ArrowUp");
		tree.Instance.SelectedNode!.Key.Should().Be("a", "the hidden root node is never selected");
	}

	/// <summary>With ShowRoot false, the up arrow does not move onto the hidden single root.</summary>
	[Fact]
	public async Task ArrowUp_WithShowRootFalse_DoesNotSelectTheHiddenRoot()
	{
		_provider.Items.RemoveAll(i => i.Id.StartsWith('b'));
		var tree = RenderItemTree(p => p.Add(x => x.ShowRoot, false));
		await SelectAsync(tree, "a1");

		Key(tree, "ArrowUp");

		tree.Instance.SelectedNode!.Key.Should().Be("a1");
	}

	/// <summary>A navigation key with no selection only passes the key on.</summary>
	[Fact]
	public void KeyWithNoSelection_IsOnlyPassedOn()
	{
		var keys = new List<string>();
		var tree = RenderItemTree(p => p.Add(x => x.KeyDown, (KeyboardEventArgs e) => keys.Add(e.Code)));

		Key(tree, "ArrowDown");

		tree.Instance.SelectedNode.Should().BeNull();
		keys.Should().Equal("ArrowDown");
	}

	/// <summary>A single mouse down selects the node once the double-click window has passed.</summary>
	[Fact]
	public async Task SingleMouseDown_SelectsTheNode()
	{
		var tree = RenderItemTree();

		await tree.FindAll(".pdtreenode_content")[1].MouseDownAsync(new MouseEventArgs { Button = 0 });

		tree.WaitForAssertion(() => tree.Instance.SelectedNode.Should().NotBeNull(), TimeSpan.FromSeconds(5));
		tree.Instance.SelectedNode!.Key.Should().Be("b");
		Node(tree, "b").IsExpanded.Should().BeFalse();
	}

	/// <summary>Two quick mouse downs on the same node select it and toggle its expansion.</summary>
	/// <remarks>
	/// Both presses are made in one dispatcher call so that they always fall inside the tree's 250ms
	/// double-click window, however loaded the machine running the tests is.
	/// </remarks>
	[Fact]
	public async Task DoubleMouseDown_SelectsAndExpands()
	{
		var tree = RenderItemTree();
		var node = Node(tree, "b");

		await tree.InvokeAsync(() =>
		{
			tree.Instance.NodeMouseDown(node, new MouseEventArgs { Button = 0 });
			tree.Instance.NodeMouseDown(node, new MouseEventArgs { Button = 0 });
		});

		tree.WaitForAssertion(() => Node(tree, "b").IsExpanded.Should().BeTrue(), TimeSpan.FromSeconds(5));
		tree.Instance.SelectedNode!.Key.Should().Be("b");
	}

	/// <summary>A right click is ignored when right clicks do not select.</summary>
	[Fact]
	public async Task RightClick_IsIgnored_WhenRightClickDoesNotSelect()
	{
		var tree = RenderItemTree(p => p.Add(x => x.RightClickSelectsItem, false));

		await tree.InvokeAsync(() =>
		{
			tree.Instance.NodeMouseDown(Node(tree, "b"), new MouseEventArgs { Button = 2 });
			tree.Instance.NodeMouseDown(Node(tree, "a"), new MouseEventArgs { Button = 0 });
		});

		tree.WaitForAssertion(() => _selections.Should().NotBeEmpty(), TimeSpan.FromSeconds(5));
		_selections.Select(n => n.Key).Should().Equal("a");
	}

	/// <summary>NodeDoubleClick selects the node and toggles its expansion.</summary>
	[Fact]
	public async Task NodeDoubleClick_SelectsAndToggles()
	{
		var tree = RenderItemTree();

		await tree.InvokeAsync(() => tree.Instance.NodeDoubleClick(Node(tree, "a"), new MouseEventArgs()));

		tree.Instance.SelectedNode!.Key.Should().Be("a");
		Node(tree, "a").IsExpanded.Should().BeTrue();
	}

	/// <summary>Toggling a leaf does nothing; toggling a branch raises NodeExpanded then NodeCollapsed.</summary>
	[Fact]
	public async Task Toggle_RaisesExpandedAndCollapsed_ButIgnoresLeaves()
	{
		var events = new List<string>();
		var tree = RenderItemTree(p => p
			.Add(x => x.NodeExpanded, (TreeNode<Item> n) => events.Add($"+{n.Key}"))
			.Add(x => x.NodeCollapsed, (TreeNode<Item> n) => events.Add($"-{n.Key}")));

		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(Node(tree, "b1")));
		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(Node(tree, "b")));
		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(Node(tree, "b")));

		events.Should().Equal("+b", "-b");
	}

	/// <summary>Loading on demand fetches a node's children when it is first expanded, and leaf items are marked leaves.</summary>
	[Fact]
	public async Task LoadOnDemand_FetchesChildrenOnFirstExpand()
	{
		_provider.LoadOnDemand = true;
		var tree = RenderItemTree(p => p
			.Add(x => x.LoadOnDemand, true)
			.Add(x => x.IsLeaf, item => item.Id == "b"));

		Node(tree, "a").Nodes.Should().BeNull("children are not loaded yet");
		Node(tree, "b").Isleaf.Should().BeTrue();

		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(Node(tree, "a")));

		Node(tree, "a").Nodes!.Select(n => n.Key).Should().Equal("a1");
		_provider.Requests.Should().Equal(string.Empty, "a");
	}

	/// <summary>With ClearOnCollapse, collapsing discards loaded children so they are fetched again.</summary>
	[Fact]
	public async Task ClearOnCollapse_DiscardsChildren()
	{
		_provider.LoadOnDemand = true;
		var tree = RenderItemTree(p => p.Add(x => x.LoadOnDemand, true).Add(x => x.ClearOnCollapse, true));
		var a = Node(tree, "a");

		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(a));
		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(a));
		a.Nodes.Should().BeNull();

		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(a));
		_provider.Requests.Should().Equal(string.Empty, "a", "a");
	}

	/// <summary>RefreshNodeAsync reloads a node's children and leaves it expanded.</summary>
	[Fact]
	public async Task RefreshNodeAsync_ReloadsChildren()
	{
		_provider.LoadOnDemand = true;
		var tree = RenderItemTree(p => p.Add(x => x.LoadOnDemand, true));
		var b = Node(tree, "b");
		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(b));

		_provider.Items.Add(new Item("b2", "b", "Bravo two"));
		await tree.InvokeAsync(() => tree.Instance.RefreshNodeAsync(b));

		b.IsExpanded.Should().BeTrue();
		b.Nodes!.Select(n => n.Key).Should().Equal("b1", "b2");
	}

	/// <summary>RemoveNodeAsync removes the node and selects its parent; a node with no parent is ignored.</summary>
	[Fact]
	public async Task RemoveNodeAsync_RemovesAndSelectsTheParent()
	{
		var tree = RenderItemTree();

		await tree.InvokeAsync(() => tree.Instance.RemoveNodeAsync(Node(tree, "b1")));
		await tree.InvokeAsync(() => tree.Instance.RemoveNodeAsync(new TreeNode<Item>()));

		tree.Instance.RootNode.Find("b1").Should().BeNull();
		tree.Instance.SelectedNode!.Key.Should().Be("b");
	}

	/// <summary>ScrollNodeIntoViewAsync asks JS to scroll the node's element into view.</summary>
	[Fact]
	public async Task ScrollNodeIntoView_CallsJs()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.Mode = JSRuntimeMode.Loose;
		var tree = RenderItemTree();
		var b = Node(tree, "b");

		await tree.InvokeAsync(() => tree.Instance.ScrollNodeIntoViewAsync(b));

		module.VerifyInvoke("scrollIntoView").Arguments.Should().Equal($"tree-node-{b.Id}");
	}

	/// <summary>The tooltip function sets each node's title, and a web link item renders as a link.</summary>
	[Fact]
	public void ToolTipAndWebLinks_AreRendered()
	{
		_provider.Items.Add(new Item("c", null, "Charlie") { Url = "https://example.com", Target = "_blank" });

		var tree = RenderItemTree(p => p.Add(x => x.ToolTip, item => $"Tip {item.Id}"));

		tree.WaitForAssertion(() => tree.Find("a[href='https://example.com']").GetAttribute("target").Should().Be("_blank"));
		tree.Find("a[href='https://example.com']").GetAttribute("title").Should().Be("Tip c");
		tree.FindAll("span[title='Tip a']").Should().ContainSingle();
	}

	/// <summary>A node template replaces the default text rendering at every level.</summary>
	[Fact]
	public void NodeTemplate_RendersEachNode()
	{
		var tree = RenderItemTree(p => p.Add(x => x.NodeTemplate, (RenderFragment<TreeNode<Item>>)(node => b => b.AddMarkupContent(0, $"<b class=\"tpl\">{node.Key}</b>"))));
		tree.Instance.ExpandAll();
		tree.Render();

		tree.FindAll("b.tpl").Select(e => e.TextContent).Should().Equal("a", "a1", "a1x", "b", "b1");
	}

	/// <summary>Disposing the tree is safe while a click is pending.</summary>
	[Fact]
	public void Dispose_WithAPendingClick_IsSafe()
	{
		var tree = RenderItemTree();
		tree.Instance.NodeMouseDown(Node(tree, "a"), new MouseEventArgs());

		var act = tree.Instance.Dispose;

		act.Should().NotThrow();
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
		public bool LoadOnDemand { get; set; }

		public Exception? Failure { get; set; }

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
			if (Failure != null)
			{
				throw Failure;
			}

			var items = LoadOnDemand
				? Items.Where(i => (i.ParentId ?? string.Empty) == (request.SearchText ?? string.Empty)).ToList()
				: [.. Items];
			return Task.FromResult(new DataResponse<Item>(items, items.Count));
		}
	}
}
