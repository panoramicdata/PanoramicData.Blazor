namespace PanoramicData.Blazor.Models;

/// <summary>
/// Configuration for graph clustering algorithms.
/// </summary>
public class GraphClusteringConfig
{
	/// <summary>
	/// Gets or sets whether clustering is enabled.
	/// </summary>
	public bool IsEnabled { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of clusters.
	/// </summary>
	public int MaxClusters { get; set; } = 5;

	/// <summary>
	/// Gets or sets the dimensions to use for clustering with their weights.
	/// Key is dimension name, value is weight (0.0-1.0).
	/// </summary>
	public Dictionary<string, double> DimensionWeights { get; set; } = new();

	/// <summary>
	/// Gets or sets the clustering algorithm to use.
	/// </summary>
	public GraphClusteringAlgorithm Algorithm { get; set; } = GraphClusteringAlgorithm.KMeans;
}

/// <summary>
/// Available clustering algorithms.
/// </summary>
public enum GraphClusteringAlgorithm
{
	/// <summary>
	/// K-means clustering algorithm.
	/// </summary>
	KMeans,

	/// <summary>
	/// Hierarchical clustering algorithm.
	/// </summary>
	Hierarchical
}