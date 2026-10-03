using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;
using Shouldly;
using System.Linq;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Nesting, size mode, depth and header tests for <see cref="TreeMapLayoutEngine"/>.
/// </summary>
public partial class TreeMapLayoutEngineTests
{
	/// <summary>
	/// In Aggregate mode a branch's size must be its own size plus every descendant, which is what
	/// makes a file system tree work where directories report zero bytes.
	/// </summary>
	[Fact]
	public void WhenSizeModeIsAggregateThenBranchSizeIncludesDescendants()
	{
		var root = new Node("root", 0,
			new Node("dir", 0,
				new Node("f1", 30),
				new Node("f2", 70)),
			new Node("loose", 100));

		var rects = Layout(root, 400, 400);

		var dir = rects.Single(r => r.Item.Name == "dir");
		dir.Size.ShouldBe(100);

		// Two equal branches, so each takes half the area.
		dir.Area.ShouldBe(400d * 400d / 2, 0.001);
	}

	/// <summary>
	/// In Explicit mode a branch's size must be exactly what the selector returned, so that a source
	/// already reporting subtree totals is not double counted.
	/// </summary>
	[Fact]
	public void WhenSizeModeIsExplicitThenBranchSizeExcludesDescendants()
	{
		var root = new Node("root", 0,
			new Node("db", 100,
				new Node("t1", 30),
				new Node("t2", 70)),
			new Node("other", 100));

		var rects = Layout(root, 400, 400, sizeMode: TreeMapSizeMode.Explicit);

		var db = rects.Single(r => r.Item.Name == "db");
		db.Size.ShouldBe(100);
		db.Area.ShouldBe(400d * 400d / 2, 0.001);
	}

	/// <summary>Children must be drawn nested inside their parent.</summary>
	[Fact]
	public void WhenBranchHasChildrenThenChildrenAreNestedWithinIt()
	{
		var root = new Node("root", 0,
			new Node("parent", 0,
				new Node("child1", 40),
				new Node("child2", 60)));

		var rects = Layout(root, 400, 300);
		var parent = rects.Single(r => r.Item.Name == "parent");
		var children = rects.Where(r => r.Depth == 1).ToList();

		children.Count.ShouldBe(2);

		foreach (var child in children)
		{
			child.X.ShouldBeGreaterThanOrEqualTo(parent.X - Tolerance);
			child.Y.ShouldBeGreaterThanOrEqualTo(parent.Y - Tolerance);
			(child.X + child.Width).ShouldBeLessThanOrEqualTo(parent.X + parent.Width + Tolerance);
			(child.Y + child.Height).ShouldBeLessThanOrEqualTo(parent.Y + parent.Height + Tolerance);
		}

		children.Sum(c => c.Area).ShouldBe(parent.Area, 0.001);
	}

