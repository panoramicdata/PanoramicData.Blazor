using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for the squarified tree map layout engine. The geometry is the part of a tree map most
/// likely to be subtly wrong, so it is separated from the component and tested directly.
/// </summary>
public partial class TreeMapLayoutEngineTests
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
}
