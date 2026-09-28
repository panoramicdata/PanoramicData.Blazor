using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDGraphControls{TItem}"/> offers the visualisation and clustering settings, writes
/// the user's choices into the configuration objects, and reports each change.
/// </summary>
public class PDGraphControlsTests : BunitContext
{
	private readonly List<(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping)> _changes = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDGraphControlsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<PDGraphControls<Node>> RenderControls(
		GraphVisualizationConfig visualization,
		GraphClusteringConfig clustering,
		List<string>? dimensions = null,
		bool isReadOnly = false)
		=> Render<PDGraphControls<Node>>(parameters => parameters
			.Add(p => p.VisualizationConfig, visualization)
			.Add(p => p.ClusteringConfig, clustering)
			.Add(p => p.AvailableDimensions, dimensions ?? ["Size", "Colour"])
			.Add(p => p.IsReadOnly, isReadOnly)
			.Add(p => p.ConfigurationChanged, value => _changes.Add(value)));

	/// <summary>
	/// Verifies that a control with no explicit id still has one on its root element, and an explicit id is kept
	/// and used to build the ids of its inputs.
	/// </summary>
	/// <remarks>
	/// The generated id's exact prefix is not asserted: it depends on a static counter shared with every other
	/// <see cref="PDComponentBase"/>, so it varies with whatever else is being constructed concurrently.
	/// </remarks>
	[Fact]
	public void Id_IsGeneratedOrKept()
	{
		var generated = Render<PDGraphControls<Node>>();
		generated.Instance.Id.Should().NotBeNullOrWhiteSpace();
		generated.Find("div.pd-graph-controls").Id.Should().Be(generated.Instance.Id);

		var explicitId = Render<PDGraphControls<Node>>(parameters => parameters.Add(p => p.Id, "my-controls"));
		explicitId.Instance.Id.Should().Be("my-controls");
		explicitId.Find("#my-controls-clustering-enabled").Should().NotBeNull();
	}

	/// <summary>
	/// Verifies that an invisible control is hidden and that the CSS class is applied.
	/// </summary>
	[Fact]
	public void Invisible_IsHidden()
	{
		var component = Render<PDGraphControls<Node>>(parameters => parameters
			.Add(p => p.IsVisible, false)
			.Add(p => p.CssClass, "side"));

		component.Find("div.pd-graph-controls").ClassList.Should().Contain(["d-none", "side"]);
	}

