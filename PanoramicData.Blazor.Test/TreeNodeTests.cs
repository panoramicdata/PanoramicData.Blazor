using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the TreeNode&lt;T&gt; class.</summary>
public class TreeNodeTests
{
    private static TreeNode<string> CreateTree()
    {
        // root
        //   ├─ A
        //   │  ├─ A1
        //   │  └─ A2
        //   └─ B
        //      └─ B1
        var root = new TreeNode<string> { Key = "root", Text = "Root", IsExpanded = true };
        var a = new TreeNode<string> { Key = "a", Text = "A", ParentNode = root, IsExpanded = true };
        var a1 = new TreeNode<string> { Key = "a1", Text = "A1", ParentNode = a, Nodes = [] };
        var a2 = new TreeNode<string> { Key = "a2", Text = "A2", ParentNode = a, Nodes = [] };
        a.Nodes = [a1, a2];

        var b = new TreeNode<string> { Key = "b", Text = "B", ParentNode = root, IsExpanded = true };
        var b1 = new TreeNode<string> { Key = "b1", Text = "B1", ParentNode = b, Nodes = [] };
        b.Nodes = [b1];

        root.Nodes = [a, b];
        return root;
    }

    /// <summary>Verifies that Find returns the node with the specified key.</summary>
    [Fact]
    public void WhenFindingExistingKeyThenNodeIsReturned()
    {
        var root = CreateTree();

        var result = root.Find("a2");

        result.ShouldNotBeNull();
        result.Text.ShouldBe("A2");
    }

    /// <summary>Verifies that Find returns null when the key does not exist in the tree.</summary>
    [Fact]
    public void WhenFindingMissingKeyThenNullIsReturned()
    {
        var root = CreateTree();

        var result = root.Find("missing");

        result.ShouldBeNull();
    }

    /// <summary>Verifies that Walk visits all nodes in the tree in depth-first order.</summary>
    [Fact]
    public void WhenWalkingThenAllNodesAreVisited()
    {
        var root = CreateTree();
        var visited = new List<string>();

        root.Walk(n =>
        {
            visited.Add(n.Key);
            return true;
        });

        visited.ShouldBe(["root", "a", "a1", "a2", "b", "b1"]);
    }

    /// <summary>Verifies that Walk stops tree traversal when the callback returns false.</summary>
    [Fact]
    public void WhenWalkReturnsFalseThenWalkStopsEarly()
    {
        var root = CreateTree();
        var visited = new List<string>();

        root.Walk(n =>
        {
            visited.Add(n.Key);
            return n.Key != "a1"; // stop after visiting a1
        });

        visited.ShouldBe(["root", "a", "a1"]);
    }

    /// <summary>Verifies that IsLeaf is true for a node that has no children.</summary>
    [Fact]
    public void WhenNodeHasNoChildrenThenIsLeafIsTrue()
    {
        var leaf = new TreeNode<string> { Key = "leaf", Nodes = [] };

        leaf.Isleaf.ShouldBeTrue();
    }

    /// <summary>Verifies that IsLeaf is false for a node that has at least one child.</summary>
    [Fact]
    public void WhenNodeHasChildrenThenIsLeafIsFalse()
    {
		var parent = new TreeNode<string>
		{
			Key = "parent",
			Nodes = [new TreeNode<string> { Key = "child", Nodes = [] }]
		};

		parent.Isleaf.ShouldBeFalse();
    }

    /// <summary>Verifies that GetNext returns the first child node when the current node is expanded and has children.</summary>
    [Fact]
    public void WhenGetNextOnExpandedNodeThenFirstChildIsReturned()
    {
        var root = CreateTree();
        var a = root.Find("a")!;

        var next = a.GetNext();

        next.ShouldNotBeNull();
        next.Key.ShouldBe("a1");
    }

    /// <summary>Verifies that GetNext returns the next sibling of the parent when the node is the last child in its subtree.</summary>
    [Fact]
    public void WhenGetNextOnLastChildThenNextSiblingOfParentIsReturned()
    {
        var root = CreateTree();
        var a2 = root.Find("a2")!;

        var next = a2.GetNext();

        next.ShouldNotBeNull();
        next.Key.ShouldBe("b");
    }

    /// <summary>Verifies that GetNext returns null for the last node in the entire tree.</summary>
    [Fact]
    public void WhenGetNextOnLastNodeThenNullIsReturned()
    {
        var root = CreateTree();
        var b1 = root.Find("b1")!;

        var next = b1.GetNext();

        next.ShouldBeNull();
    }

    /// <summary>Verifies that GetPrevious returns the parent node when the current node is the first child.</summary>
    [Fact]
    public void WhenGetPreviousOnFirstChildThenParentIsReturned()
    {
        var root = CreateTree();
        var a1 = root.Find("a1")!;

        var prev = a1.GetPrevious();

        prev.ShouldNotBeNull();
        prev.Key.ShouldBe("a");
    }

    /// <summary>Verifies that GetPrevious returns the last descendant of the preceding sibling when traversing backward.</summary>
    [Fact]
    public void WhenGetPreviousOnSecondSiblingThenFirstSiblingIsReturned()
    {
        var root = CreateTree();
        var b = root.Find("b")!;

        var prev = b.GetPrevious();

        prev.ShouldNotBeNull();
        prev.Key.ShouldBe("a2");
    }

    /// <summary>Verifies that CompareTo compares nodes alphabetically by their Text property.</summary>
    [Fact]
    public void WhenCompareToThenComparesTextAlphabetically()
    {
        var apple = new TreeNode<string> { Text = "Apple" };
        var banana = new TreeNode<string> { Text = "Banana" };

        apple.CompareTo(banana).ShouldBeLessThan(0);
        banana.CompareTo(apple).ShouldBeGreaterThan(0);
        apple.CompareTo(apple).ShouldBe(0);
    }

    /// <summary>Verifies that CompareTo throws an InvalidOperationException when comparing against null.</summary>
    [Fact]
    public void WhenCompareToNullThenThrows()
    {
        var node = new TreeNode<string> { Text = "A" };

        Should.Throw<InvalidOperationException>(() => node.CompareTo(null));
    }

    /// <summary>Verifies that ToString includes the node's key in its string representation.</summary>
    [Fact]
    public void WhenToStringWithDataThenIncludesKey()
    {
        var node = new TreeNode<string> { Key = "k1" };

        node.ToString().ShouldContain("k1");
    }

    /// <summary>Verifies that ToString returns "key: {Key}" for a node with only a key set.</summary>
    [Fact]
    public void WhenToStringWithoutDataThenShowsKeyOnly()
    {
        var node = new TreeNode<string> { Key = "k1" };

        node.ToString().ShouldBe("key: k1");
    }
}
