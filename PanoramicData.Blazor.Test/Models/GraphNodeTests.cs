using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="GraphNode"/>.</summary>
public class GraphNodeTests
{
	/// <summary>A new node sits at the origin, at rest, unselected and free to move.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var node = new GraphNode();

		node.Id.Should().BeEmpty();
		node.Label.Should().BeEmpty();
		node.Dimensions.Should().BeEmpty();
		node.X.Should().Be(0);
		node.Y.Should().Be(0);
		node.VelocityX.Should().Be(0);
		node.VelocityY.Should().Be(0);
		node.IsSelected.Should().BeFalse();
		node.IsFixed.Should().BeFalse();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var node = new GraphNode
		{
			Id = "n1",
			Label = "Server",
			Dimensions = new Dictionary<string, double> { ["cpu"] = 0.9 },
			X = 10,
			Y = 20,
			VelocityX = 1.5,
			VelocityY = -2.5,
			IsSelected = true,
			IsFixed = true
		};

		node.Id.Should().Be("n1");
		node.Label.Should().Be("Server");
		node.Dimensions.Should().ContainKey("cpu");
		node.X.Should().Be(10);
		node.Y.Should().Be(20);
		node.VelocityX.Should().Be(1.5);
		node.VelocityY.Should().Be(-2.5);
		node.IsSelected.Should().BeTrue();
		node.IsFixed.Should().BeTrue();
	}
}
