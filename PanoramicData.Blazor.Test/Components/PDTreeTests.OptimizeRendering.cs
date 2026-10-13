using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests <see cref="PDTree{TItem}.OptimizeRendering"/>: off by default (existing behaviour, every
/// visible node's <see cref="PDTree{TItem}.NodeTemplate"/> re-runs whenever anything re-renders the
/// tree), on by request (a node's template only re-runs when something about that node changed, or
/// <see cref="PDTree{TItem}.RefreshNode"/> says otherwise).
/// </summary>
public partial class PDTreeTests
{
	private IRenderedComponent<PDTree<Item>> RenderItemTreeWithTemplateCounter(
		List<string> invocations,
		bool optimizeRendering)
	{
		var tree = RenderItemTree(p =>
		{
			p.Add(x => x.OptimizeRendering, optimizeRendering);
			p.Add(x => x.NodeTemplate, (RenderFragment<TreeNode<Item>>)(node => builder =>
			{
				invocations.Add(node.Key);
				builder.AddMarkupContent(0, $"<b>{node.Key}</b>");
			}));
		});
		tree.Instance.ExpandAll();

		// Each node newly created by ExpandAll() renders unconditionally the first time (Blazor never
		// consults ShouldRender on a component's first render), so its ShouldRender snapshot is still
		// unpopulated going into that render. A second render is needed to populate it before the
		// steady state this test actually wants to assert on begins.
		tree.Render();
		tree.Render();
		invocations.Clear();
		return tree;
	}

	/// <summary>The default preserves existing behaviour: an unrelated render still revisits every node.</summary>
	[Fact]
	public void OptimizeRendering_DefaultOff_ReRendersEveryVisibleNodeOnAnUnrelatedRender()
	{
		List<string> invocations = [];
		var tree = RenderItemTreeWithTemplateCounter(invocations, optimizeRendering: false);

		tree.Render();

		invocations.Should().Contain(["a", "a1", "a1x", "b", "b1"]);
	}

	/// <summary>Opting in skips nodes whose own state has not changed.</summary>
	[Fact]
	public void OptimizeRendering_On_SkipsUnchangedNodesOnAnUnrelatedRender()
	{
		List<string> invocations = [];
		var tree = RenderItemTreeWithTemplateCounter(invocations, optimizeRendering: true);

		tree.Render();

		invocations.Should().BeEmpty("no node's own state changed, so nothing should have re-rendered");
	}

	/// <summary>Toggling expansion still works, and re-renders only along the affected branch.</summary>
	[Fact]
	public void OptimizeRendering_On_ExpandingACollapsedNodeStillRendersItsNewChildren()
	{
		List<string> invocations = [];
		var tree = RenderItemTreeWithTemplateCounter(invocations, optimizeRendering: true);
		tree.Instance.RootNode.Walk(n => { n.IsExpanded = false; return true; });
		tree.Render();
		invocations.Clear();

		tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(Node(tree, "a")));

		invocations.Should().Contain("a");
		invocations.Should().Contain("a1");
		invocations.Should().NotContain("b", "a sibling branch that never changed should not re-render");
	}

	/// <summary>
	/// RefreshNode is the escape hatch for state a template reads that lives off the node itself: it
	/// marks the target and every ancestor back to the root as needing to render, since reaching the
	/// target depends on each component on the path down to it also deciding to render.
	/// </summary>
	[Fact]
	public void OptimizeRendering_On_RefreshNodeReRendersTheNodeAndItsAncestorsOnly()
	{
		List<string> invocations = [];
		var tree = RenderItemTreeWithTemplateCounter(invocations, optimizeRendering: true);

		var target = Node(tree, "a1x");
		tree.InvokeAsync(() => tree.Instance.RefreshNode(target));
		tree.Render();

		invocations.Should().Contain(["a", "a1", "a1x"]);
		invocations.Should().NotContain("b");
		invocations.Should().NotContain("b1");
	}

	/// <summary>
	/// PDTree.ApplyItem reuses the same TreeNode instance across a refresh, replacing only its Data,
	/// Text and IconCssClass — none of which ShouldRender's other checks cover. Without tracking them
	/// too, a refresh that changes an item's content without changing the tree's shape would leave that
	/// node's template showing stale data indefinitely.
	/// </summary>
	[Fact]
	public async Task OptimizeRendering_On_RefreshAsyncChangingAnItemsContentStillReRendersIt()
	{
		List<string> invocations = [];
		var tree = RenderItemTreeWithTemplateCounter(invocations, optimizeRendering: true);
		var nodeBeforeRefresh = Node(tree, "a1x");

		var index = _provider.Items.FindIndex(i => i.Id == "a1x");
		_provider.Items[index] = new Item("a1x", "a1", "Alpha one x, renamed");
		await tree.InvokeAsync(() => tree.Instance.RefreshAsync());

		Node(tree, "a1x").Should().BeSameAs(nodeBeforeRefresh, "a refresh merges by key rather than rebuilding");
		invocations.Should().Contain("a1x");
	}
}
