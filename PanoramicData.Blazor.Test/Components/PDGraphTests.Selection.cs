using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Node and edge selection tests for <see cref="PDGraph{TItem}"/>.
/// </summary>
public partial class PDGraphTests
{
	/// <summary>
	/// Verifies that clicking a node selects it alone, updates the module and raises both events.
	/// </summary>
	[Fact]
	public async Task Clicking_a_node_selects_it_and_raises_events()
	{
		var data = CreateData();
		data.Edges[0].IsSelected = true;
		GraphNode? clicked = null;
		(GraphNode? Node, GraphEdge? Edge) selection = default;
		var component = RenderGraph(p => p
			.Add(x => x.NodeClick, n => clicked = n)
			.Add(x => x.SelectionChanged, s => selection = s), data);
		await ReportPositionsAsync(component, ("n1", 0, 0), ("n2", 1, 1));

		await component.InvokeAsync(() => component.FindAll("g.graph-node")[1].ClickAsync(new MouseEventArgs()));

		clicked.Should().BeSameAs(data.Nodes[1]);
		selection.Node.Should().BeSameAs(data.Nodes[1]);
		selection.Edge.Should().BeNull();
		data.Nodes.Select(n => n.IsSelected).Should().Equal(false, true, false);
		data.Edges[0].IsSelected.Should().BeFalse();
		_module.VerifyInvoke("updateSelection").Arguments.Should().Equal(GraphId, "n2", "node");
		_module.VerifyInvoke("setFocusNode").Arguments.Should().Equal(GraphId, "n2");
	}

	/// <summary>
	/// Verifies that clicking an edge selects it alone, updates the module and raises both events.
	/// </summary>
	[Fact]
	public async Task Clicking_an_edge_selects_it_and_raises_events()
	{
		var data = CreateData();
		data.Nodes[0].IsSelected = true;
		GraphEdge? clicked = null;
		(GraphNode? Node, GraphEdge? Edge) selection = default;
		var component = RenderGraph(p => p
			.Add(x => x.EdgeClick, e => clicked = e)
			.Add(x => x.SelectionChanged, s => selection = s), data);
		await ReportPositionsAsync(component, ("n1", 0, 0), ("n2", 1, 1));

		await component.InvokeAsync(() => component.Find("line.graph-edge").ClickAsync(new MouseEventArgs()));

		clicked.Should().BeSameAs(data.Edges[0]);
		selection.Edge.Should().BeSameAs(data.Edges[0]);
		selection.Node.Should().BeNull();
		data.Nodes.Should().OnlyContain(n => !n.IsSelected);
		data.Edges[0].IsSelected.Should().BeTrue();
		_module.VerifyInvoke("updateSelection").Arguments.Should().Equal(GraphId, "e1", "edge");
	}

	/// <summary>
	/// Verifies that a selected node and edge are drawn with the selected class.
	/// </summary>
	[Fact]
	public async Task Selected_items_are_drawn_selected()
	{
		var data = CreateData();
		data.Nodes[0].IsSelected = true;
		data.Edges[0].IsSelected = true;
		var component = RenderGraph(data: data);

		await ReportPositionsAsync(component, ("n1", 0, 0), ("n2", 1, 1));

		component.FindAll("g.graph-node")[0].ClassList.Should().Contain("selected");
		component.Find("line.graph-edge").ClassList.Should().Contain("selected");
	}

	/// <summary>
	/// Verifies that a node click reported from JavaScript is handled as a click on that node, and that an
	/// unknown or malformed node is ignored.
	/// </summary>
	[Fact]
	public async Task A_node_click_from_javascript_clicks_that_node()
	{
		var clicked = new List<GraphNode>();
		var data = CreateData();
		var component = RenderGraph(p => p.Add(x => x.NodeClick, n => clicked.Add(n)), data);

		await component.InvokeAsync(() => component.Instance.OnNodeClickedFromJS(Json("{\"id\":\"n3\"}")));
		await component.InvokeAsync(() => component.Instance.OnNodeClickedFromJS(Json("{\"id\":\"missing\"}")));
		await component.InvokeAsync(() => component.Instance.OnNodeClickedFromJS(Json("{\"label\":\"no id\"}")));

		clicked.Should().ContainSingle().Which.Should().BeSameAs(data.Nodes[2]);
	}
}
