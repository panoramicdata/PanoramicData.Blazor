using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDGraphViewer{TItem}"/> lays out the graph beside its information panel, relays
/// selection and configuration between them, and passes view commands to the graph.
/// </summary>
public class PDGraphViewerTests : BunitContext
{
	private const string GraphModulePath = "./_content/PanoramicData.Blazor/PDGraph.razor.js";
	private readonly GraphProvider _provider = new();
	private readonly BunitJSModuleInterop _graphModule;

	/// <summary>Sets up the rendering context.</summary>
	public PDGraphViewerTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		_graphModule = JSInterop.SetupModule(GraphModulePath);
	}

	private IRenderedComponent<PDGraphViewer<GraphData>> RenderViewer(Action<ComponentParameterCollectionBuilder<PDGraphViewer<GraphData>>>? configure = null)
		=> Render<PDGraphViewer<GraphData>>(parameters =>
		{
			parameters
				.Add(p => p.Id, "viewer")
				.Add(p => p.DataProvider, _provider);
			configure?.Invoke(parameters);
		});

	private static IRenderedComponent<PDGraph<GraphData>> Graph(IRenderedComponent<PDGraphViewer<GraphData>> viewer)
		=> viewer.FindComponent<PDGraph<GraphData>>();

	/// <summary>
	/// Verifies that with the information panel shown, the graph and the panel share a splitter in the
	/// requested direction, the panel being laid out across it.
	/// </summary>
	[Theory]
	[InlineData(SplitDirection.Horizontal, SplitDirection.Vertical)]
	[InlineData(SplitDirection.Vertical, SplitDirection.Horizontal)]
	public void ShowInfo_SplitsTheGraphAndThePanel(SplitDirection direction, SplitDirection panelDirection)
	{
		var viewer = RenderViewer(p => p
			.Add(x => x.SplitDirection, direction)
			.Add(x => x.CssClass, "my-graph"));

		viewer.Find("div.pd-graph-viewer").ClassList.Should().Contain("my-graph");
		viewer.Find("div.pd-graph-viewer").Id.Should().Be("viewer");
		viewer.FindComponent<PDSplitter>().Instance.Direction.Should().Be(direction);
		viewer.FindComponents<PDGraph<GraphData>>().Should().ContainSingle();
		viewer.FindComponent<PDGraphInfo<GraphData>>().Instance.SplitDirection.Should().Be(panelDirection);
	}

	/// <summary>
	/// Verifies that without the information panel the graph is rendered on its own.
	/// </summary>
	[Fact]
	public void HideInfo_RendersTheGraphAlone()
	{
		var viewer = RenderViewer(p => p
			.Add(x => x.ShowInfo, false)
			.Add(x => x.IsVisible, false));

		viewer.Find("div.pd-graph-viewer").ClassList.Should().Contain("d-none");
		viewer.FindComponents<PDSplitter>().Should().BeEmpty();
		viewer.FindComponents<PDGraphInfo<GraphData>>().Should().BeEmpty();
		viewer.FindComponents<PDGraph<GraphData>>().Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that the physics and read-only settings reach the graph and the panel.
	/// </summary>
	[Fact]
	public void Parameters_ReachTheGraphAndThePanel()
	{
		var viewer = RenderViewer(p => p
			.Add(x => x.ConvergenceThreshold, 0.5)
			.Add(x => x.Damping, 0.7)
			.Add(x => x.ShowControls, false)
			.Add(x => x.ReadOnlyControls, true));

		Graph(viewer).Instance.ConvergenceThreshold.Should().Be(0.5);
		Graph(viewer).Instance.Damping.Should().Be(0.7);
		var info = viewer.FindComponent<PDGraphInfo<GraphData>>().Instance;
		info.ShowControls.Should().BeFalse();
		info.ReadOnlyControls.Should().BeTrue();
	}

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

	/// <summary>
	/// Verifies that a configuration change made in the panel is applied to the viewer and the graph, and announced.
	/// </summary>
	[Fact]
	public async Task PanelConfigurationChange_IsAppliedAndAnnounced()
	{
		var announced = new List<(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping)>();
		var viewer = RenderViewer(p => p.Add(x => x.ConfigurationChanged, config => announced.Add(config)));
		var config = (new GraphVisualizationConfig(), new GraphClusteringConfig(), 0.5);
		var info = viewer.FindComponent<PDGraphInfo<GraphData>>().Instance;

		await viewer.InvokeAsync(() => info.ConfigurationChanged.InvokeAsync(config));

		announced.Should().Equal(config);
		viewer.Instance.VisualizationConfig.Should().BeSameAs(config.Item1);
		viewer.Instance.ClusteringConfig.Should().BeSameAs(config.Item2);
		viewer.Instance.Damping.Should().Be(0.5);
		Graph(viewer).Instance.VisualizationConfig.Should().BeSameAs(config.Item1);
		_graphModule.Invocations["updateConfiguration"].Should().NotBeEmpty();
	}

	/// <summary>
	/// Verifies that refreshing the viewer asks the data provider for the graph again.
	/// </summary>
	[Fact]
	public async Task RefreshAsync_ReloadsTheGraphData()
	{
		var viewer = RenderViewer();
		viewer.WaitForState(() => _provider.Requests > 0);
		var before = _provider.Requests;

		await viewer.InvokeAsync(() => viewer.Instance.RefreshAsync());

		_provider.Requests.Should().Be(before + 1);
	}

	/// <summary>
	/// Verifies that the view commands reach the graph's script: fitting the whole graph, and centring on a
	/// node that the graph knows about.
	/// </summary>
	[Fact]
	public async Task ViewCommands_ReachTheGraphScript()
	{
		var viewer = RenderViewer();
		viewer.WaitForState(() => _provider.Requests > 0);
		await viewer.InvokeAsync(() => viewer.Instance.RefreshAsync());

		await viewer.InvokeAsync(() => viewer.Instance.FitToViewAsync());
		await viewer.InvokeAsync(() => viewer.Instance.CenterOnNodeAsync("a"));

		_graphModule.VerifyInvoke("fitToView").Arguments[0].Should().Be(Graph(viewer).Instance.Id);
		_graphModule.VerifyInvoke("centerOnNode").Arguments[0].Should().Be(Graph(viewer).Instance.Id);
	}

	/// <summary>A provider that returns a two-node graph and counts requests.</summary>
	private sealed class GraphProvider : DataProviderBase<GraphData>
	{
		public int Requests { get; private set; }

		public override Task<DataResponse<GraphData>> GetDataAsync(DataRequest<GraphData> request, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(request);
			cancellationToken.ThrowIfCancellationRequested();
			Requests++;
			var graph = new GraphData
			{
				Nodes = [new GraphNode { Id = "a", Label = "A" }, new GraphNode { Id = "b", Label = "B" }],
				Edges = [new GraphEdge { Id = "ab", FromNodeId = "a", ToNodeId = "b" }]
			};
			return Task.FromResult(new DataResponse<GraphData>([graph], 1));
		}
	}
}
