using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ElementInfo"/>.</summary>
public class ElementInfoTests
{
	private static ElementInfo BuildChain()
	{
		var table = new ElementInfo { Tag = "TABLE", ClassList = ["table", "pd-table"], Id = "t1" };
		var row = new ElementInfo { Tag = "TR", ClassList = ["row", "selected"], Parent = table };
		return new ElementInfo { Tag = "TD", ClassList = ["cell"], Parent = row };
	}

	/// <summary>A new element has no tag, id, classes or parent.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var element = new ElementInfo();

		element.Tag.Should().BeEmpty();
		element.Id.Should().BeEmpty();
		element.ClassList.Should().BeEmpty();
		element.Parent.Should().BeNull();
	}

	/// <summary>Find returns the nearest ancestor with the tag when no classes are required.</summary>
	[Fact]
	public void Find_ByTagOnly_ReturnsAncestor()
	{
		var cell = BuildChain();

		cell.Find("TABLE")!.Id.Should().Be("t1");
	}

	/// <summary>Find requires every requested class to be present on the ancestor.</summary>
	[Fact]
	public void Find_WithClasses_RequiresAllClasses()
	{
		var cell = BuildChain();

		cell.Find("TR", "selected")!.Tag.Should().Be("TR");
		cell.Find("TR", "row", "selected").Should().NotBeNull();
		cell.Find("TR", "selected", "missing").Should().BeNull();
	}

	/// <summary>Find searches ancestors only, never the element itself.</summary>
	[Fact]
	public void Find_DoesNotMatchSelf()
	{
		var cell = BuildChain();

		cell.Find("TD").Should().BeNull();
	}

	/// <summary>An element with no parent has no ancestors.</summary>
	[Fact]
	public void Find_NoParent_ReturnsNull()
	{
		new ElementInfo { Tag = "DIV" }.Find("DIV").Should().BeNull();
	}

	/// <summary>HasAncestor reports whether a matching ancestor exists.</summary>
	[Fact]
	public void HasAncestor_ReflectsFind()
	{
		var cell = BuildChain();

		cell.HasAncestor("TABLE", "pd-table").Should().BeTrue();
		cell.HasAncestor("UL").Should().BeFalse();
	}
}
