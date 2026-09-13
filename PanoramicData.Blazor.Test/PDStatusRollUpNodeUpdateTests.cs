using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDStatusRollUp"/> keeps the pop-over in step with the node it is given.
/// </summary>
/// <remarks>
/// The trigger icon is rendered by Blazor from <c>Node.Status</c> on every render, but the pop-over is
/// drawn by the JavaScript module from a snapshot of the node. Until this work that snapshot was taken
/// once, on first render, and never refreshed (issue #139) - so a node that changed afterwards produced a live icon
/// beside a pop-over describing the state the component started in. In the Magic Suite Overview that
/// read as a red row whose pop-over insisted "All available checks are healthy".
/// </remarks>
public class PDStatusRollUpNodeUpdateTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDStatusRollUp.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDStatusRollUpNodeUpdateTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>
	/// Verifies that a node whose status changes after first render is pushed to the pop-over.
	/// </summary>
	[Fact]
	public void Changing_the_node_pushes_it_to_the_popup()
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, new PDStatusRollUpNode
			{
				Status = RollUpStatus.Green,
				Title = "Test",
				Summary = "All available checks are healthy"
			}));

		component.Render(parameters => parameters
			.Add(p => p.Node, new PDStatusRollUpNode
			{
				Status = RollUpStatus.Red,
				Title = "Test",
				Summary = "API connection failed"
			}));

		var updates = module.Invocations["update"];
		updates.Should().ContainSingle();

		var json = updates[0].Arguments[1].Should().BeOfType<string>().Subject;
		json.Should().Contain("red").And.Contain("API connection failed");
	}

	/// <summary>
	/// Verifies that an unchanged node does not chatter across the interop boundary. Rows re-render for
	/// reasons that have nothing to do with their status, and every consumer rebuilds its node each time.
	/// </summary>
	[Fact]
	public void An_unchanged_node_is_not_pushed_again()
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, new PDStatusRollUpNode
			{
				Status = RollUpStatus.Green,
				Title = "Test",
				Summary = "All available checks are healthy"
			}));

		// A freshly built but identical node - exactly what a consumer that rebuilds its node per render supplies.
		component.Render(parameters => parameters
			.Add(p => p.Node, new PDStatusRollUpNode
			{
				Status = RollUpStatus.Green,
				Title = "Test",
				Summary = "All available checks are healthy"
			}));

		module.Invocations["update"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a component first rendered without a node still initialises the pop-over once one
	/// arrives, rather than being left with no pop-over for the life of the component.
	/// </summary>
	[Fact]
	public void A_node_arriving_after_first_render_initialises_the_popup()
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDStatusRollUp>();

		module.Invocations["init"].Should().BeEmpty();

		component.Render(parameters => parameters
			.Add(p => p.Node, new PDStatusRollUpNode
			{
				Status = RollUpStatus.Amber,
				Title = "Test",
				Summary = "At least one check is degraded"
			}));

		module.Invocations["init"].Should().ContainSingle();
	}
}
