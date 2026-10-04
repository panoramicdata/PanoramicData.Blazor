using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Drag and drop tests for <see cref="PDTreeNode{TItem}"/>.
/// </summary>
public partial class PDTreeNodeTests
{
	/// <summary>Dragging a node puts its item in the drag context as the payload.</summary>
	[Fact]
	public void DragStart_SetsThePayload()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDrag, true));

		Content(tree, "b").DragStart();

		var payload = tree.Instance.DragContext!.Payload.Should().BeOfType<List<Item>>().Subject;
		payload.Should().ContainSingle().Which.Id.Should().Be("b");
	}

	/// <summary>Dragging over a drop target highlights it, and leaving removes the highlight.</summary>
	[Fact]
	public void DragEnterAndLeave_HighlightTheTarget()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDrop, true));

		Content(tree, "a").DragEnter();
		Content(tree, "a").ClassList.Should().Contain("drag-over");

		Content(tree, "a").DragLeave();
		Content(tree, "a").ClassList.Should().NotContain("drag-over");

		Content(tree, "a").DragLeave();
		Content(tree, "a").ClassList.Should().NotContain("drag-over", "the count never goes below zero");
	}

	/// <summary>A target the validity check rejects is highlighted as invalid.</summary>
	[Fact]
	public void DragEnter_OnAnInvalidTarget_HighlightsItAsInvalid()
	{
		var tree = RenderInContext(p => p
			.Add(x => x.AllowDrop, true)
			.Add(x => x.IsDropValid, (Item target, object? _) => target.Id != "a"));

		Content(tree, "a").DragEnter();
		Content(tree, "b").DragEnter();

		Content(tree, "a").ClassList.Should().Contain("drag-over-invalid");
		Content(tree, "b").ClassList.Should().Contain("drag-over");
	}

	/// <summary>Without AllowDrop, dragging over a node does not highlight it.</summary>
	[Fact]
	public void DragEnter_WithoutAllowDrop_DoesNothing()
	{
		var tree = RenderInContext();

		Content(tree, "a").DragEnter();

		Content(tree, "a").ClassList.Should().NotContain(["drag-over", "drag-over-invalid"]);
	}

	/// <summary>Dropping onto a valid node raises Drop with the node, the payload and the ctrl key.</summary>
	[Fact]
	public void Drop_OnAValidNode_RaisesDrop()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDrop, true).Add(x => x.AllowDrag, true));
		Content(tree, "b").DragStart();

		Content(tree, "a").DragEnter();
		Content(tree, "a").Drop(new DragEventArgs { CtrlKey = true });

		var drop = _drops.Should().ContainSingle().Subject;
		drop.Target.Should().BeSameAs(Node(tree, "a"));
		drop.Payload.Should().BeSameAs(tree.Instance.DragContext!.Payload);
		drop.Ctrl.Should().BeTrue();
		drop.Before.Should().BeNull();
		Content(tree, "a").ClassList.Should().NotContain("drag-over");
	}

	/// <summary>Dropping onto a node the validity check rejects raises nothing.</summary>
	[Fact]
	public void Drop_OnAnInvalidNode_RaisesNothing()
	{
		var tree = RenderInContext(p => p
			.Add(x => x.AllowDrop, true)
			.Add(x => x.IsDropValid, (Item _, object? _) => false));

		Content(tree, "a").Drop();

		_drops.Should().BeEmpty();
	}

	/// <summary>A drop onto a nested node is passed up through its ancestors to the tree.</summary>
	[Fact]
	public async Task Drop_OnANestedNode_ReachesTheTree()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDrop, true));
		await ExpandAsync(tree, "a");
		await ExpandAsync(tree, "a1");

		Content(tree, "a1x").Drop();

		_drops.Should().ContainSingle().Which.Target.Should().BeSameAs(Node(tree, "a1x"));
	}

	/// <summary>A drop onto a nested node with a node template is also passed up to the tree.</summary>
	[Fact]
	public async Task Drop_OnANestedTemplatedNode_ReachesTheTree()
	{
		var tree = RenderInContext(p => p
			.Add(x => x.AllowDrop, true)
			.Add(x => x.NodeTemplate, node => b => b.AddContent(0, $"[{node.Key}]")));
		await ExpandAsync(tree, "a");

		tree.Markup.Should().Contain("[a1]");
		Content(tree, "a1").Drop();

		_drops.Should().ContainSingle().Which.Target.Should().BeSameAs(Node(tree, "a1"));
	}

	/// <summary>With drop in between, separators appear before each node and after the last sibling.</summary>
	[Fact]
	public void DropInBetween_RendersSeparators()
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDropInBetween, true));

		tree.FindAll(".pdseparator.before").Should().HaveCount(2);
		tree.FindAll(".pdseparator.after").Should().ContainSingle("only the last top-level node has an after separator");
	}

	/// <summary>A drop on a separator raises Drop for that node with the before or after position.</summary>
	[Theory]
	[InlineData(".pdseparator.before", true, "a")]
	[InlineData(".pdseparator.after", false, "b")]
	public void DropOnASeparator_RaisesDropWithThePosition(string selector, bool before, string expectedKey)
	{
		var tree = RenderInContext(p => p.Add(x => x.AllowDropInBetween, true));

		tree.FindAll(selector)[before ? 0 : ^1].Drop();

		var drop = _drops.Should().ContainSingle().Subject;
		drop.Before.Should().Be(before);
		drop.Target.Should().BeSameAs(Node(tree, expectedKey));
	}
}
