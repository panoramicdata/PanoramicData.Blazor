using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Expansion, search and selection tests for <see cref="PDTree{TItem}"/>.
/// </summary>
public partial class PDTreeTests
{
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
}
