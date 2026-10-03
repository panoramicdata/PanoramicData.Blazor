using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>
/// Tests for the edge cases of <see cref="TreeMapLayoutEngine"/> that the original <c>TreeMapLayoutEngineTests</c>
/// leave uncovered.
/// </summary>
public class TreeMapLayoutEngineTests
{
	private static Node Branch(double size, params Node[] children) => new(size, children);

	/// <summary>
	/// In explicit mode a branch has its own size; when all of its children are empty it is laid out as a
	/// branch but nothing is placed inside it.
	/// </summary>
	[Fact]
	public void ExplicitMode_BranchWithOnlyEmptyChildren_HasNoInnerRectangles()
	{
		var root = Branch(0, Branch(10, Branch(0), Branch(0)));

		var rects = TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 100, 100, sizeMode: TreeMapSizeMode.Explicit);

		rects.Should().ContainSingle().Which.HasChildren.Should().BeTrue();
	}

	/// <summary>
	/// A child too small to register once scaled into the area is collapsed onto the edge rather than given
	/// invalid geometry, while its sibling still fills the area.
	/// </summary>
	[Fact]
	public void VanishinglySmallChild_IsCollapsedNotInvalid()
	{
		var root = Branch(0, Branch(1), Branch(double.Epsilon));

		var rects = TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 0.5, 0.5);

		rects.Should().HaveCount(2);
		rects.Should().OnlyContain(r => double.IsFinite(r.X) && double.IsFinite(r.Y) && r.Width >= 0 && r.Height >= 0);
		rects[0].Area.Should().BeApproximately(0.25, 1e-9);
		rects[1].Area.Should().Be(0);
	}

	/// <summary>
	/// A null entry in a children list is skipped; its siblings are laid out as though it were absent.
	/// </summary>
	[Fact]
	public void NullChildEntry_IsSkipped()
	{
		var first = Branch(30);
		var second = Branch(10);
		var root = new Node(0, [first, null!, second]);

		var rects = TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 100, 100);

		rects.Select(r => r.Item).Should().Equal(first, second);
		rects.Sum(r => r.Area).Should().BeApproximately(10000, 1e-6);
	}

	/// <summary>
	/// Each overload applies the documented defaults for the arguments it omits, matching the full overload.
	/// </summary>
	[Fact]
	public void Overloads_ApplyTheDocumentedDefaults()
	{
		var root = Branch(0, Branch(5, Branch(0, Branch(0, Branch(40), Branch(20)), Branch(30))), Branch(10));

		static string Describe(IReadOnlyList<TreeMapRect<Node>> rects)
			=> string.Join(";", rects.Select(r => $"{r.X:F3},{r.Y:F3},{r.Width:F3},{r.Height:F3},{r.Depth},{r.Size}"));

		var full = Describe(TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100, 3, 0, 0, TreeMapSizeMode.Aggregate));

		Describe(TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100)).Should().Be(full);
		Describe(TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100, 3)).Should().Be(full);
		Describe(TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100, 3, 0)).Should().Be(full);
		Describe(TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100, 3, 0, 0)).Should().Be(full);
		Describe(TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100, TreeMapSizeMode.Explicit))
			.Should().Be(Describe(TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100, 3, 0, 0, TreeMapSizeMode.Explicit)));
	}

	/// <summary>
	/// The padding and header overloads pass their values through: children are inset by the padding and
	/// pushed down by the header.
	/// </summary>
	[Fact]
	public void PaddingAndHeaderOverloads_InsetTheChildren()
	{
		var child = Branch(10);
		var root = Branch(0, Branch(0, child));

		var padded = TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100, 2, 5).Single(r => r.Item == child);
		var headed = TreeMapLayoutEngine.Layout(root, n => n.Children, n => n.Size, 200, 100, 2, 5, 20).Single(r => r.Item == child);

		padded.X.Should().BeApproximately(5, 1e-9);
		padded.Y.Should().BeApproximately(5, 1e-9);
		headed.Y.Should().BeApproximately(25, 1e-9);
		headed.Height.Should().BeApproximately(70, 1e-9);
	}

	private sealed class Node(double size, IReadOnlyList<Node> children)
	{
		public double Size { get; } = size;

		public IReadOnlyList<Node> Children { get; } = children;
	}
}
