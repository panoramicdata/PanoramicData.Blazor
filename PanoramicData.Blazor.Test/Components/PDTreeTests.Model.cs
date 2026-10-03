using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Exceptions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests how <see cref="PDTree{TItem}"/> builds nodes from the items it loads on demand.
/// </summary>
public partial class PDTreeTests
{
	/// <summary>Expanding a node whose children include an item without a key is refused.</summary>
	[Fact]
	public async Task LoadOnDemand_ExpandingANodeWithAKeylessChild_Throws()
	{
		_provider.UseLoadOnDemand();
		_provider.Items.Add(new Item(string.Empty, "b", "Nameless"));
		var tree = RenderItemTree(p => p.Add(x => x.LoadOnDemand, true));
		var b = Node(tree, "b");

		var act = () => tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(b));

		(await act.Should().ThrowAsync<PDTreeException>()).WithMessage("Items must supply a key value.");
	}

	/// <summary>
	/// Children loaded on demand are added under their parent, sorted, collapsed and unloaded, except those
	/// <see cref="PDTree{TItem}.IsLeaf"/> declares leaves, which have no children to load.
	/// </summary>
	[Fact]
	public async Task LoadOnDemand_ExpandingANode_AddsItsChildrenSorted_LeavesLoaded()
	{
		_provider.UseLoadOnDemand();
		_provider.Items.Add(new Item("a0", "a", "Alpha zero"));
		var tree = RenderItemTree(p => p
			.Add(x => x.LoadOnDemand, true)
			.Add(x => x.IsLeaf, item => item.Id == "a0"));
		var a = Node(tree, "a");

		await tree.InvokeAsync(() => tree.Instance.ToggleNodeIsExpandedAsync(a));

		a.IsExpanded.Should().BeTrue();
		a.Nodes!.Select(n => n.Key).Should().Equal("a1", "a0");
		Node(tree, "a1").Nodes.Should().BeNull("its children are not loaded until it is expanded");
		Node(tree, "a1").IsExpanded.Should().BeFalse();
		Node(tree, "a0").Nodes.Should().BeEmpty("a leaf has no children to load");
	}
}
