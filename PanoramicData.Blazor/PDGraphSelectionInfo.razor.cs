namespace PanoramicData.Blazor;

/// <summary>
/// Read-only component for displaying information about selected nodes and edges.
/// </summary>
/// <typeparam name="TItem">The type of data items used to generate graph data.</typeparam>
public partial class PDGraphSelectionInfo<TItem> : PDComponentBase where TItem : class
{
	private static int _idSequence;

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

	protected override void OnInitialized()
	{
		base.OnInitialized();
		// Set a unique ID if not provided
		if (Id == $"pd-component-{Sequence}")
		{
			Id = $"pd-graph-selection-info-{++_idSequence}";
		}
	}

	/// <summary>
	/// Updates the current selection and refreshes the display.
	/// </summary>
	/// <param name="node">The selected node, or null if no node is selected.</param>
	/// <param name="edge">The selected edge, or null if no edge is selected.</param>
	public void UpdateSelection(GraphNode? node, GraphEdge? edge)
	{
		SelectedNode = node;
		SelectedEdge = edge;
		StateHasChanged();
	}
}