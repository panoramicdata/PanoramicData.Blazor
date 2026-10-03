using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;
using System.Text.Json;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDStatusCascade"/> renders its trigger from the node, hands the tree to its pop-over
/// module, and applies the updates its expand callback returns to the right node.
/// </summary>
public partial class PDStatusCascadeTests : BunitContext
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

	/// <summary>Verifies that a pop-over that fails to initialise does not break the component.</summary>
	[Fact]
	public void A_failing_popup_initialisation_is_tolerated()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("init", _ => true).SetException(new JSException("gone"));

		var component = Render<PDStatusCascade>(parameters => parameters.Add(p => p.Node, Tree()));

		component.Find(".pdsc-trigger").Should().NotBeNull();
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
