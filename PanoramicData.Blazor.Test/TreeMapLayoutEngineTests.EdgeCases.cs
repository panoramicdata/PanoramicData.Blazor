using PanoramicData.Blazor.Helpers;
using Shouldly;
using System;
using System.Linq;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Edge case tests for <see cref="TreeMapLayoutEngine"/>: degenerate input, determinism, cycles and large trees.
/// </summary>
public partial class TreeMapLayoutEngineTests
{
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

	/// <summary>A large tree must lay out without excessive time or invalid geometry.</summary>
	[Fact]
	public void WhenTreeIsLargeThenLayoutCompletes()
	{
		// Varied but deterministic leaf sizes in the range 1 to 999.
		var children = Enumerable.Range(0, 100)
			.Select(i => new Node($"b{i}", 0,
				[.. Enumerable.Range(0, 100).Select(j => new Node($"l{i}_{j}", ((i * 100 + j) * 7919 % 999) + 1))]))
			.ToArray();

		var rects = Layout(new Node("root", 0, children), 1920, 1080, maxDepth: 2);

		rects.Count.ShouldBe(100 + 10000);
		rects.ShouldAllBe(r => r.Width >= 0 && r.Height >= 0);
	}

	private static Node DeepTree()
		=> new("root", 0,
			new Node("L1", 0,
				new Node("L2", 0,
					new Node("L3", 0,
						new Node("L4", 100)))),
			new Node("flat", 20));
}
