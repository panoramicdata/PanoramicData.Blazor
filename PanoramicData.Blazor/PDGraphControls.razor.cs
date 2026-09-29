namespace PanoramicData.Blazor;

/// <summary>
/// Configuration panel component for graph visualization and clustering settings.
/// </summary>
/// <typeparam name="TItem">The type of data items used to generate graph data.</typeparam>
public partial class PDGraphControls<TItem> : PDComponentBase where TItem : class
{
	private static int _idSequence;
	private List<string> _availableDimensions = [];

	/// <summary>
	/// Gets or sets whether the controls are read-only.
	/// </summary>
	[Parameter]
	public bool IsReadOnly { get; set; }

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
	/// Gets or sets the available dimension names for mapping.
	/// </summary>
	[Parameter]
	public List<string> AvailableDimensions { get; set; } = [];

	/// <summary>
	/// Gets or sets the damping factor for the physics simulation.
	/// </summary>
	[Parameter]
	public double Damping { get; set; } = 0.95;

	/// <summary>
	/// Gets or sets a callback that is invoked when the configuration changes.
	/// </summary>
	[Parameter]
	public EventCallback<(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double damping)> ConfigurationChanged { get; set; }

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		base.OnInitialized();
		// Set a unique ID if not provided
		if (HasDefaultId)
		{
			Id = $"pd-graph-controls-{Interlocked.Increment(ref _idSequence)}";
		}
	}

	/// <inheritdoc />
	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		// Copy rather than keep the caller's list, so that offering the built-in dimensions never fills it in.
		_availableDimensions = AvailableDimensions is { Count: > 0 }
			? [.. AvailableDimensions]
			:
			[
				// Meaningful dimensions for the innovation knowledge graph
				"Influence", "Fame", "Creativity", "Era", "Category",
				"ConnectionStrength", "RelationshipType", "Certainty"
			];
	}

	private async Task OnConfigurationChanged()
	{
		if (!IsReadOnly)
		{
			await ConfigurationChanged.InvokeAsync((VisualizationConfig, ClusteringConfig, Damping)).ConfigureAwait(true);
		}
	}
}