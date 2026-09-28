using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="TreeNodeEventArgs{TItem}"/> and the tree event argument types derived from it.</summary>
public class TreeNodeEventArgsTests
{
	/// <summary>The base type captures the node.</summary>
	[Fact]
	public void TreeNodeEventArgs_CapturesNode()
	{
		var node = new TreeNode<Item>();

		new TreeNodeEventArgs<Item>(node).Node.Should().BeSameAs(node);
	}

	/// <summary>The cancellable type starts uncancelled and can be cancelled.</summary>
	[Fact]
	public void TreeNodeCancelEventArgs_CanBeCancelled()
	{
		var args = new TreeNodeCancelEventArgs<Item>(new TreeNode<Item>()) { Cancel = true };

		args.Cancel.Should().BeTrue();
	}

	/// <summary>The before-edit type is a cancellable node event.</summary>
	[Fact]
	public void TreeNodeBeforeEditEventArgs_IsCancellable()
	{
		var node = new TreeNode<Item>();
		var args = new TreeNodeBeforeEditEventArgs<Item>(node);

		args.Node.Should().BeSameAs(node);
		args.Cancel.Should().BeFalse();
		args.Should().BeAssignableTo<TreeNodeCancelEventArgs<Item>>();
	}

	/// <summary>The after-edit type carries the old and new text, and a handler can change the new text.</summary>
	[Fact]
	public void TreeNodeAfterEditEventArgs_CarriesOldAndNewText()
	{
		var args = new TreeNodeAfterEditEventArgs<Item>(new TreeNode<Item>(), "old", "new");

		args.OldValue.Should().Be("old");
		args.NewValue.Should().Be("new");

		args.NewValue = "changed";

		args.NewValue.Should().Be("changed");
	}

	/// <summary>The selection-change type records the new and old nodes the right way round.</summary>
	[Fact]
	public void TreeBeforeSelectionChangeEventArgs_RecordsNewAndOldNodes()
	{
		var newNode = new TreeNode<Item> { Key = "new" };
		var oldNode = new TreeNode<Item> { Key = "old" };

		var args = new TreeBeforeSelectionChangeEventArgs<Item>(newNode, oldNode);

		args.NewNode.Should().BeSameAs(newNode);
		args.OldNode.Should().BeSameAs(oldNode);
		args.Cancel.Should().BeFalse();
	}

	/// <summary>The selection-change type allows either node to be absent.</summary>
	[Fact]
	public void TreeBeforeSelectionChangeEventArgs_AllowsNullNodes()
	{
		var args = new TreeBeforeSelectionChangeEventArgs<Item>(null, null);

		args.NewNode.Should().BeNull();
		args.OldNode.Should().BeNull();
	}

	private sealed class Item
	{
		public string Name { get; set; } = string.Empty;
	}
}
