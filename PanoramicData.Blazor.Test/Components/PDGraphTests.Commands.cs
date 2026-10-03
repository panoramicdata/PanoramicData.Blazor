using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// View, physics, configuration and refresh command tests for <see cref="PDGraph{TItem}"/>.
/// </summary>
public partial class PDGraphTests
{
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
	/// Verifies that refreshing after load starts a fresh layout with the new data.
	/// </summary>
	[Fact]
	public async Task Refreshing_starts_a_fresh_layout()
	{
		var component = RenderGraph();

		await component.InvokeAsync(() => component.Instance.RefreshAsync(Xunit.TestContext.Current.CancellationToken));

		_module.Invocations["regenerateLayout"].Should().HaveCount(2);
	}
}
