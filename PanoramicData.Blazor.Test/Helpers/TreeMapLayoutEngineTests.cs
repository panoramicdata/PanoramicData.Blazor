using AwesomeAssertions;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Helpers;

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

	private sealed class Node(double size, IReadOnlyList<Node> children)
	{
		public double Size { get; } = size;

		public IReadOnlyList<Node> Children { get; } = children;
	}
}
