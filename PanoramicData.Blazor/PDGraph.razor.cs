using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// SVG-based graph visualization component with force-directed layout and interactive features.
/// </summary>
/// <typeparam name="TItem">The type of data items used to generate graph data.</typeparam>
public partial class PDGraph<TItem> : JSModuleComponentBase where TItem : class
{
	private ElementReference _svgElement;
	private GraphData? _graphData;
	private readonly Dictionary<string, (double X, double Y)> _nodePositions = [];
	private string _transformMatrix = "translate(0,0) scale(1)";
	private bool _isLoading = true;
	private bool _hasError;
	private DotNetObjectReference<PDGraph<TItem>>? _objRef;

	// Add a flag to prevent re-initialization during selection updates
	private bool _isUpdatingSelection;

	// Add these fields to track parameter changes
	private bool _isUpdatingParameters;
	private GraphVisualizationConfig? _previousVisualizationConfig;
	private GraphClusteringConfig? _previousClusteringConfig;
	private double _previousConvergenceThreshold;
	private double _previousDamping;

	/// <summary>Gets the JavaScript module path for the graph component.</summary>
	protected override string ModulePath => "./_content/PanoramicData.Blazor/PDGraph.razor.js";

	/// <summary>
	/// Gets or sets the unique identifier for this component.
	/// </summary>
	[Parameter]
	public string Id { get; set; } = $"pd-graph-{ComponentIdSequence.Next()}";

	/// <summary>
	/// Gets or sets the CSS class for styling.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the component is visible.
	/// </summary>
	[Parameter]
	public bool IsVisible { get; set; } = true;

	/// <summary>
	/// Gets or sets the data provider for the graph data.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public IDataProviderService<GraphData>? DataProvider { get; set; }

	/// <summary>
	/// Gets or sets the visualization configuration.
	/// </summary>
	[Parameter]
	public GraphVisualizationConfig VisualizationConfig { get; set; } = new();

	/// <summary>
	/// Gets or sets the clustering configuration.
	/// </summary>
	[Parameter]
	public GraphClusteringConfig ClusteringConfig { get; set; } = new();

	/// <summary>
	/// Gets or sets the convergence threshold for the physics simulation. Lower values make physics run longer.
	/// </summary>
	[Parameter]
	public double ConvergenceThreshold { get; set; } = 0.02;

	/// <summary>
	/// Gets or sets the damping factor for the physics simulation. Higher values mean faster settling.
	/// </summary>
	[Parameter]
	public double Damping { get; set; } = 0.95;

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

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		await base.OnParametersSetAsync();

		// ✅ FIXED: Early exit if we're updating to prevent cascading
		if (_isUpdatingSelection || _isUpdatingParameters)
		{
			Console.WriteLine("PDGraph: Skipping OnParametersSetAsync - already updating");
			return;
		}

		// ✅ FIXED: Track if this is the first load
		if (_previousVisualizationConfig == null)
		{
			Console.WriteLine("PDGraph: First load - doing full refresh");
			RememberParameters();

			await RefreshAsync().ConfigureAwait(true);
			return;
		}

		// ✅ FIXED: Check for actual changes with better logic
		var hasVisualizationChanged = !ReferenceEquals(_previousVisualizationConfig, VisualizationConfig);
		var hasClusteringChanged = !ReferenceEquals(_previousClusteringConfig, ClusteringConfig);
		var hasConvergenceChanged = Math.Abs(_previousConvergenceThreshold - ConvergenceThreshold) > 0.001;
		var hasDampingChanged = Math.Abs(_previousDamping - Damping) > 0.001;
		var hasConfigurationChanged = hasVisualizationChanged || hasClusteringChanged;

		if (!hasConfigurationChanged && !hasConvergenceChanged && !hasDampingChanged)
		{
			// No actual changes detected
			return;
		}

