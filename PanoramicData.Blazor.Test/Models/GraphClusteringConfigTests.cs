using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="GraphClusteringConfig"/>.</summary>
public class GraphClusteringConfigTests
{
	/// <summary>Clustering is off by default, with no algorithm and up to five clusters.</summary>
	[Fact]
	public void New_HasDocumentedDefaults()
	{
		var config = new GraphClusteringConfig();

		config.IsEnabled.Should().BeFalse();
		config.MaxClusters.Should().Be(5);
		config.DimensionWeights.Should().BeEmpty();
		config.Algorithm.Should().Be(GraphClusteringAlgorithm.None);
		config.ClusterByDimension.Should().BeNull();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var config = new GraphClusteringConfig
		{
			IsEnabled = true,
			MaxClusters = 3,
			DimensionWeights = new Dictionary<string, double> { ["cpu"] = 2.0 },
			Algorithm = GraphClusteringAlgorithm.Hierarchical,
			ClusterByDimension = "region"
		};

		config.IsEnabled.Should().BeTrue();
		config.MaxClusters.Should().Be(3);
		config.DimensionWeights.Should().ContainKey("cpu").WhoseValue.Should().Be(2.0);
		config.Algorithm.Should().Be(GraphClusteringAlgorithm.Hierarchical);
		config.ClusterByDimension.Should().Be("region");
	}
}
