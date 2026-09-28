using System.Text.Json;
using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDStatusCascade"/> renders its trigger from the node, hands the tree to its pop-over
/// module, and applies the updates its expand callback returns to the right node.
/// </summary>
public class PDStatusCascadeTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDStatusCascade.razor.js";

	/// <summary>Sets up the rendering context.</summary>
	public PDStatusCascadeTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Verifies that the trigger icon takes its icon and colour from the node's status and its label from its title.</summary>
	[Fact]
	public void The_trigger_reflects_the_node()
	{
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, new PDStatusCascadeNode { Status = StatusType.Red, Title = "Database" }));

		var icon = component.Find(".pdsc-trigger i");
		icon.ClassList.Should().Contain("fa-times-circle").And.Contain("text-danger");
		component.Find(".pdsc-trigger").GetAttribute("aria-label").Should().Be("Database");
		icon.HasAttribute("style").Should().BeFalse();
	}

	/// <summary>Verifies that a custom status supplies its own icon and colour.</summary>
	[Fact]
	public void A_custom_status_supplies_its_own_icon()
	{
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, new PDStatusCascadeNode { Status = StatusType.Custom("blue", "fas fa-info", "text-info") }));

		component.Find(".pdsc-trigger i").ClassList.Should().Contain("fa-info").And.Contain("text-info");
	}

	/// <summary>Verifies that with no node the trigger is gray, labelled by the Label, and the label is shown.</summary>
	[Fact]
	public void Without_a_node_the_trigger_is_gray_and_uses_the_label()
	{
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Label, "Overall")
			.Add(p => p.TriggerTitle, "Show")
			.Add(p => p.TriggerIconSize, "2rem"));

		var trigger = component.Find(".pdsc-trigger");
		trigger.GetAttribute("aria-label").Should().Be("Overall");
		trigger.GetAttribute("title").Should().Be("Show");
		component.Find(".pdsc-label").TextContent.Should().Be("Overall");
		var icon = component.Find(".pdsc-trigger i");
		icon.ClassList.Should().Contain("fa-question-circle").And.Contain("text-secondary");
		icon.GetAttribute("style").Should().Be("font-size: 2rem");
	}

	/// <summary>Verifies that with neither node nor label the trigger is labelled "Status" and nothing is imported.</summary>
	[Fact]
	public void Without_node_or_label_nothing_is_initialised()
	{
		var component = Render<PDStatusCascade>();

		component.Find(".pdsc-trigger").GetAttribute("aria-label").Should().Be("Status");
		component.FindAll(".pdsc-label").Should().BeEmpty();
		JSInterop.Invocations.Should().NotContain(i => i.Identifier == "import");
	}

	/// <summary>
	/// Verifies that the pop-over is initialised with the tree as camel-case JSON carrying each status's icon,
	/// and with no callback reference when there is no expand callback.
	/// </summary>
	[Fact]
	public void The_popup_is_initialised_with_the_tree()
	{
		var module = JSInterop.SetupModule(ModulePath);

		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, Tree()));

		var init = module.VerifyInvoke("init");
		init.Arguments[0].Should().Be(component.Find(".pdsc-trigger").Id);
		var json = init.Arguments[1].Should().BeOfType<string>().Subject;
		json.Should().Contain("\"title\":\"Root\"")
			.And.Contain("\"iconClass\":\"fas fa-check-circle\"")
			.And.Contain("\"children\":[");
		init.Arguments[2].Should().BeNull();
	}

	/// <summary>Verifies that an expand callback causes a callback reference to be handed to the pop-over.</summary>
	[Fact]
	public void An_expand_callback_hands_a_reference_to_the_popup()
	{
		var module = JSInterop.SetupModule(ModulePath);

		Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, Tree())
			.Add(p => p.OnBeforeExpand, node => Task.FromResult<PDStatusCascadeNode?>(null)));

		module.VerifyInvoke("init").Arguments[2].Should().BeOfType<DotNetObjectReference<PDStatusCascade>>();
	}

	/// <summary>Verifies that a pop-over that fails to initialise does not break the component.</summary>
	[Fact]
	public void A_failing_popup_initialisation_is_tolerated()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("init", _ => true).SetException(new JSException("gone"));

		var component = Render<PDStatusCascade>(parameters => parameters.Add(p => p.Node, Tree()));

		component.Find(".pdsc-trigger").Should().NotBeNull();
	}

	/// <summary>Verifies that expanding does nothing when there is no callback.</summary>
	[Fact]
	public async Task Expanding_without_a_callback_returns_nothing()
	{
		var component = Render<PDStatusCascade>(parameters => parameters.Add(p => p.Node, Tree()));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().BeNull();
	}

	/// <summary>Verifies that expanding with no node returns nothing and does not call the callback.</summary>
	[Fact]
	public async Task Expanding_without_a_node_returns_nothing()
	{
		var calls = 0;
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.OnBeforeExpand, node =>
			{
				calls++;
				return Task.FromResult<PDStatusCascadeNode?>(node);
			}));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		result.Should().BeNull();
		calls.Should().Be(0);
	}

	/// <summary>Verifies that an updated root replaces the root's content and re-renders the trigger.</summary>
	[Fact]
	public async Task An_updated_root_is_applied_and_rendered()
	{
		var root = Tree();
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, root)
			.Add(p => p.OnBeforeExpand, _ => Task.FromResult<PDStatusCascadeNode?>(
				new PDStatusCascadeNode { Status = StatusType.Red, Title = "Root now", Summary = "Down", Detail = "Detail" })));

		var json = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(""));

		json.Should().Contain("\"title\":\"Root now\"");
		root.Title.Should().Be("Root now");
		root.Summary.Should().Be("Down");
		root.Detail.Should().Be("Detail");
		root.Children.Should().BeEmpty();
		component.Find(".pdsc-trigger i").ClassList.Should().Contain("text-danger");
	}

	/// <summary>Verifies that the callback receives the node at the given path and a nested update is patched in place.</summary>
	[Fact]
	public async Task A_nested_update_is_patched_at_its_path()
	{
		var root = Tree();
		PDStatusCascadeNode? received = null;
		var replacement = new PDStatusCascadeNode { Title = "Leaf now", Status = StatusType.Amber };
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, root)
			.Add(p => p.OnBeforeExpand, node =>
			{
				received = node;
				return Task.FromResult<PDStatusCascadeNode?>(replacement);
			}));

		var json = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("1.0"));

		received!.Title.Should().Be("Leaf");
		root.Children[1].Children[0].Should().BeSameAs(replacement);
		json.Should().Contain("\"name\":\"amber\"");
	}

	/// <summary>Verifies that a path that does not name a node returns nothing and never calls the callback.</summary>
	[Theory]
	[InlineData("5")]
	[InlineData("-1")]
	[InlineData("x")]
	[InlineData("0.0")]
	public async Task An_invalid_path_returns_nothing(string path)
	{
		var calls = 0;
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, Tree())
			.Add(p => p.OnBeforeExpand, node =>
			{
				calls++;
				return Task.FromResult<PDStatusCascadeNode?>(node);
			}));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(path));

		result.Should().BeNull();
		calls.Should().Be(0);
	}

	/// <summary>Verifies that a callback returning null leaves the tree unchanged.</summary>
	[Fact]
	public async Task A_callback_returning_null_leaves_the_tree_unchanged()
	{
		var root = Tree();
		var original = root.Children[0];
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, root)
			.Add(p => p.OnBeforeExpand, _ => Task.FromResult<PDStatusCascadeNode?>(null)));

		var result = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync("0"));

		result.Should().BeNull();
		root.Children[0].Should().BeSameAs(original);
	}

	/// <summary>
	/// Verifies that an update is not patched in when the callback itself removed the node it was asked about,
	/// rather than being written to a position that now holds something else.
	/// </summary>
	[Theory]
	[InlineData("0")]
	[InlineData("1.0")]
	public async Task An_update_for_a_node_that_vanished_is_not_patched(string path)
	{
		var root = Tree();
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, root)
			.Add(p => p.OnBeforeExpand, _ =>
			{
				root.Children.Clear();
				return Task.FromResult<PDStatusCascadeNode?>(new PDStatusCascadeNode { Title = "Late" });
			}));

		var json = await component.InvokeAsync(() => component.Instance.ExpandNodeAsync(path));

		json.Should().Contain("Late");
		root.Children.Should().BeEmpty();
	}

	/// <summary>Verifies that disposing tells the pop-over to tear itself down.</summary>
	[Fact]
	public async Task Disposing_tears_down_the_popup()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var component = Render<PDStatusCascade>(parameters => parameters
			.Add(p => p.Node, Tree())
			.Add(p => p.OnBeforeExpand, node => Task.FromResult<PDStatusCascadeNode?>(node)));

		await component.Instance.DisposeAsync();

		module.VerifyInvoke("dispose").Arguments[0].Should().Be(component.Find(".pdsc-trigger").Id);
	}

	/// <summary>Verifies that a failing teardown is tolerated.</summary>
	[Fact]
	public async Task A_failing_teardown_is_tolerated()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("dispose", _ => true).SetException(new JSException("gone"));
		var component = Render<PDStatusCascade>(parameters => parameters.Add(p => p.Node, Tree()));

		var act = async () => await component.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
	}

	/// <summary>Verifies that the status converter reads back what it writes, as a custom status.</summary>
	[Fact]
	public void The_status_converter_round_trips()
	{
		var options = new JsonSerializerOptions { Converters = { new StatusTypeJsonConverter() } };

		var json = JsonSerializer.Serialize(StatusType.Amber, options);
		var read = JsonSerializer.Deserialize<StatusType>(json, options);

		json.Should().Be("{\"name\":\"amber\",\"iconClass\":\"fas fa-exclamation-triangle\",\"colorClass\":\"pdsc-icon-amber\"}");
		read!.Name.Should().Be("amber");
		read.DefaultIconClass.Should().Be("fas fa-exclamation-triangle");
		read.DefaultColorClass.Should().Be("pdsc-icon-amber");
	}

	/// <summary>Verifies that the converter falls back to gray's values when fields are null.</summary>
	[Fact]
	public void The_status_converter_falls_back_to_gray_for_nulls()
	{
		var options = new JsonSerializerOptions { Converters = { new StatusTypeJsonConverter() } };

		var read = JsonSerializer.Deserialize<StatusType>("{\"name\":null,\"iconClass\":null,\"colorClass\":null}", options);

		read!.Name.Should().Be("gray");
		read.DefaultIconClass.Should().Be(StatusType.Gray.DefaultIconClass);
		read.DefaultColorClass.Should().Be(StatusType.Gray.DefaultColorClass);
	}

	private static PDStatusCascadeNode Tree() => new()
	{
		Status = StatusType.Green,
		Title = "Root",
		Summary = "All good",
		Children =
		[
			new PDStatusCascadeNode { Title = "First" },
			new PDStatusCascadeNode
			{
				Title = "Second",
				Children = [new PDStatusCascadeNode { Title = "Leaf" }]
			}
		]
	};
}
