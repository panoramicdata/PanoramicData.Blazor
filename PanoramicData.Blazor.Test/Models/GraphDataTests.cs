using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="GraphData"/>.</summary>
public class GraphDataTests
{
	/// <summary>A new graph has no nodes or edges.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var data = new GraphData();

		data.Nodes.Should().BeEmpty();
		data.Edges.Should().BeEmpty();
	}

	/// <summary>Nodes and edges can be replaced.</summary>
	[Fact]
	public void NodesAndEdges_RoundTrip()
	{
		var data = new GraphData
		{
			Nodes = [new GraphNode { Id = "a" }, new GraphNode { Id = "b" }],
			Edges = [new GraphEdge { FromNodeId = "a", ToNodeId = "b" }]
		};

		data.Nodes.Select(n => n.Id).Should().Equal("a", "b");
		data.Edges.Should().ContainSingle().Which.ToNodeId.Should().Be("b");
	}
}
