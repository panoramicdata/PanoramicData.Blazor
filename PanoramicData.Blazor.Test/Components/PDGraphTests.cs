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
public partial class PDGraphTests : BunitContext
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

	private static JsonElement Json(string text)
	{
		using var document = JsonDocument.Parse(text);
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
			ArgumentNullException.ThrowIfNull(request);
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
