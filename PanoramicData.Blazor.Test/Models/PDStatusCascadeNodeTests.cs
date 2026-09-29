using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="PDStatusCascadeNode"/>.</summary>
public class PDStatusCascadeNodeTests
{
	/// <summary>A new node is gray, untitled, has no children and leaves expandability to the component.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var node = new PDStatusCascadeNode();

		node.Status.Should().BeSameAs(StatusType.Gray);
		node.Title.Should().BeEmpty();
		node.Summary.Should().BeEmpty();
		node.Detail.Should().BeNull();
		node.Children.Should().BeEmpty();
		node.Expandable.Should().BeNull();
	}

	/// <summary>All members round-trip, and children can be nested.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var child = new PDStatusCascadeNode { Title = "Disk" };

		var node = new PDStatusCascadeNode
		{
			Status = StatusType.Red,
			Title = "Server",
			Summary = "1 problem",
			Detail = "Disk full",
			Children = [child],
			Expandable = false
		};

		node.Status.Should().BeSameAs(StatusType.Red);
		node.Title.Should().Be("Server");
		node.Summary.Should().Be("1 problem");
		node.Detail.Should().Be("Disk full");
		node.Children.Should().ContainSingle().Which.Should().BeSameAs(child);
		node.Expandable.Should().BeFalse();
	}
}
