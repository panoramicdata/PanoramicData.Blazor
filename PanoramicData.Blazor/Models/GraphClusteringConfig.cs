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
	public Dictionary<string, double> DimensionWeights { get; set; } = [];

	/// <summary>
	/// Gets or sets the algorithm used for clustering.
	/// </summary>
	public GraphClusteringAlgorithm Algorithm { get; set; } = GraphClusteringAlgorithm.None;

	/// <summary>
	/// Gets or sets the dimension to use for spatial clustering.
	/// </summary>
	public string? ClusterByDimension { get; set; }
}

/// <summary>
/// Available clustering algorithms.
/// </summary>
public enum GraphClusteringAlgorithm
{
	/// <summary>
	/// No clustering algorithm applied.
	/// </summary>
	None,

	/// <summary>
	/// K-means clustering algorithm.
	/// </summary>
	KMeans,

	/// <summary>
	/// Hierarchical clustering algorithm.
	/// </summary>
	Hierarchical
}