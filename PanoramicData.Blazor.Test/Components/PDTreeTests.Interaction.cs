using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Keyboard, mouse, expansion, loading and rendering tests for <see cref="PDTree{TItem}"/>.
/// </summary>
public partial class PDTreeTests
{
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
}