	/// <summary>
	/// Verifies that read-only controls show a notice and no inputs.
	/// </summary>
	[Fact]
	public void ReadOnly_ShowsNoticeAndNoInputs()
	{
		var component = RenderControls(new(), new(), isReadOnly: true);

		component.Find(".readonly-info").TextContent.Should().Contain("Controls are read-only");
		component.FindAll("input, select").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that when no dimensions are supplied a built-in set is offered in every dimension picker.
	/// </summary>
	[Fact]
	public void NoDimensions_OffersDefaultSet()
	{
		var component = Render<PDGraphControls<Node>>(parameters => parameters
			.Add(p => p.AvailableDimensions, null!));

		var options = component.FindAll("select")[0].QuerySelectorAll("option").Select(o => o.TextContent);
		options.Should().Equal("None", "Influence", "Fame", "Creativity", "Era", "Category",
			"ConnectionStrength", "RelationshipType", "Certainty");
	}

	/// <summary>
	/// Verifies that the supplied dimensions are offered, and that clustering settings stay hidden until
	/// clustering is enabled.
	/// </summary>
	[Fact]
	public void SuppliedDimensions_AreOffered_AndClusteringStartsHidden()
	{
		var component = RenderControls(new(), new());

		var selects = component.FindAll("select");
		selects.Should().HaveCount(9);
		selects[0].QuerySelectorAll("option").Select(o => o.TextContent).Should().Equal("None", "Size", "Colour");
		component.FindAll("input[type=range]").Should().BeEmpty();
		component.Find(".size-range").GetAttribute("style").Should().Contain("display: none");
		component.Find(".thickness-range").GetAttribute("style").Should().Contain("display: none");
	}

	/// <summary>
	/// Verifies that enabling clustering reveals its settings, and that each clustering setting is written back
	/// and reported.
	/// </summary>
	[Fact]
	public void Clustering_SettingsAreWrittenAndReported()
	{
		var clustering = new GraphClusteringConfig();
		var component = RenderControls(new(), clustering);

		component.Find("input[type=checkbox]").Change(true);
		clustering.IsEnabled.Should().BeTrue();

		var ranges = component.FindAll("input[type=range]");
		ranges.Should().HaveCount(2);
		ranges[0].Change("7");
		component.FindAll("input[type=range]")[1].Change("0.97");
		component.FindAll("select")[0].Change(nameof(GraphClusteringAlgorithm.KMeans));
		component.FindAll("select")[1].Change("Colour");

		clustering.MaxClusters.Should().Be(7);
		clustering.Algorithm.Should().Be(GraphClusteringAlgorithm.KMeans);
		clustering.ClusterByDimension.Should().Be("Colour");
		component.Find(".control-group small").TextContent.Should().Be("7 clusters");

		_changes.Should().HaveCount(5);
		_changes[^1].Clustering.Should().BeSameAs(clustering);
		_changes[^1].Damping.Should().Be(0.97);
	}

	/// <summary>
	/// Verifies that every node and edge dimension picker writes its choice back and reports the change, and that
	/// choosing a size or thickness dimension reveals the matching range inputs.
	/// </summary>
	[Fact]
	public void DimensionPickers_AreWrittenAndReported()
	{
		var visualization = new GraphVisualizationConfig();
		var component = RenderControls(visualization, new());

		for (var i = 0; i < 9; i++)
		{
			component.FindAll("select")[i].Change(i % 2 == 0 ? "Size" : "Colour");
		}

		var node = visualization.NodeVisualization;
		var edge = visualization.EdgeVisualization;
		new[] { node.SizeDimension, node.ShapeDimension, node.FillHueDimension, node.FillSaturationDimension,
			node.FillLuminanceDimension, node.StrokeThicknessDimension, edge.ThicknessDimension, edge.HueDimension,
			edge.AlphaDimension }
			.Should().Equal("Size", "Colour", "Size", "Colour", "Size", "Colour", "Size", "Colour", "Size");

		component.Find(".size-range").GetAttribute("style").Should().Contain("display: block");
		component.Find(".thickness-range").GetAttribute("style").Should().Contain("display: block");
		_changes.Should().HaveCount(9);
		_changes[0].Visualization.Should().BeSameAs(visualization);
	}

	/// <summary>
	/// Verifies that the node size and edge thickness range inputs are written back and reported.
	/// </summary>
	[Fact]
	public void RangeInputs_AreWrittenAndReported()
	{
		var visualization = new GraphVisualizationConfig();
		visualization.NodeVisualization.SizeDimension = "Size";
		visualization.EdgeVisualization.ThicknessDimension = "Size";
		var component = RenderControls(visualization, new());

		component.FindAll("input[type=number]")[0].Change("2.5");
		component.FindAll("input[type=number]")[1].Change("30");
		component.FindAll("input[type=number]")[2].Change("0.2");
		component.FindAll("input[type=number]")[3].Change("8");

		visualization.NodeVisualization.MinSize.Should().Be(2.5);
		visualization.NodeVisualization.MaxSize.Should().Be(30);
		visualization.EdgeVisualization.MinThickness.Should().Be(0.2);
		visualization.EdgeVisualization.MaxThickness.Should().Be(8);
		_changes.Should().HaveCount(4);
	}

	/// <summary>A graph item type; the controls never read it.</summary>
	private sealed class Node;
}
