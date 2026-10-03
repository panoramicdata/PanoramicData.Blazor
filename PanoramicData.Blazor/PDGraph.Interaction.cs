using System.Text.Json;

namespace PanoramicData.Blazor;

/// <summary>
/// The selection handling and JavaScript callbacks of <see cref="PDGraph{TItem}"/>.
/// </summary>
public partial class PDGraph<TItem>
{
	private async Task OnNodeClick(GraphNode node)
	{
		// ✅ FIXED: Set flag to prevent re-initialization
		_isUpdatingSelection = true;

		try
		{
			ClearSelection();

			// Select the clicked node
			node.IsSelected = true;

			// ✅ FIXED: Update selection in JavaScript without regenerating layout
			if (Module != null)
			{
				await Module.InvokeVoidAsync("updateSelection", Id, node.Id, "node").ConfigureAwait(true);
				await Module.InvokeVoidAsync("setFocusNode", Id, node.Id).ConfigureAwait(true);
			}

			await NodeClick.InvokeAsync(node).ConfigureAwait(true);
			await SelectionChanged.InvokeAsync((node, null)).ConfigureAwait(true);

			// ✅ FIXED: Don't call StateHasChanged() here to avoid triggering refresh
		}
		finally
		{
			_isUpdatingSelection = false;
		}
	}

	private async Task OnEdgeClick(GraphEdge edge)
	{
		// ✅ FIXED: Set flag to prevent re-initialization
		_isUpdatingSelection = true;

		try
		{
			ClearSelection();

			// Select the clicked edge
			edge.IsSelected = true;

			// ✅ FIXED: Update selection in JavaScript without regenerating layout
			if (Module != null)
			{
				await Module.InvokeVoidAsync("updateSelection", Id, edge.Id, "edge").ConfigureAwait(true);
			}

			await EdgeClick.InvokeAsync(edge).ConfigureAwait(true);
			await SelectionChanged.InvokeAsync((null, edge)).ConfigureAwait(true);

			// ✅ FIXED: Don't call StateHasChanged() here to avoid triggering refresh
		}
		finally
		{
			_isUpdatingSelection = false;
		}
	}

	/// <summary>
	/// Deselects every node and edge.
	/// </summary>
	private void ClearSelection()
	{
		foreach (var n in _graphData?.Nodes ?? [])
		{
			n.IsSelected = false;
		}

		foreach (var e in _graphData?.Edges ?? [])
		{
			e.IsSelected = false;
		}
	}

	/// <summary>
	/// Called from JavaScript to update the stored positions of all graph nodes.
	/// </summary>
	/// <param name="positions">A dictionary mapping node identifiers to their x/y coordinate objects.</param>
	[JSInvokable]
	public void UpdateNodePositions(Dictionary<string, object> positions)
	{
		_nodePositions.Clear();
		foreach (var kvp in positions)
		{
			if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Object)
			{
				var x = element.GetProperty("x").GetDouble();
				var y = element.GetProperty("y").GetDouble();
				_nodePositions[kvp.Key] = (x, y);
			}
		}
		// Remove StateHasChanged() here to prevent interference with hover
		// The positions will be updated when the next render cycle occurs naturally
	}

	/// <summary>
	/// Called from JavaScript to update the SVG transform matrix string used for pan and zoom state.
	/// </summary>
	/// <param name="transform">The current SVG transform matrix as a string (e.g. <c>"matrix(1,0,0,1,0,0)"</c>).</param>
	[JSInvokable]
	public void UpdateTransform(string transform)
	{
		_transformMatrix = transform;
		// Remove StateHasChanged() here to prevent unnecessary re-renders during smooth animations
	}

	/// <summary>
	/// JavaScript interop method called when a node is clicked from the JavaScript side.
	/// </summary>
	/// <param name="nodeData">The node data from JavaScript.</param>
	[JSInvokable]
	public async Task OnNodeClickedFromJS(JsonElement nodeData)
	{
		try
		{
			Console.WriteLine($"Node clicked from JS: {nodeData}");

			// Find the corresponding node in our graph data
			if (_graphData?.Nodes != null)
			{
				var nodeId = nodeData.GetProperty("id").GetString();
				var node = _graphData.Nodes.FirstOrDefault(n => n.Id == nodeId);

				if (node != null)
				{
					Console.WriteLine($"Found node: {node.Label}, invoking click handler");
					await OnNodeClick(node).ConfigureAwait(true);
				}
				else
				{
					Console.WriteLine($"Node with ID {nodeId} not found in graph data");
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error handling node click from JS: {ex.Message}");
		}
	}
}
