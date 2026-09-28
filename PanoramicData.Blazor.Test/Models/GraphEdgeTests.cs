using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="GraphEdge"/>.</summary>
public class GraphEdgeTests
{
	/// <summary>A new edge is an unselected, unlabelled edge of full strength between no nodes.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var edge = new GraphEdge();

		edge.Id.Should().BeEmpty();
		edge.FromNodeId.Should().BeEmpty();
		edge.ToNodeId.Should().BeEmpty();
		edge.Dimensions.Should().BeEmpty();
		edge.Strength.Should().Be(1.0);
		edge.Label.Should().BeEmpty();
		edge.IsSelected.Should().BeFalse();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var edge = new GraphEdge
		{
			Id = "e1",
			FromNodeId = "a",
			ToNodeId = "b",
			Dimensions = new Dictionary<string, double> { ["latency"] = 0.25 },
			Strength = 0.5,
			Label = "calls",
			IsSelected = true
		};

		edge.Id.Should().Be("e1");
		edge.FromNodeId.Should().Be("a");
		edge.ToNodeId.Should().Be("b");
		edge.Dimensions.Should().ContainKey("latency");
		edge.Strength.Should().Be(0.5);
		edge.Label.Should().Be("calls");
		edge.IsSelected.Should().BeTrue();
	}
}
