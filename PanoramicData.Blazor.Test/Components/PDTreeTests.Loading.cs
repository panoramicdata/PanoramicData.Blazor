using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Loading and configuration tests for <see cref="PDTree{TItem}"/>.
/// </summary>
public partial class PDTreeTests
{
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
}
