namespace PanoramicData.Blazor;

/// <summary>
/// Configuration panel component for graph visualization and clustering settings.
/// </summary>
/// <typeparam name="TItem">The type of data items used to generate graph data.</typeparam>
public partial class PDGraphControls<TItem> : PDComponentBase where TItem : class
{
	private static int _idSequence;
	private List<string> _availableDimensions = new();

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
	public List<string> AvailableDimensions { get; set; } = new();

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

	protected override void OnInitialized()
	{
		base.OnInitialized();
		// Set a unique ID if not provided
		if (Id == $"pd-component-{Sequence}")
		{
			Id = $"pd-graph-controls-{++_idSequence}";
		}
	}

	protected override void OnParametersSet()
	{
		base.OnParametersSet();
		_availableDimensions = AvailableDimensions ?? new List<string>();

		// Add meaningful dimensions for the innovation knowledge graph
		if (_availableDimensions.Count == 0)
		{
			_availableDimensions.AddRange(new[]
			{
				"Influence", "Fame", "Creativity", "Era", "Category",
				"ConnectionStrength", "RelationshipType", "Certainty"
			});
		}
	}

	private async Task OnConfigurationChanged()
	{
		if (!IsReadOnly)
		{
			await ConfigurationChanged.InvokeAsync((VisualizationConfig, ClusteringConfig, Damping)).ConfigureAwait(false);
		}
	}
}