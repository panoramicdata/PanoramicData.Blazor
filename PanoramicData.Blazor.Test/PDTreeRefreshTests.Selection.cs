using AwesomeAssertions;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for how refreshing a <see cref="PDTree{TItem}"/> keeps, moves and re-announces the selection.
/// </summary>
public partial class PDTreeRefreshTests
{
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
}
