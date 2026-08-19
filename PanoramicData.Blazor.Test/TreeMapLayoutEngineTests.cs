using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for the squarified tree map layout engine. The geometry is the part of a tree map most
/// likely to be subtly wrong, so it is separated from the component and tested directly.
/// </summary>
public class TreeMapLayoutEngineTests
{
	private const double Tolerance = 0.000001;

	private sealed class Node(string name, double size, params Node[] children)
	{
		public string Name { get; } = name;

		public double Size { get; } = size;

		public List<Node> Children { get; } = [.. children];
	}

	private static IReadOnlyList<TreeMapRect<Node>> Layout(
		Node root,
		double width = 800,
		double height = 600,
		int maxDepth = 3,
		double nestedPadding = 0,
		double headerHeight = 0,
		TreeMapSizeMode sizeMode = TreeMapSizeMode.Aggregate)
		=> TreeMapLayoutEngine.Layout<Node>(
			root,
			n => n.Children,
			n => n.Size,
			width,
			height,
			maxDepth,
			nestedPadding,
			headerHeight,
			sizeMode);

	private static bool Overlaps(TreeMapRect<Node> a, TreeMapRect<Node> b)
		=> a.X < b.X + b.Width - Tolerance
			&& b.X < a.X + a.Width - Tolerance
			&& a.Y < b.Y + b.Height - Tolerance
			&& b.Y < a.Y + a.Height - Tolerance;

	/// <summary>Top level rectangles must fill the whole layout area.</summary>
	[Fact]
	public void WhenLayingOutThenTopLevelAreasFillTheContainer()
	{
		var root = new Node("root", 0,
			new Node("a", 50),
			new Node("b", 30),
			new Node("c", 20));

		var rects = Layout(root, 800, 600);

		rects.Where(r => r.Depth == 0).Sum(r => r.Area).ShouldBe(800d * 600d, 0.001);
	}

	/// <summary>Rectangle areas must be proportional to the sizes they represent.</summary>
	[Fact]
	public void WhenLayingOutThenAreasAreProportionalToSize()
	{
		var root = new Node("root", 0,
			new Node("a", 50),
			new Node("b", 30),
			new Node("c", 20));

		var rects = Layout(root, 800, 600).Where(r => r.Depth == 0).ToList();
		var total = 800d * 600d;

		rects.Single(r => r.Item.Name == "a").Area.ShouldBe(total * 0.5, 0.001);
		rects.Single(r => r.Item.Name == "b").Area.ShouldBe(total * 0.3, 0.001);
		rects.Single(r => r.Item.Name == "c").Area.ShouldBe(total * 0.2, 0.001);
	}

	/// <summary>Sibling rectangles must not overlap one another.</summary>
	[Fact]
	public void WhenLayingOutThenSiblingsDoNotOverlap()
	{
		var root = new Node("root", 0,
			[.. Enumerable.Range(1, 25).Select(i => new Node($"n{i}", i * i))]);

		var rects = Layout(root, 1000, 700).Where(r => r.Depth == 0).ToList();

		for (var i = 0; i < rects.Count; i++)
		{
			for (var j = i + 1; j < rects.Count; j++)
			{
				Overlaps(rects[i], rects[j]).ShouldBeFalse(
					$"'{rects[i].Item.Name}' overlaps '{rects[j].Item.Name}'");
			}
		}
	}

	/// <summary>Every rectangle must lie within the bounds it was given.</summary>
	[Fact]
	public void WhenLayingOutThenAllRectanglesAreWithinTheContainer()
	{
		var root = new Node("root", 0,
			[.. Enumerable.Range(1, 40).Select(i => new Node($"n{i}", 100 - i))]);

		var rects = Layout(root, 500, 400);

		foreach (var rect in rects)
		{
			rect.X.ShouldBeGreaterThanOrEqualTo(-Tolerance);
			rect.Y.ShouldBeGreaterThanOrEqualTo(-Tolerance);
			(rect.X + rect.Width).ShouldBeLessThanOrEqualTo(500 + Tolerance);
			(rect.Y + rect.Height).ShouldBeLessThanOrEqualTo(400 + Tolerance);
		}
	}

	/// <summary>
	/// Squarified layout must beat slice and dice on aspect ratio, which is the entire reason for
	/// choosing the more complex algorithm.
	/// </summary>
	[Fact]
	public void WhenLayingOutThenAspectRatiosBeatSliceAndDice()
	{
		var sizes = new double[] { 6, 6, 4, 3, 2, 2, 1 };
		var root = new Node("root", 0, [.. sizes.Select((s, i) => new Node($"n{i}", s))]);

		var squarified = Layout(root, 600, 400).Where(r => r.Depth == 0).ToList();
		var squarifiedWorst = squarified.Max(r => AspectRatio(r.Width, r.Height));

		// Slice and dice: every rectangle is a full-height vertical slice.
		var total = sizes.Sum();
		var sliceAndDiceWorst = sizes.Max(s => AspectRatio(600 * (s / total), 400));

		squarifiedWorst.ShouldBeLessThan(sliceAndDiceWorst);
	}

	private static double AspectRatio(double width, double height)
		=> width <= 0 || height <= 0 ? double.MaxValue : Math.Max(width / height, height / width);