		Console.WriteLine($"PDGraph: Parameter changes - Viz: {hasVisualizationChanged}, Clustering: {hasClusteringChanged}, Convergence: {hasConvergenceChanged} ({_previousConvergenceThreshold:F3} -> {ConvergenceThreshold:F3}), Damping: {hasDampingChanged} ({_previousDamping:F3} -> {Damping:F3})");

		// ✅ FIXED: Set flag to prevent cascading and store new values
		_isUpdatingParameters = true;
		try
		{
			RememberParameters();
			await ApplyParameterChangesAsync(hasConfigurationChanged).ConfigureAwait(true);
		}
		finally
		{
			_isUpdatingParameters = false;
		}
	}

	/// <summary>
	/// Records the current configuration and physics parameters so later changes can be detected.
	/// </summary>
	private void RememberParameters()
	{
		_previousVisualizationConfig = VisualizationConfig;
		_previousClusteringConfig = ClusteringConfig;
		_previousConvergenceThreshold = ConvergenceThreshold;
		_previousDamping = Damping;
	}

	/// <summary>
	/// Pushes changed parameters to the JavaScript module: a configuration change restyles the graph,
	/// otherwise (only the physics changed) the simulation parameters are updated.
	/// </summary>
	/// <param name="hasConfigurationChanged">Whether the visualization or clustering configuration changed.</param>
	private async Task ApplyParameterChangesAsync(bool hasConfigurationChanged)
	{
		if (hasConfigurationChanged)
		{
			Console.WriteLine("PDGraph: Updating configuration via JavaScript");
			if (Module != null && _graphData != null)
			{
				await Module.InvokeVoidAsync("updateConfiguration", Id, _graphData, ClusteringConfig).ConfigureAwait(true);
			}
		}
		else
		{
			Console.WriteLine($"PDGraph: Updating physics parameters to Convergence: {ConvergenceThreshold:F3}, Damping: {Damping:F3}");
			if (Module != null)
			{
				await Module.InvokeVoidAsync("updatePhysicsParameters", Id, ConvergenceThreshold, Damping).ConfigureAwait(true);
			}
		}
	}

	/// <inheritdoc />
	protected override async Task OnModuleLoadedAsync(bool firstRender)
	{
		if (firstRender && Module != null)
		{
			// Create a DotNet object reference for JavaScript interop
			_objRef = DotNetObjectReference.Create(this);
			await Module.InvokeVoidAsync("initialize", Id, _objRef, ClusteringConfig).ConfigureAwait(true);

			// If we have data already, initialize the layout after module loads
			if (_graphData?.Nodes != null)
			{
				await InitializeLayout().ConfigureAwait(true);
			}
		}
	}

	/// <summary>
	/// Refreshes the graph data from the data provider.
	/// </summary>
	/// <returns>A task representing the async operation.</returns>
	public Task RefreshAsync() => RefreshAsync(CancellationToken.None);

	/// <summary>
	/// Refreshes the graph data from the data provider.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token for the async operation.</param>
	/// <returns>A task representing the async operation.</returns>
	public async Task RefreshAsync(CancellationToken cancellationToken)
	{
		if (DataProvider == null)
		{
			// Nothing to load, so there is nothing to wait for either.
			_isLoading = false;
			return;
		}

		try
		{
			_isLoading = true;
			_hasError = false;
			StateHasChanged();

			var request = new DataRequest<GraphData>();
			var response = await DataProvider.GetDataAsync(request, cancellationToken).ConfigureAwait(true);

			if (response.Items.Any())
			{
				_graphData = response.Items.First();

				// Only initialize layout if module is loaded
				if (Module != null)
				{
					await InitializeLayout().ConfigureAwait(true);
				}
			}
			else
			{
				_graphData = new GraphData();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error loading graph data: {ex.Message}");
			_hasError = true;
			_graphData = null;
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	/// <summary>
	/// Centers the graph on the specified node.
	/// </summary>
	/// <param name="nodeId">The ID of the node to center on.</param>
	/// <returns>A task representing the async operation.</returns>
	public async Task CenterOnNodeAsync(string nodeId)
	{
		if (Module != null && _nodePositions.TryGetValue(nodeId, out var position))
		{
			await Module.InvokeVoidAsync("centerOnNode", Id, position.X, position.Y).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Fits the entire graph into the viewport.
	/// </summary>
	/// <returns>A task representing the async operation.</returns>
	public async Task FitToViewAsync()
	{
		if (Module != null)
		{
			await Module.InvokeVoidAsync("fitToView", Id).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Updates the physics simulation parameters.
	/// </summary>
	/// <param name="convergenceThreshold">The convergence threshold for physics simulation.</param>
	public async Task UpdatePhysicsParametersAsync(double convergenceThreshold)
	{
		// ✅ FIXED: Check if we're already updating to prevent cascading
		if (_isUpdatingParameters)
		{
			Console.WriteLine($"UpdatePhysicsParametersAsync: Already updating, skipping ({convergenceThreshold:F3})");
			return;
		}

		_isUpdatingParameters = true;
		try
		{
			Console.WriteLine($"UpdatePhysicsParametersAsync: Updating from {ConvergenceThreshold:F3} to {convergenceThreshold:F3}");
			ConvergenceThreshold = convergenceThreshold;
			if (Module != null)
			{
				await Module.InvokeVoidAsync("updatePhysicsParameters", Id, convergenceThreshold).ConfigureAwait(true);
			}
		}
		finally
		{
			_isUpdatingParameters = false;
		}
	}

	/// <summary>
	/// Updates the visualization configuration.
	/// </summary>
	/// <param name="visualizationConfig">The new visualization configuration.</param>
	/// <param name="clusteringConfig">The new clustering configuration.</param>
	public void UpdateConfiguration(GraphVisualizationConfig visualizationConfig, GraphClusteringConfig clusteringConfig)
	{
		VisualizationConfig = visualizationConfig;
		ClusteringConfig = clusteringConfig;
		StateHasChanged();
	}

	/// <summary>
	/// Updates the configuration and refreshes the styling without regenerating positions.
	/// </summary>
	/// <param name="visualizationConfig">The new visualization configuration.</param>
	/// <param name="clusteringConfig">The new clustering configuration.</param>
	public async Task UpdateConfigurationAsync(GraphVisualizationConfig visualizationConfig, GraphClusteringConfig clusteringConfig)
	{
		_isUpdatingParameters = true;
		try
		{
			VisualizationConfig = visualizationConfig;
			ClusteringConfig = clusteringConfig;

			// ✅ FIXED: Use updateConfiguration to preserve positions while updating styling
			if (Module != null && _graphData != null)
			{
				await Module.InvokeVoidAsync("updateConfiguration", Id, _graphData).ConfigureAwait(true);
			}

			StateHasChanged();
		}
		finally
		{
			_isUpdatingParameters = false;
		}
	}

	private async Task InitializeLayout()
	{
		if (_graphData?.Nodes == null)
		{
			return;
		}

		// Clear existing positions
		_nodePositions.Clear();

		// Initialize position tracking dictionary
		foreach (var node in _graphData.Nodes)
		{
			_nodePositions[node.Id] = (0, 0); // Temporary placeholder
		}

		// Start the force simulation - use regenerateLayout to force fresh positioning
		if (Module != null)
		{
			// Pass the convergence threshold parameter
			await Module.InvokeVoidAsync("regenerateLayout", Id, _graphData, ConvergenceThreshold, ClusteringConfig).ConfigureAwait(true);
		}
	}

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		if (Module != null)
		{
			await Module.InvokeVoidAsync("destroy", Id).ConfigureAwait(true);
		}

		_objRef?.Dispose();
		await base.DisposeAsync().ConfigureAwait(true);
		GC.SuppressFinalize(this);
	}
}