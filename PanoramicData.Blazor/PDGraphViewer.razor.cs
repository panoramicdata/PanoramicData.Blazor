using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// A comprehensive graph visualization component that supports multi-dimensional data visualization
/// with interactive node and edge displays, configurable clustering, and dark mode support.
/// </summary>
/// <typeparam name="TItem">The type of data items that will be used to generate the graph data.</typeparam>
public partial class PDGraphViewer<TItem> : PDComponentBase where TItem : class
{
	private PDSplitter? _splitter;
	private GraphNode? _selectedNode;
	private GraphEdge? _selectedEdge;

	/// <summary>
	/// Gets the graph, set by the markup.
	/// </summary>
	internal PDGraph<TItem>? Graph { get; set; }

	/// <summary>
	/// Gets the graph information panel, set by the markup.
	/// </summary>
	internal PDGraphInfo<TItem>? GraphInfo { get; set; }

	/// <summary>
	/// Gets or sets the data provider for the graph data.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public IDataProviderService<GraphData>? DataProvider { get; set; }

	/// <summary>
	/// Gets or sets the split panel direction (Horizontal or Vertical).
	/// </summary>
	[Parameter]
	public SplitDirection SplitDirection { get; set; } = SplitDirection.Horizontal;

	/// <summary>
	/// Gets or sets whether to show the information panel.
	/// </summary>
	[Parameter]
	public bool ShowInfo { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the controls panel within the info panel.
	/// </summary>
	[Parameter]
	public bool ShowControls { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the controls are read-only.
	/// </summary>
	[Parameter]
	public bool ReadOnlyControls { get; set; }

	/// <summary>
	/// Gets or sets the visualization configuration for mapping dimensions to visual properties.
	/// </summary>
	[Parameter]
	public GraphVisualizationConfig VisualizationConfig { get; set; } = new();

	/// <summary>
	/// Gets or sets the clustering configuration.
	/// </summary>
	[Parameter]
	public GraphClusteringConfig ClusteringConfig { get; set; } = new();

	/// <summary>
	/// Gets or sets the convergence threshold for the physics simulation.
	/// </summary>
	[Parameter]
	public double ConvergenceThreshold { get; set; } = 0.02;

	/// <summary>
	/// Gets or sets the damping factor for the physics simulation. Higher values mean faster settling.
	/// </summary>
	[Parameter]
	public double Damping { get; set; } = 0.98;

	/// <summary>
	/// Gets or sets a callback that is invoked when a node is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<GraphNode> NodeClick { get; set; }

	/// <summary>
	/// Gets or sets a callback that is invoked when an edge is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<GraphEdge> EdgeClick { get; set; }

	/// <summary>
	/// Gets or sets a callback that is invoked when the selection changes.
	/// </summary>
	[Parameter]
	public EventCallback<(GraphNode? Node, GraphEdge? Edge)> SelectionChanged { get; set; }

	/// <summary>
	/// Gets or sets a callback that is invoked when the configuration changes.
	/// </summary>
	[Parameter]
	public EventCallback<(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping)> ConfigurationChanged { get; set; }

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		// Set a unique ID if not provided
		if (HasDefaultId)
		{
			Id = $"pd-graph-viewer-{ComponentIdSequence.Next()}";
		}
	}

	private async Task OnNodeClick(GraphNode node)
	{
		_selectedNode = node;
		_selectedEdge = null;

		GraphInfo?.SetSelection(node, null);

		await NodeClick.InvokeAsync(node).ConfigureAwait(true);
		await SelectionChanged.InvokeAsync((node, null)).ConfigureAwait(true);
	}

	private async Task OnEdgeClick(GraphEdge edge)
	{
		_selectedNode = null;
		_selectedEdge = edge;

		GraphInfo?.SetSelection(null, edge);

		await EdgeClick.InvokeAsync(edge).ConfigureAwait(true);
		await SelectionChanged.InvokeAsync((null, edge)).ConfigureAwait(true);
	}

	private async Task OnSelectionChanged((GraphNode? Node, GraphEdge? Edge) selection)
	{
		_selectedNode = selection.Node;
		_selectedEdge = selection.Edge;
		await SelectionChanged.InvokeAsync(selection).ConfigureAwait(true);
	}

	// Update the OnConfigurationChanged method
	/// <summary>
	/// Updates the visualization and clustering configuration, preserving current node positions.
	/// </summary>
	/// <param name="config">A tuple containing the new visualization configuration, clustering configuration, and damping value.</param>
	public async Task UpdateConfigurationAsync((GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping) config)
	{
		VisualizationConfig = config.Visualization;
		ClusteringConfig = config.Clustering;
		Damping = config.Damping;

		// ✅ FIXED: Use UpdateConfigurationAsync to preserve positions
		if (Graph is not null)
		{
			await Graph.UpdateConfigurationAsync(config.Visualization, config.Clustering).ConfigureAwait(true);
		}

		await ConfigurationChanged.InvokeAsync(config).ConfigureAwait(true);
	}

	private async Task OnConfigurationChanged((GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping) config)
	{
		await UpdateConfigurationAsync(config).ConfigureAwait(true);
	}

	// ✅ KEEP: Only the methods that are called externally
	/// <summary>
	/// Refreshes the graph data from the data provider.
	/// </summary>
	public Task RefreshAsync() => RefreshAsync(CancellationToken.None);

	/// <summary>
	/// Refreshes the graph data from the data provider.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
	public async Task RefreshAsync(CancellationToken cancellationToken)
	{
		if (Graph is not null)
		{
			await Graph.RefreshAsync(cancellationToken).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Centers the graph view on the specified node.
	/// </summary>
	/// <param name="nodeId">The ID of the node to center on.</param>
	public async Task CenterOnNodeAsync(string nodeId)
	{
		if (Graph is not null)
		{
			await Graph.CenterOnNodeAsync(nodeId).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Fits the entire graph into the current viewport.
	/// </summary>
	public async Task FitToViewAsync()
	{
		if (Graph is not null)
		{
			await Graph.FitToViewAsync().ConfigureAwait(true);
		}
	}
}