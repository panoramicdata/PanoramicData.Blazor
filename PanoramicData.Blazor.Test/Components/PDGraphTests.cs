using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDGraph{TItem}"/> loads its data, hands it to the layout module, draws nodes and
/// edges styled from their dimensions at the positions JavaScript reports, raises selection events, and
/// forwards its view and configuration commands to the module.
/// </summary>
public class PDGraphTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDGraph.razor.js";
	private const string GraphId = "graph";

	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context and the graph module.</summary>
	public PDGraphTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	/// <summary>
	/// Verifies that the container carries the id and CSS class, and is hidden when not visible.
	/// </summary>
	[Fact]
	public void The_container_carries_its_id_and_classes()
	{
		var component = RenderGraph(p => p.Add(x => x.CssClass, "extra").Add(x => x.IsVisible, false));

		var root = component.Find(".pd-graph");
		root.Id.Should().Be(GraphId);
		root.ClassList.Should().Contain(["extra", "d-none"]);
	}

	/// <summary>
	/// Verifies that the module is initialised and the layout started with the loaded data.
	/// </summary>
	[Fact]
	public void First_render_initialises_the_module_and_starts_the_layout()
	{
		var clustering = new GraphClusteringConfig { MaxClusters = 3 };
		var data = CreateData();

		RenderGraph(p => p.Add(x => x.ClusteringConfig, clustering), data);

		var initialize = _module.VerifyInvoke("initialize");
		initialize.Arguments[0].Should().Be(GraphId);
		initialize.Arguments[2].Should().BeSameAs(clustering);
		var layout = _module.VerifyInvoke("regenerateLayout");
		layout.Arguments.Should().Equal(GraphId, data, 0.02, clustering);
	}

	/// <summary>
	/// Verifies that nodes and edges are drawn once JavaScript has reported their positions.
	/// </summary>
	[Fact]
	public async Task Nodes_and_edges_are_drawn_at_their_reported_positions()
	{
		var component = RenderGraph();

		await ReportPositionsAsync(component, ("n1", 10, 20), ("n2", 30, 40));

		var nodes = component.FindAll("g.graph-node");
		nodes.Should().HaveCount(2);
		nodes[0].GetAttribute("transform").Should().Be("translate(10,20)");
		nodes[0].GetAttribute("data-node-id").Should().Be("n1");
		var edge = component.Find("line.graph-edge");
		edge.GetAttribute("data-edge-id").Should().Be("e1");
		edge.GetAttribute("marker-end").Should().Be($"url(#{GraphId}-arrowhead)");
		edge.QuerySelector("title")!.TextContent.Should().Be("links");
		component.FindAll("line.graph-edge").Should().ContainSingle("the edge to an unplaced node is not drawn");
	}

	/// <summary>
	/// Verifies the default node style: a circle of middle size, filled and stroked from the defaults,
	/// with a truncated label in contrasting text.
	/// </summary>
	[Fact]
	public async Task A_node_without_dimensions_uses_the_default_style()
	{
		var component = RenderGraph();

		await ReportPositionsAsync(component, ("n1", 0, 0));

		var shape = component.Find("g.graph-node .node-shape");
		shape.LocalName.Should().Be("circle");
		shape.GetAttribute("fill").Should().Be("hsl(216, 70%, 50%)");
		shape.GetAttribute("stroke").Should().Be("hsl(0, 0%, 0%)");
		shape.GetAttribute("stroke-dasharray").Should().Be("none");
		var label = component.Find("g.graph-node text.node-label");
		label.TextContent.Should().Be("Alp...");
		label.GetAttribute("fill").Should().Be("#ffffff");
		component.Find("g.graph-node title").TextContent.Should().StartWith("Alpha");
	}

	/// <summary>
	/// Verifies that the shape dimension selects the node shape.
	/// </summary>
	[Theory]
	[InlineData(0.0, "circle", "r")]
	[InlineData(0.2, "ellipse", "rx")]
	[InlineData(0.4, "polygon", "points")]
	[InlineData(0.6, "polygon", "points")]
	[InlineData(0.8, "rect", "width")]
	[InlineData(1.0, "rect", "width")]
	public async Task The_shape_dimension_selects_the_shape(double shapeValue, string tag, string sizeAttribute)
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.ShapeDimension = "shape";
		var data = CreateData();
		data.Nodes[0].Dimensions["shape"] = shapeValue;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0));

		var shape = component.Find("g.graph-node .node-shape");
		shape.LocalName.Should().Be(tag);
		shape.HasAttribute(sizeAttribute).Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the octagon and diamond differ in their number of points, and square and rectangle in
	/// their proportions.
	/// </summary>
	[Theory]
	[InlineData(0.4, 4)]
	[InlineData(0.6, 8)]
	public async Task Polygon_shapes_have_their_number_of_points(double shapeValue, int points)
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.ShapeDimension = "shape";
		var data = CreateData();
		data.Nodes[0].Dimensions["shape"] = shapeValue;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0));

		component.Find("g.graph-node polygon").GetAttribute("points")!.Split(' ').Should().HaveCount(points);
	}

	/// <summary>
	/// Verifies that a rectangle is wider than it is tall, while a square is not.
	/// </summary>
	[Theory]
	[InlineData(0.8, false)]
	[InlineData(1.0, true)]
	public async Task A_rectangle_is_wider_than_a_square(double shapeValue, bool wider)
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.ShapeDimension = "shape";
		config.NodeVisualization.SizeDimension = "size";
		var data = CreateData();
		data.Nodes[0].Dimensions["shape"] = shapeValue;
		data.Nodes[0].Dimensions["size"] = 1;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0));

		var rect = component.Find("g.graph-node rect");
		rect.GetAttribute("height").Should().Be("40");
		(rect.GetAttribute("width") == "60").Should().Be(wider);
	}

	/// <summary>
	/// Verifies that dimension values are clamped, and that a light fill gets dark label text.
	/// </summary>
	[Fact]
	public async Task Dimensions_are_clamped_and_light_fills_get_dark_text()
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.SizeDimension = "size";
		config.NodeVisualization.FillLuminanceDimension = "light";
		var data = CreateData();
		data.Nodes[0].Dimensions["size"] = 5;
		data.Nodes[0].Dimensions["light"] = 0.8;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0));

		component.Find("g.graph-node circle").GetAttribute("r").Should().Be("20");
		component.Find("g.graph-node circle").GetAttribute("fill").Should().Be("hsl(216, 70%, 80%)");
		component.Find("text.node-label").GetAttribute("fill").Should().Be("#212529");
		component.Find("text.node-label").TextContent.Should().Be("Alpha");
	}

	/// <summary>
	/// Verifies that the pattern dimension selects solid, dotted or dashed strokes.
	/// </summary>
	[Theory]
	[InlineData(0.05, "none")]
	[InlineData(0.3, "2,2")]
	[InlineData(0.7, "5,5")]
	[InlineData(1.0, "none")]
	public async Task The_pattern_dimension_selects_the_stroke_pattern(double pattern, string expected)
	{
		var config = new GraphVisualizationConfig();
		config.NodeVisualization.StrokePatternDimension = "pattern";
		config.EdgeVisualization.PatternDimension = "pattern";
		var data = CreateData();
		data.Nodes[0].Dimensions["pattern"] = pattern;
		data.Edges[0].Dimensions["pattern"] = pattern;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0), ("n2", 1, 1));

		component.Find("g.graph-node .node-shape").GetAttribute("stroke-dasharray").Should().Be(expected);
		component.Find("line.graph-edge").GetAttribute("stroke-dasharray").Should().Be(expected);
	}

	/// <summary>
	/// Verifies the default edge style and that edge dimensions change it.
	/// </summary>
	[Fact]
	public async Task Edges_are_styled_from_their_dimensions()
	{
		var config = new GraphVisualizationConfig();
		config.EdgeVisualization.HueDimension = "hue";
		config.EdgeVisualization.ThicknessDimension = "weight";
		var data = CreateData();
		data.Edges[0].Dimensions["hue"] = 0.5;
		data.Edges[0].Dimensions["weight"] = 1;
		var component = RenderGraph(p => p.Add(x => x.VisualizationConfig, config), data);

		await ReportPositionsAsync(component, ("n1", 0, 0), ("n2", 1, 1));

		var edge = component.Find("line.graph-edge");
		edge.GetAttribute("stroke").Should().Be("hsl(180, 0%, 40%)");
		edge.GetAttribute("stroke-width").Should().Be("5");
	}

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

		component.FindAll("g.graph-node")[1].Click();

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

		component.Find("line.graph-edge").Click();

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

	/// <summary>
	/// Verifies that centring moves to a placed node's position and ignores an unknown node.
	/// </summary>
	[Fact]
	public async Task Centring_on_a_node_uses_its_reported_position()
	{
		var component = RenderGraph();
		await ReportPositionsAsync(component, ("n1", 12, 34));

		await component.InvokeAsync(() => component.Instance.CenterOnNodeAsync("n1"));
		await component.InvokeAsync(() => component.Instance.CenterOnNodeAsync("missing"));

		_module.VerifyInvoke("centerOnNode").Arguments.Should().Equal(GraphId, 12d, 34d);
	}

	/// <summary>
	/// Verifies that the view and physics commands are forwarded to the module.
	/// </summary>
	[Fact]
	public async Task View_and_physics_commands_are_forwarded()
	{
		var component = RenderGraph();

		await component.InvokeAsync(component.Instance.FitToViewAsync);
		await component.InvokeAsync(() => component.Instance.UpdatePhysicsParametersAsync(0.1));

		_module.VerifyInvoke("fitToView").Arguments.Should().Equal(GraphId);
		_module.VerifyInvoke("updatePhysicsParameters").Arguments.Should().Equal(GraphId, 0.1);
		component.Instance.ConvergenceThreshold.Should().Be(0.1);
	}

	/// <summary>
	/// Verifies that UpdateConfigurationAsync restyles through the module and the markup.
	/// </summary>
	[Fact]
	public async Task UpdateConfigurationAsync_restyles_the_graph()
	{
		var data = CreateData();
		var component = RenderGraph(data: data);
		await ReportPositionsAsync(component, ("n1", 0, 0));
		var config = new GraphVisualizationConfig();
		config.Defaults.NodeFillHue = 0;

		await component.InvokeAsync(() => component.Instance.UpdateConfigurationAsync(config, new GraphClusteringConfig()));

		_module.VerifyInvoke("updateConfiguration").Arguments.Should().Equal(GraphId, data);
		component.Find("g.graph-node circle").GetAttribute("fill").Should().Be("hsl(0, 70%, 50%)");
	}

	/// <summary>
	/// Verifies that UpdateConfiguration restyles the markup.
	/// </summary>
	[Fact]
	public async Task UpdateConfiguration_restyles_the_markup()
	{
		var component = RenderGraph();
		await ReportPositionsAsync(component, ("n1", 0, 0));
		var config = new GraphVisualizationConfig();
		config.Defaults.NodeFillSaturation = 0;

		await component.InvokeAsync(() => component.Instance.UpdateConfiguration(config, new GraphClusteringConfig()));

		component.Find("g.graph-node circle").GetAttribute("fill").Should().Be("hsl(216, 0%, 50%)");
	}

	/// <summary>
	/// Verifies that new configuration parameters update the module configuration.
	/// </summary>
	[Fact]
	public void New_configuration_parameters_update_the_module()
	{
		var data = CreateData();
		var clustering = new GraphClusteringConfig();
		var component = RenderGraph(data: data);

		component.Render(parameters => parameters.Add(p => p.ClusteringConfig, clustering));

		_module.VerifyInvoke("updateConfiguration").Arguments.Should().Equal(GraphId, data, clustering);
	}

	/// <summary>
	/// Verifies that new physics parameters update the module physics, and unchanged ones do nothing.
	/// </summary>
	[Fact]
	public void New_physics_parameters_update_the_module()
	{
		var component = RenderGraph();

		component.Render(parameters => parameters.Add(p => p.Damping, 0.95));
		_module.Invocations["updatePhysicsParameters"].Should().BeEmpty();

		component.Render(parameters => parameters.Add(p => p.Damping, 0.5).Add(p => p.ConvergenceThreshold, 0.3));
		_module.VerifyInvoke("updatePhysicsParameters").Arguments.Should().Equal(GraphId, 0.3, 0.5);
	}

	/// <summary>
	/// Verifies that the pan and zoom transform reported from JavaScript is applied on the next render.
	/// </summary>
	[Fact]
	public async Task The_reported_transform_is_applied()
	{
		var component = RenderGraph();

		await component.InvokeAsync(() => component.Instance.UpdateTransform("translate(5,6) scale(2)"));
		component.Render();

		component.Find("g.nodes-group").GetAttribute("transform").Should().Be("translate(5,6) scale(2)");
		component.Find("g.edges-group").GetAttribute("transform").Should().Be("translate(5,6) scale(2)");
	}

	/// <summary>
	/// Verifies that a failing data provider shows the error message.
	/// </summary>
	[Fact]
	public void A_failing_provider_shows_the_error()
	{
		var component = Render<PDGraph<GraphNode>>(parameters => parameters
			.Add(p => p.Id, GraphId)
			.Add(p => p.DataProvider, new GraphProvider(null)));

		component.Find(".pd-graph-error .alert-danger").TextContent.Should().Contain("Failed to load graph data");
		component.FindAll("svg").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that an empty response draws an empty graph.
	/// </summary>
	[Fact]
	public void An_empty_response_draws_an_empty_graph()
	{
		var component = Render<PDGraph<GraphNode>>(parameters => parameters
			.Add(p => p.Id, GraphId)
			.Add(p => p.DataProvider, new GraphProvider(new GraphData(), empty: true)));

		component.Find("svg.graph-svg").Should().NotBeNull();
		component.FindAll("g.graph-node").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that refreshing after load starts a fresh layout with the new data.
	/// </summary>
	[Fact]
	public async Task Refreshing_starts_a_fresh_layout()
	{
		var component = RenderGraph();

		await component.InvokeAsync(() => component.Instance.RefreshAsync(Xunit.TestContext.Current.CancellationToken));

		_module.Invocations["regenerateLayout"].Should().HaveCount(2);
	}

	/// <summary>
	/// Verifies that disposing destroys the graph in the module.
	/// </summary>
	[Fact]
	public async Task Disposing_destroys_the_graph()
	{
		var component = RenderGraph();

		await component.Instance.DisposeAsync();

		_module.VerifyInvoke("destroy").Arguments.Should().Equal(GraphId);
	}

	private IRenderedComponent<PDGraph<GraphNode>> RenderGraph(
		Action<ComponentParameterCollectionBuilder<PDGraph<GraphNode>>>? configure = null,
		GraphData? data = null)
		=> Render<PDGraph<GraphNode>>(parameters =>
		{
			parameters
				.Add(p => p.Id, GraphId)
				.Add(p => p.DataProvider, new GraphProvider(data ?? CreateData()));
			configure?.Invoke(parameters);
		});

	private static async Task ReportPositionsAsync(
		IRenderedComponent<PDGraph<GraphNode>> component,
		params (string Id, double X, double Y)[] positions)
	{
		var reported = positions.ToDictionary(
			p => p.Id,
			p => (object)Json($"{{\"x\":{p.X.ToString(CultureInfo.InvariantCulture)},\"y\":{p.Y.ToString(CultureInfo.InvariantCulture)}}}"));
		reported["ignored"] = "not an object";

		await component.InvokeAsync(() => component.Instance.UpdateNodePositions(reported));
		component.Render();
	}

	private static JsonElement Json(string json)
	{
		using var document = JsonDocument.Parse(json);
		return document.RootElement.Clone();
	}

	private static GraphData CreateData() => new()
	{
		Nodes =
		[
			new GraphNode { Id = "n1", Label = "Alpha" },
			new GraphNode { Id = "n2", Label = "Beta" },
			new GraphNode { Id = "n3", Label = "Gamma" }
		],
		Edges =
		[
			new GraphEdge { Id = "e1", FromNodeId = "n1", ToNodeId = "n2", Label = "links" },
			new GraphEdge { Id = "e2", FromNodeId = "n1", ToNodeId = "n3" }
		]
	};

	/// <summary>
	/// Supplies one graph, no graph, or a failure.
	/// </summary>
	/// <param name="data">The graph to supply, or null to fail.</param>
	/// <param name="empty">Whether to supply no items at all.</param>
	private sealed class GraphProvider(GraphData? data, bool empty = false) : DataProviderBase<GraphData>
	{
		/// <inheritdoc />
		public override Task<DataResponse<GraphData>> GetDataAsync(DataRequest<GraphData> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (data is null)
			{
				throw new InvalidOperationException("No graph");
			}

			List<GraphData> items = empty ? [] : [data];
			return Task.FromResult(new DataResponse<GraphData>(items, items.Count));
		}
	}
}
