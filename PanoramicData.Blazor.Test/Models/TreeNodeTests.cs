using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>
/// Tests for the editing, navigation and sibling helpers of <see cref="TreeNode{T}"/> that the original
/// <c>TreeNodeTests</c> leave uncovered.
/// </summary>
public class TreeNodeTests
{
	private static TreeNode<string> Node(string key, params TreeNode<string>[] children)
	{
		var node = new TreeNode<string> { Key = key, Text = key, Nodes = [.. children] };
		foreach (var child in children)
		{
			child.ParentNode = node;
		}

		return node;
	}

	/// <summary>The asynchronous walk visits every node in order and stops as soon as the function returns false.</summary>
	[Fact]
	public async Task WalkAsync_VisitsUntilStopped()
	{
		var root = Node("root", Node("a", Node("a1")), Node("b"));
		var visited = new List<string>();

		(await root.WalkAsync(n => { visited.Add(n.Key); return Task.FromResult(true); })).Should().BeTrue();
		visited.Should().Equal("root", "a", "a1", "b");

		visited.Clear();
		(await root.WalkAsync(n => { visited.Add(n.Key); return Task.FromResult(n.Key != "a1"); })).Should().BeFalse();
		visited.Should().Equal("root", "a", "a1");
	}

	/// <summary>The asynchronous walk stops at once when the root is rejected, and copes with no children.</summary>
	[Fact]
	public async Task WalkAsync_RootRejectedOrLeaf()
	{
		(await Node("root", Node("a")).WalkAsync(_ => Task.FromResult(false))).Should().BeFalse();
		(await new TreeNode<string>().WalkAsync(_ => Task.FromResult(true))).Should().BeTrue();
	}

	/// <summary>Committing an edit replaces the text; cancelling restores it.</summary>
	[Fact]
	public void Editing_CommitAndCancel()
	{
		var node = Node("n");

		node.BeginEdit();
		node.IsEditing.Should().BeTrue();
		node.EditText.Should().Be("n");
		node.BeginEditEvent.WaitOne(0).Should().BeTrue();
		node.EditText = "renamed";
		node.CommitEdit();

		node.Text.Should().Be("renamed");
		node.IsEditing.Should().BeFalse();

		node.BeginEdit();
		node.EditText = "discarded";
		node.CancelEdit();

		node.Text.Should().Be("renamed");
		node.EditText.Should().Be("renamed");
		node.IsEditing.Should().BeFalse();
	}

	/// <summary>Committing blank text cancels the edit, and committing or cancelling when not editing does nothing.</summary>
	[Fact]
	public void Editing_BlankOrNotEditing_KeepsText()
	{
		var node = Node("n");
		node.CommitEdit();
		node.CancelEdit();
		node.Text.Should().Be("n");

		node.BeginEdit();
		node.EditText = "  ";
		node.CommitEdit();

		node.Text.Should().Be("n");
		node.IsEditing.Should().BeFalse();
	}

	/// <summary>From a collapsed node, next moves to its sibling, and from the last child climbs to the parent's sibling.</summary>
	[Fact]
	public void GetNext_MovesToSiblingOrClimbs()
	{
		var a1 = Node("a1");
		var a = Node("a", a1, Node("a2"));
		var b = Node("b");
		_ = Node("root", a, b);

		a.GetNext().Should().BeSameAs(b);
		a1.GetNext()!.Key.Should().Be("a2");
		a.Nodes![1].GetNext().Should().BeSameAs(b);
		b.GetNext().Should().BeNull();
	}

	/// <summary>A node with no parent has no next or previous node.</summary>
	[Fact]
	public void GetNextAndPrevious_Orphan_IsNull()
	{
		var node = new TreeNode<string>();

		node.GetNext().Should().BeNull();
		node.GetPrevious().Should().BeNull();
	}

	/// <summary>Previous descends into the deepest last visible child of an expanded previous sibling.</summary>
	[Fact]
	public void GetPrevious_DescendsIntoExpandedSibling()
	{
		var deep = Node("a2x");
		var a2 = Node("a2", deep);
		var a = Node("a", Node("a1"), a2);
		var b = Node("b");
		_ = Node("root", a, b);

		b.GetPrevious().Should().BeSameAs(a);

		a.IsExpanded = true;
		b.GetPrevious().Should().BeSameAs(a2);

		a2.IsExpanded = true;
		b.GetPrevious().Should().BeSameAs(deep);
	}

	/// <summary>A node detached from its parent's list has no previous node.</summary>
	[Fact]
	public void GetPrevious_NotInParentList_IsNull()
	{
		var parent = Node("p", Node("a"));

		new TreeNode<string> { ParentNode = parent }.GetPrevious().Should().BeNull();
	}

	/// <summary>Sibling text checks ignore case and require a parent.</summary>
	[Fact]
	public void HasSiblingWithText_IgnoresCase()
	{
		var a = Node("Alpha");
		_ = Node("root", a, Node("Beta"));

		a.HasSiblingWithText("beta").Should().BeTrue();
		a.HasSiblingWithText("gamma").Should().BeFalse();
		new TreeNode<string>().HasSiblingWithText("x").Should().BeFalse();
	}

	/// <summary>First and last sibling checks reflect the node's position, and are false without a parent.</summary>
	[Fact]
	public void IsFirstAndLastSibling_ReflectPosition()
	{
		var first = Node("a");
		var middle = Node("b");
		var last = Node("c");
		_ = Node("root", first, middle, last);

		first.IsFirstSibling().Should().BeTrue();
		first.IsLastSibling().Should().BeFalse();
		middle.IsFirstSibling().Should().BeFalse();
		middle.IsLastSibling().Should().BeFalse();
		last.IsLastSibling().Should().BeTrue();
		new TreeNode<string>().IsFirstSibling().Should().BeFalse();
		new TreeNode<string>().IsLastSibling().Should().BeFalse();
	}

	/// <summary>Unique text numbers a clash from two upwards, skipping numbers already taken.</summary>
	[Fact]
	public void MakeUniqueText_NumbersClashes()
	{
		var folder = Node("root", Node("New"), Node("New (2)"), Node("Other"));

		folder.MakeUniqueText("Fresh").Should().Be("Fresh");
		folder.MakeUniqueText("Other").Should().Be("Other (2)");
		folder.MakeUniqueText("New").Should().Be("New (3)");
		new TreeNode<string>().MakeUniqueText("x").Should().Be("x");
	}

	/// <summary>Each node gets a distinct identifier.</summary>
	[Fact]
	public void Id_IsDistinct()
	{
		new TreeNode<string>().Id.Should().NotBe(new TreeNode<string>().Id);
	}
}
