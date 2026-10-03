using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDStatusRollUp"/>: the trigger icon it renders and the drill-down it serves to the pop-over.
/// </summary>
/// <remarks>
/// Keeping the pop-over in step with a changing node is covered by <c>PDStatusRollUpNodeUpdateTests</c>.
/// </remarks>
public class PDStatusRollUpTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDStatusRollUp.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDStatusRollUpTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private static PDStatusRollUpNode Tree() => new()
	{
		Status = RollUpStatus.Amber,
		Title = "Root",
		Summary = "Some checks are degraded",
		Children =
		[
			new() { Status = RollUpStatus.Green, Title = "Child 0" },
			new()
			{
				Status = RollUpStatus.Amber,
				Title = "Child 1",
				Children = [new() { Status = RollUpStatus.Red, Title = "Grandchild 1.0" }]
			}
		]
	};

	/// <summary>
	/// Verifies that the trigger icon and its colour follow the node's status.
	/// </summary>
	[Theory]
	[InlineData(RollUpStatus.Red, "fa-times-circle", "text-danger")]
	[InlineData(RollUpStatus.Amber, "fa-exclamation-triangle", "pdsr-icon-amber")]
	[InlineData(RollUpStatus.Green, "fa-check-circle", "text-success")]
	[InlineData(RollUpStatus.Gray, "fa-question-circle", "text-secondary")]
	public void Icon_FollowsTheStatus(RollUpStatus status, string icon, string colour)
	{
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, new PDStatusRollUpNode { Status = status, Title = "Check" }));

		component.Find(".pdsr-trigger i").ClassList.Should().Contain(icon).And.Contain(colour);
	}

	/// <summary>
	/// Verifies that custom icon classes replace the defaults for their status.
	/// </summary>
	[Fact]
	public void CustomIconClasses_ReplaceTheDefaults()
	{
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.RedIconClass, "my-red")
			.Add(p => p.Node, new PDStatusRollUpNode { Status = RollUpStatus.Red }));

		component.Find(".pdsr-trigger i").ClassList.Should().Contain("my-red").And.NotContain("fa-times-circle");
	}

	/// <summary>
	/// Verifies that with no node the trigger is gray, labelled generically, and no pop-over is initialised.
	/// </summary>
	[Fact]
	public void WithoutANode_TheTriggerIsGrayAndGeneric()
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDStatusRollUp>();

		var trigger = component.Find(".pdsr-trigger");
		trigger.GetAttribute("aria-label").Should().Be("Status");
		trigger.GetAttribute("title").Should().Be("Click to view status");
		component.Find(".pdsr-trigger i").ClassList.Should().Contain("fa-question-circle").And.Contain("text-secondary");
		component.Find(".pdsr-trigger i").HasAttribute("style").Should().BeFalse();
		component.FindAll(".pdsr-label").Should().BeEmpty();
		module.Invocations["init"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the label, icon size and tooltip parameters are rendered, and the label names the trigger when there is no node.
	/// </summary>
	[Fact]
	public void LabelSizeAndTooltip_AreRendered()
	{
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Label, "Database")
			.Add(p => p.TriggerIconSize, "2rem")
			.Add(p => p.TriggerTitle, "Show database health"));

		var trigger = component.Find(".pdsr-trigger");
		trigger.GetAttribute("aria-label").Should().Be("Database");
		trigger.GetAttribute("title").Should().Be("Show database health");
		component.Find(".pdsr-label").TextContent.Should().Be("Database");
		component.Find(".pdsr-trigger i").GetAttribute("style").Should().Be("font-size: 2rem");
	}

	/// <summary>
	/// Verifies that the node's title names the trigger in preference to the label.
	/// </summary>
	[Fact]
	public void NodeTitle_NamesTheTrigger()
	{
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Label, "Database")
			.Add(p => p.Node, new PDStatusRollUpNode { Title = "Primary database" }));

		component.Find(".pdsr-trigger").GetAttribute("aria-label").Should().Be("Primary database");
	}

	/// <summary>
	/// Verifies that the pop-over is initialised with the trigger, the node as camel-cased JSON and the icon map,
	/// and without a callback reference when there is no expansion callback.
	/// </summary>
	[Fact]
	public void Init_PassesTheTriggerNodeAndIcons()
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDStatusRollUp>(parameters => parameters.Add(p => p.Node, Tree()));

		var init = module.VerifyInvoke("init");
		init.Arguments[0].Should().Be(component.Find(".pdsr-trigger").Id);
		init.Arguments[1].Should().BeOfType<string>().Which.Should().Contain("\"status\":\"amber\"").And.Contain("\"title\":\"Root\"");
		init.Arguments[2].Should().NotBeNull();
		init.Arguments[2]!.ToString().Should().Contain("fas fa-times-circle");
		init.Arguments[3].Should().BeNull();
	}

	/// <summary>
	/// Verifies that an expansion callback is wired up by passing a reference back to the component.
	/// </summary>
	[Fact]
	public void Init_WithAnExpansionCallback_PassesAReference()
	{
		var module = JSInterop.SetupModule(ModulePath);

		Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, Tree())
			.Add(p => p.OnBeforeExpand, node => Task.FromResult<PDStatusRollUpNode?>(node)));

		module.VerifyInvoke("init").Arguments[3].Should().BeOfType<DotNetObjectReference<PDStatusRollUp>>();
	}

	/// <summary>
	/// Verifies that a module that fails to load leaves the trigger usable rather than failing the render.
	/// </summary>
	[Fact]
	public void ModuleFailure_LeavesTheTriggerRendered()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;

		var component = Render<PDStatusRollUp>(parameters => parameters.Add(p => p.Node, Tree()));

		component.FindAll(".pdsr-trigger").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that disposing the component disposes its pop-over.
	/// </summary>
	[Fact]
	public async Task Dispose_DisposesThePopup()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var component = Render<PDStatusRollUp>(parameters => parameters.Add(p => p.Node, Tree()));
		var triggerId = component.Find(".pdsr-trigger").Id;

		await DisposeComponentsAsync();

		module.VerifyInvoke("dispose").Arguments[0].Should().Be(triggerId);
	}

	/// <summary>
	/// Verifies that without an expansion callback there is never anything to update.
	/// </summary>
	[Fact]
	public async Task ExpandNode_WithoutACallback_ReturnsNull()
	{
		var component = Render<PDStatusRollUp>(parameters => parameters.Add(p => p.Node, Tree()));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().BeNull();
	}

	/// <summary>
	/// Verifies that with no node there is nothing to expand, even with a callback.
	/// </summary>
	[Fact]
	public async Task ExpandNode_WithoutANode_ReturnsNull()
	{
		var calls = 0;
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.OnBeforeExpand, node => { calls++; return Task.FromResult<PDStatusRollUpNode?>(node); }));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().BeNull();
		calls.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a path that does not lead to a node is ignored without calling the callback.
	/// </summary>
	[Theory]
	[InlineData("5")]
	[InlineData("-1")]
	[InlineData("x")]
	[InlineData("1.3")]
	[InlineData("0.0")]
	public async Task ExpandNode_ForAPathThatLeadsNowhere_ReturnsNull(string path)
	{
		var calls = 0;
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, Tree())
			.Add(p => p.OnBeforeExpand, node => { calls++; return Task.FromResult<PDStatusRollUpNode?>(node); }));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(path));

		result.Should().BeNull();
		calls.Should().Be(0);
	}

	/// <summary>
	/// Verifies that a callback declining to update leaves the node unchanged and returns nothing.
	/// </summary>
	[Fact]
	public async Task ExpandNode_WhenTheCallbackDeclines_LeavesTheNode()
	{
		var tree = Tree();
		PDStatusRollUpNode? received = null;
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, tree)
			.Add(p => p.OnBeforeExpand, node => { received = node; return Task.FromResult<PDStatusRollUpNode?>(null); }));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("1"));

		result.Should().BeNull();
		received.Should().BeSameAs(tree.Children[1]);
		tree.Children[1].Title.Should().Be("Child 1");
	}

	/// <summary>
	/// Verifies that updating the root copies the update onto the node, re-renders the trigger and returns the update as JSON.
	/// </summary>
	[Fact]
	public async Task ExpandNode_ForTheRoot_UpdatesTheTrigger()
	{
		var tree = Tree();
		var update = new PDStatusRollUpNode { Status = RollUpStatus.Red, Title = "Root", Summary = "Down", Detail = "Timed out" };
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, tree)
			.Add(p => p.OnBeforeExpand, _ => Task.FromResult<PDStatusRollUpNode?>(update)));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().Contain("\"status\":\"red\"").And.Contain("\"detail\":\"Timed out\"");
		tree.Status.Should().Be(RollUpStatus.Red);
		tree.Summary.Should().Be("Down");
		tree.Detail.Should().Be("Timed out");
		tree.Children.Should().BeEmpty();
		component.Find(".pdsr-trigger i").ClassList.Should().Contain("text-danger");
	}

	/// <summary>
	/// Verifies that updating a descendant replaces it in the tree, so a later expansion sees the update.
	/// </summary>
	[Fact]
	public async Task ExpandNode_ForADescendant_ReplacesItInTheTree()
	{
		var tree = Tree();
		var update = new PDStatusRollUpNode { Status = RollUpStatus.Green, Title = "Recovered" };
		var received = new List<PDStatusRollUpNode>();
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, tree)
			.Add(p => p.OnBeforeExpand, node => { received.Add(node); return Task.FromResult<PDStatusRollUpNode?>(update); }));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("1.0"));
		await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("1.0"));

		result.Should().Contain("\"title\":\"Recovered\"");
		tree.Children[1].Children[0].Should().BeSameAs(update);
		received[0].Title.Should().Be("Grandchild 1.0");
		received[1].Should().BeSameAs(update);
		tree.Status.Should().Be(RollUpStatus.Amber);
	}

	/// <summary>
	/// Verifies that an update is not patched in when the callback itself removed the node it was asked about,
	/// rather than being written to a position that now holds something else.
	/// </summary>
	[Theory]
	[InlineData("0")]
	[InlineData("1.0")]
	public async Task ExpandNode_ForANodeThatVanished_DoesNotPatchTheTree(string path)
	{
		var tree = Tree();
		var component = Render<PDStatusRollUp>(parameters => parameters
			.Add(p => p.Node, tree)
			.Add(p => p.OnBeforeExpand, _ =>
			{
				tree.Children.Clear();
				return Task.FromResult<PDStatusRollUpNode?>(new PDStatusRollUpNode { Title = "Late" });
			}));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(path));

		result.Should().Contain("\"title\":\"Late\"");
		tree.Children.Should().BeEmpty();
	}
}
