using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor;

/// <summary>
/// Container component for graph controls and selection information panels.
/// </summary>
/// <typeparam name="TItem">The type of data items used to generate graph data.</typeparam>
public partial class PDGraphInfo<TItem> : PDComponentBase where TItem : class
{
	private static int _idSequence;
	private PDSplitter? _splitter;
	private PDGraphControls<TItem>? _controls;
	private PDGraphSelectionInfo<TItem>? _selectionInfo;

	/// <summary>
	/// Gets or sets the split direction for the controls and selection info panels.
	/// </summary>
	[Parameter]
	public SplitDirection SplitDirection { get; set; } = SplitDirection.Vertical;

	/// <summary>
	/// Gets or sets whether to show the controls panel.
	/// </summary>
	[Parameter]
	public bool ShowControls { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the controls are read-only.
	/// </summary>
	[Parameter]
	public bool ReadOnlyControls { get; set; }

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
	/// Gets or sets the currently selected node.
	/// </summary>
	[Parameter]
	public GraphNode? SelectedNode { get; set; }

	/// <summary>
	/// Gets or sets the currently selected edge.
	/// </summary>
	[Parameter]
	public GraphEdge? SelectedEdge { get; set; }

	/// <summary>
	/// Gets or sets a callback that is invoked when the configuration changes.
	/// </summary>
	[Parameter]
	public EventCallback<(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping)> ConfigurationChanged { get; set; }

	protected override void OnInitialized()
	{
		base.OnInitialized();
		// Set a unique ID if not provided
		if (Id == $"pd-component-{Sequence}")
		{
			Id = $"pd-graph-info-{++_idSequence}";
		}
	}

	/// <summary>
	/// Sets the current selection for display in the selection info panel.
	/// </summary>
	/// <param name="node">The selected node, or null if no node is selected.</param>
	/// <param name="edge">The selected edge, or null if no edge is selected.</param>
	public void SetSelection(GraphNode? node, GraphEdge? edge)
	{
		SelectedNode = node;
		SelectedEdge = edge;
		
		if (_selectionInfo is not null)
		{
			_selectionInfo.UpdateSelection(node, edge);
		}
		
		StateHasChanged();
	}

	private async Task OnConfigurationChanged((GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping) config)
	{
		VisualizationConfig = config.Visualization;
		ClusteringConfig = config.Clustering;
		await ConfigurationChanged.InvokeAsync(config).ConfigureAwait(false);
	}
}