	/// <summary>A single child must fill the whole area.</summary>
	[Fact]
	public void WhenThereIsOneChildThenItFillsTheContainer()
	{
		var root = new Node("root", 0, new Node("only", 42));

		var rect = Layout(root, 300, 200).ShouldHaveSingleItem();

		rect.X.ShouldBe(0, Tolerance);
		rect.Y.ShouldBe(0, Tolerance);
		rect.Width.ShouldBe(300, Tolerance);
		rect.Height.ShouldBe(200, Tolerance);
	}

	/// <summary>A null root must produce an empty layout rather than throwing.</summary>
	[Fact]
	public void WhenRootIsNullThenLayoutIsEmpty()
		=> TreeMapLayoutEngine.Layout<Node>(null, n => n.Children, n => n.Size, 100, 100)
			.ShouldBeEmpty();

	/// <summary>A root with no children must produce an empty layout rather than throwing.</summary>
	[Fact]
	public void WhenRootHasNoChildrenThenLayoutIsEmpty()
		=> Layout(new Node("root", 100)).ShouldBeEmpty();

	/// <summary>Zero, negative and non-finite sizes must not produce invalid geometry.</summary>
	[Fact]
	public void WhenSizesAreZeroNegativeOrNonFiniteThenGeometryStaysValid()
	{
		var root = new Node("root", 0,
			new Node("good", 10),
			new Node("zero", 0),
			new Node("negative", -5),
			new Node("nan", double.NaN),
			new Node("infinite", double.PositiveInfinity));

		var rects = Layout(root, 200, 100);

		// Only the one usable node is drawn, and it takes the whole area.
		var rect = rects.ShouldHaveSingleItem();
		rect.Item.Name.ShouldBe("good");
		rect.Area.ShouldBe(200d * 100d, 0.001);

		foreach (var r in rects)
		{
			double.IsNaN(r.X).ShouldBeFalse();
			double.IsNaN(r.Y).ShouldBeFalse();
			double.IsNaN(r.Width).ShouldBeFalse();
			double.IsNaN(r.Height).ShouldBeFalse();
			r.Width.ShouldBeGreaterThanOrEqualTo(0);
			r.Height.ShouldBeGreaterThanOrEqualTo(0);
		}
	}

	/// <summary>A zero or negative container must produce an empty layout rather than throwing.</summary>
	[Theory]
	[InlineData(0, 100)]
	[InlineData(100, 0)]
	[InlineData(-10, 100)]
	[InlineData(double.NaN, 100)]
	public void WhenContainerIsUnusableThenLayoutIsEmpty(double width, double height)
		=> Layout(new Node("root", 0, new Node("a", 1)), width, height).ShouldBeEmpty();

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

	/// <summary>The layout must be deterministic across runs for the same input.</summary>
	[Fact]
	public void WhenLayingOutRepeatedlyThenResultIsDeterministic()
	{
		var root = new Node("root", 0,
			[.. Enumerable.Range(1, 30).Select(i => new Node($"n{i}", (i * 7 % 11) + 1))]);

		var first = Layout(root, 640, 480);
		var second = Layout(root, 640, 480);

		first.Count.ShouldBe(second.Count);

		for (var i = 0; i < first.Count; i++)
		{
			first[i].Item.Name.ShouldBe(second[i].Item.Name);
			first[i].X.ShouldBe(second[i].X, Tolerance);
			first[i].Y.ShouldBe(second[i].Y, Tolerance);
			first[i].Width.ShouldBe(second[i].Width, Tolerance);
			first[i].Height.ShouldBe(second[i].Height, Tolerance);
		}
	}

	/// <summary>A cyclic graph must terminate rather than recursing until the stack is exhausted.</summary>
	[Fact]
	public void WhenHierarchyIsCyclicThenLayoutTerminates()
	{
		var a = new Node("a", 10);
		var b = new Node("b", 10);
		a.Children.Add(b);
		b.Children.Add(a);

		var root = new Node("root", 0, a);

		var rects = Layout(root, 200, 200, maxDepth: 10);

		rects.ShouldNotBeEmpty();
		rects.Count(r => r.Item.Name == "a").ShouldBe(1);
	}

	/// <summary>Nested padding must inset children without pushing them outside their parent.</summary>
	[Fact]
	public void WhenNestedPaddingIsAppliedThenChildrenAreInsetWithinTheParent()
	{
		var root = new Node("root", 0,
			new Node("parent", 0,
				new Node("child", 100)));

		var rects = Layout(root, 400, 300, maxDepth: 3, nestedPadding: 10);

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

	/// <summary>A large tree must lay out without excessive time or invalid geometry.</summary>
	[Fact]
	public void WhenTreeIsLargeThenLayoutCompletes()
	{
		var random = new Random(42);
		var children = Enumerable.Range(0, 100)
			.Select(i => new Node($"b{i}", 0,
				[.. Enumerable.Range(0, 100).Select(j => new Node($"l{i}_{j}", random.Next(1, 1000)))]))
			.ToArray();

		var rects = Layout(new Node("root", 0, children), 1920, 1080, maxDepth: 2);

		rects.Count.ShouldBe(100 + 10000);
		rects.ShouldAllBe(r => r.Width >= 0 && r.Height >= 0);
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

		var rects = Layout(root, 400, 300, maxDepth: 3, nestedPadding: 0, headerHeight: 20);

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
		var rects = Layout(root, 1200, 24, maxDepth: 3, nestedPadding: 0, headerHeight: 20);

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

	private static Node DeepTree()
		=> new("root", 0,
			new Node("L1", 0,
				new Node("L2", 0,
					new Node("L3", 0,
						new Node("L4", 100)))),
			new Node("flat", 20));
}