	/// <summary>
	/// The depth cap must stop rectangles being emitted, but must not change the total or the sizes
	/// recorded at the cut. This is what lets a capped tree still reconcile with the true total.
	/// </summary>
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(10)]
	public void WhenDepthIsCappedThenTopLevelTotalIsUnchanged(int maxDepth)
	{
		var root = DeepTree();

		var rects = Layout(root, 500, 500, maxDepth);

		rects.ShouldAllBe(r => r.Depth < maxDepth);
		rects.Where(r => r.Depth == 0).Sum(r => r.Size).ShouldBe(120);
		rects.Where(r => r.Depth == 0).Sum(r => r.Area).ShouldBe(500d * 500d, 0.001);
	}

	/// <summary>
	/// A rectangle at the cut that still has children must be marked as aggregated, and must carry
	/// the size of its whole subtree so nothing is lost from the total.
	/// </summary>
	[Fact]
	public void WhenSubtreeIsBelowTheCutThenItIsMarkedAggregatedAndRetainsItsFullSize()
	{
		var root = DeepTree();

		var rects = Layout(root, 500, 500, maxDepth: 1);

		var level1 = rects.Single(r => r.Item.Name == "L1");
		level1.IsAggregated.ShouldBeTrue();
		level1.HasChildren.ShouldBeTrue();
		level1.Size.ShouldBe(100);

		var leaf = rects.Single(r => r.Item.Name == "flat");
		leaf.IsAggregated.ShouldBeFalse();
		leaf.HasChildren.ShouldBeFalse();
	}

	/// <summary>Nested padding must inset children without pushing them outside their parent.</summary>
	[Fact]
	public void WhenNestedPaddingIsAppliedThenChildrenAreInsetWithinTheParent()
	{
		var root = new Node("root", 0,
			new Node("parent", 0,
				new Node("child", 100)));

		var rects = Layout(root, 400, 300, nestedPadding: 10);

		var parent = rects.Single(r => r.Item.Name == "parent");
		var child = rects.Single(r => r.Item.Name == "child");

		child.X.ShouldBe(parent.X + 10, Tolerance);
		child.Y.ShouldBe(parent.Y + 10, Tolerance);
		child.Width.ShouldBe(parent.Width - 20, Tolerance);
		child.Height.ShouldBe(parent.Height - 20, Tolerance);
	}

	/// <summary>Parents must be emitted before their children so that painting order is correct.</summary>
	[Fact]
	public void WhenLayingOutThenParentsArePlacedBeforeTheirChildren()
	{
		var rects = Layout(DeepTree(), 500, 500, maxDepth: 4);

		var parentIndex = rects.Select((r, i) => (r, i)).First(t => t.r.Item.Name == "L1").i;
		var childIndex = rects.Select((r, i) => (r, i)).First(t => t.r.Item.Name == "L2").i;

		parentIndex.ShouldBeLessThan(childIndex);
	}

	/// <summary>
	/// A branch must reserve a header band so that its own label cannot sit on top of its children.
	/// </summary>
	[Fact]
	public void WhenHeaderHeightIsSetThenChildrenStartBelowTheHeader()
	{
		var root = new Node("root", 0,
			new Node("parent", 0,
				new Node("child", 100)));

		var rects = Layout(root, 400, 300, headerHeight: 20);

		var parent = rects.Single(r => r.Item.Name == "parent");
		var child = rects.Single(r => r.Item.Name == "child");

		child.Y.ShouldBe(parent.Y + 20, Tolerance);
		child.Height.ShouldBe(parent.Height - 20, Tolerance);
		child.X.ShouldBe(parent.X, Tolerance);
	}

	/// <summary>
	/// A branch too short to spare a header must drop it rather than collapsing its children to nothing.
	/// </summary>
	[Fact]
	public void WhenBranchIsTooShortForAHeaderThenTheHeaderIsDropped()
	{
		var root = new Node("root", 0,
			new Node("tall", 0, new Node("a", 900)),
			new Node("short", 0, new Node("b", 4)));

		// The 'short' branch gets a sliver of a very wide, flat container.
		var rects = Layout(root, 1200, 24, headerHeight: 20);

		var shortBranch = rects.Single(r => r.Item.Name == "short");
		var b = rects.Single(r => r.Item.Name == "b");

		shortBranch.Height.ShouldBeLessThan(44);
		b.Height.ShouldBe(shortBranch.Height, Tolerance);
		b.Y.ShouldBe(shortBranch.Y, Tolerance);
	}

	/// <summary>The header must never push a child outside its parent.</summary>
	[Fact]
	public void WhenHeaderHeightIsSetThenChildrenStayWithinTheirParent()
	{
		var rects = Layout(DeepTree(), 600, 400, maxDepth: 4, nestedPadding: 2, headerHeight: 16);

		foreach (var rect in rects)
		{
			rect.X.ShouldBeGreaterThanOrEqualTo(-Tolerance);
			rect.Y.ShouldBeGreaterThanOrEqualTo(-Tolerance);
			(rect.X + rect.Width).ShouldBeLessThanOrEqualTo(600 + Tolerance);
			(rect.Y + rect.Height).ShouldBeLessThanOrEqualTo(400 + Tolerance);
		}
	}
}
