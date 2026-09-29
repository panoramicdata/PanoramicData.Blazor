using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDGraphInfo{TItem}"/> lays out its controls and selection panels according to its
/// parameters, passes the selection down and relays configuration changes.
/// </summary>
public class PDGraphInfoTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDGraphInfoTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>By default the panels are split vertically, controls first and selection second.</summary>
	[Fact]
	public void Default_layout_is_a_vertical_split_with_both_panels()
	{
		var component = Render<PDGraphInfo<object>>();

		component.Find(".pdsplitter").ClassList.Should().Contain("vertical");
		component.FindAll(".pdsplitpanel").Should().HaveCount(2);
		component.FindComponents<PDGraphControls<object>>().Should().ContainSingle();
		component.FindComponents<PDGraphSelectionInfo<object>>().Should().ContainSingle();
		component.Instance.Id.Should().MatchRegex("^pd-graph-info-[0-9]+$");
		component.Find(".pd-graph-info").Id.Should().Be(component.Instance.Id);
	}

	/// <summary>A horizontal split direction renders a horizontal splitter.</summary>
	[Fact]
	public void Horizontal_split_direction_renders_a_horizontal_splitter()
	{
		var component = Render<PDGraphInfo<object>>(parameters => parameters
			.Add(p => p.SplitDirection, SplitDirection.Horizontal));

		component.Find(".pdsplitter").ClassList.Should().Contain("horizontal");
		component.FindComponents<PDGraphControls<object>>().Should().ContainSingle();
	}

	/// <summary>With the controls hidden only the selection panel is rendered, with no splitter.</summary>
	[Fact]
	public void Hiding_the_controls_renders_only_the_selection_panel()
	{
		var component = Render<PDGraphInfo<object>>(parameters => parameters
			.Add(p => p.ShowControls, false)
			.Add(p => p.Id, "gi")
			.Add(p => p.CssClass, "extra")
			.Add(p => p.IsVisible, false));

		component.FindAll(".pdsplitter").Should().BeEmpty();
		component.FindComponents<PDGraphControls<object>>().Should().BeEmpty();
		component.FindComponents<PDGraphSelectionInfo<object>>().Should().ContainSingle();
		var root = component.Find(".pd-graph-info");
		root.Id.Should().Be("gi");
		root.ClassList.Should().Contain("extra").And.Contain("d-none");
	}

	/// <summary>Read-only controls hide the editable inputs of the controls panel.</summary>
	[Fact]
	public void Read_only_controls_are_passed_to_the_controls_panel()
	{
		var component = Render<PDGraphInfo<object>>(parameters => parameters.Add(p => p.ReadOnlyControls, true));

		component.FindComponent<PDGraphControls<object>>().Instance.IsReadOnly.Should().BeTrue();
	}

	/// <summary>The selected node parameter reaches the selection panel.</summary>
	[Fact]
	public void Selected_node_parameter_is_shown()
	{
		var component = Render<PDGraphInfo<object>>(parameters => parameters
			.Add(p => p.ShowControls, false)
			.Add(p => p.SelectedNode, new GraphNode { Label = "Given" }));

		component.Find(".item-header strong").TextContent.Should().Be("Node: Given");
	}

	/// <summary>SetSelection updates the component and the rendered selection panel.</summary>
	[Fact]
	public async Task SetSelection_updates_the_selection_panel()
	{
		var component = Render<PDGraphInfo<object>>();
		var edge = new GraphEdge { Label = "picked" };

		await component.InvokeAsync(() => component.Instance.SetSelection(null, edge));

		component.Instance.SelectedEdge.Should().BeSameAs(edge);
		component.Instance.SelectedNode.Should().BeNull();
		component.Find(".item-header strong").TextContent.Should().Be("Edge: picked");
	}

	/// <summary>SetSelection works before any selection panel reference exists to update.</summary>
	[Fact]
	public async Task SetSelection_with_a_node_updates_the_panel_without_controls()
	{
		var component = Render<PDGraphInfo<object>>(parameters => parameters.Add(p => p.ShowControls, false));

		await component.InvokeAsync(() => component.Instance.SetSelection(new GraphNode { Label = "n" }, null));

		component.Find(".item-header strong").TextContent.Should().Be("Node: n");
	}

	/// <summary>
	/// A change made in the controls panel is stored on the component and relayed through ConfigurationChanged.
	/// </summary>
	[Fact]
	public void A_controls_change_is_relayed_and_stored()
	{
		(GraphVisualizationConfig Visualization, GraphClusteringConfig Clustering, double Damping)? received = null;
		var clustering = new GraphClusteringConfig();
		var component = Render<PDGraphInfo<object>>(parameters => parameters
			.Add(p => p.ClusteringConfig, clustering)
			.Add(p => p.ConfigurationChanged, args => received = args));

		component.Find("input[type=checkbox][id$='-clustering-enabled']").Change(true);

		component.WaitForAssertion(() => received.Should().NotBeNull());
		received!.Value.Clustering.IsEnabled.Should().BeTrue();
		component.Instance.ClusteringConfig.Should().BeSameAs(received.Value.Clustering);
		component.Instance.VisualizationConfig.Should().BeSameAs(received.Value.Visualization);
	}
}
