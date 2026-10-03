using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDGraphViewer{TItem}"/> relays the graph's selection.
/// </summary>
public partial class PDGraphViewerTests
{
	/// <summary>
	/// Verifies that clicking a node raises NodeClick and a selection of that node alone, and shows it in the panel.
	/// </summary>
	[Fact]
	public async Task NodeClick_IsRelayed_AsTheSelection()
	{
		GraphNode? clicked = null;
		var selections = new List<(GraphNode? Node, GraphEdge? Edge)>();
		var viewer = RenderViewer(p => p
			.Add(x => x.NodeClick, node => clicked = node)
			.Add(x => x.SelectionChanged, selection => selections.Add(selection)));
		var node = new GraphNode { Id = "n1" };

		await viewer.InvokeAsync(() => Graph(viewer).Instance.NodeClick.InvokeAsync(node));

		clicked.Should().BeSameAs(node);
		selections.Should().ContainSingle().Which.Should().Be((node, (GraphEdge?)null));
		viewer.FindComponent<PDGraphInfo<GraphData>>().Instance.SelectedNode.Should().BeSameAs(node);
	}

	/// <summary>
	/// Verifies that clicking an edge raises EdgeClick and a selection of that edge alone, and shows it in the panel.
	/// </summary>
	[Fact]
	public async Task EdgeClick_IsRelayed_AsTheSelection()
	{
		GraphEdge? clicked = null;
		var selections = new List<(GraphNode? Node, GraphEdge? Edge)>();
		var viewer = RenderViewer(p => p
			.Add(x => x.EdgeClick, edge => clicked = edge)
			.Add(x => x.SelectionChanged, selection => selections.Add(selection)));
		var edge = new GraphEdge { Id = "e1" };

		await viewer.InvokeAsync(() => Graph(viewer).Instance.EdgeClick.InvokeAsync(edge));

		clicked.Should().BeSameAs(edge);
		selections.Should().ContainSingle().Which.Should().Be(((GraphNode?)null, edge));
		viewer.FindComponent<PDGraphInfo<GraphData>>().Instance.SelectedEdge.Should().BeSameAs(edge);
	}

	/// <summary>
	/// Verifies that a selection change made in the graph is passed on unchanged, with the panel hidden.
	/// </summary>
	[Fact]
	public async Task GraphSelectionChanged_IsPassedOn()
	{
		var selections = new List<(GraphNode? Node, GraphEdge? Edge)>();
		var viewer = RenderViewer(p => p
			.Add(x => x.ShowInfo, false)
			.Add(x => x.SelectionChanged, selection => selections.Add(selection)));
		var node = new GraphNode { Id = "n1" };
		var edge = new GraphEdge { Id = "e1" };

		await viewer.InvokeAsync(() => Graph(viewer).Instance.SelectionChanged.InvokeAsync((node, edge)));

		selections.Should().Equal((node, edge));
	}
}
