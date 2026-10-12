using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The agent picker's optional action: the last entry, separated from the agents, which runs a callback and never
/// becomes the selected agent.
/// </summary>
public partial class PDChatTests
{
	private static PDChatPickerAction CountingAction(Action onChosen)
		=> new("New Assistant", () =>
		{
			onChosen();
			return Task.CompletedTask;
		}, "[+]", "Make a new assistant");

	/// <summary>Verifies that without an action the picker holds exactly the agents, as it did before actions existed.</summary>
	[Fact]
	public void Without_an_action_the_picker_holds_only_the_agents()
	{
		var component = RenderChat(new FakeChatService { Agents = [_merlin, _alice] });

		var select = component.Find(".pdchat-agent-picker select");
		select.QuerySelectorAll("option").Select(option => option.TextContent).Should().Equal("Merlin", "Alice");
		select.QuerySelectorAll("option[disabled]").Should().BeEmpty();
		select.QuerySelectorAll(".pdchat-picker-action, .pdchat-picker-separator").Should().BeEmpty();
	}

	/// <summary>Verifies that the action is the last option, after a disabled separator, showing its icon then its label.</summary>
	[Fact]
	public void The_action_is_the_last_option_after_a_separator()
	{
		var component = RenderChat(new FakeChatService { Agents = [_merlin, _alice], AgentPickerAction = CountingAction(() => { }) });

		var options = component.Find(".pdchat-agent-picker select").QuerySelectorAll("option");
		options.Should().HaveCount(4);
		options[0].TextContent.Should().Be("Merlin");
		options[1].TextContent.Should().Be("Alice");
		options[2].ClassList.Should().Contain("pdchat-picker-separator");
		options[2].HasAttribute("disabled").Should().BeTrue();
		options[3].ClassList.Should().Contain("pdchat-picker-action");
		options[3].TextContent.Should().Be("[+] New Assistant");
		options[3].GetAttribute("title").Should().Be("Make a new assistant");
		options[3].HasAttribute("selected").Should().BeFalse();
	}

	/// <summary>Verifies that an action without an icon shows its label alone.</summary>
	[Fact]
	public void An_action_without_an_icon_shows_its_label()
	{
		var action = new PDChatPickerAction("New Assistant", () => Task.CompletedTask);

		var component = RenderChat(new FakeChatService { Agents = [_merlin, _alice], AgentPickerAction = action });

		component.Find(".pdchat-picker-action").TextContent.Should().Be("New Assistant");
	}

	/// <summary>Verifies that choosing the action runs it, leaves the selected agent alone and snaps the picker back.</summary>
	[Fact]
	public async Task Choosing_the_action_runs_it_and_keeps_the_selected_agent()
	{
		var chosen = 0;
		var service = new FakeChatService { Agents = [_merlin, _alice], AgentPickerAction = CountingAction(() => chosen++) };
		((IChatService)service).SelectedAgentId = "alice";
		var component = RenderChat(service);
		var actionValue = component.Find(".pdchat-picker-action").GetAttribute("value");

		await component.Find(".pdchat-agent-picker select").ChangeAsync(new ChangeEventArgs { Value = actionValue });

		chosen.Should().Be(1);
		((IChatService)service).SelectedAgentId.Should().Be("alice");
		component.WaitForAssertion(() =>
		{
			component.Find("option[value='alice']").HasAttribute("selected").Should().BeTrue();
			component.Find(".pdchat-picker-action").HasAttribute("selected").Should().BeFalse();
		}, Patience);
	}

	/// <summary>Verifies that a single agent is offered with an action, so the action is reachable, but not without one.</summary>
	[Fact]
	public void One_agent_with_an_action_shows_the_picker()
	{
		RenderChat(new FakeChatService { Agents = [_merlin], AgentPickerAction = CountingAction(() => { }) })
			.FindAll(".pdchat-agent-picker").Should().ContainSingle();
		RenderChat(new FakeChatService { Agents = [_merlin] })
			.FindAll(".pdchat-agent-picker").Should().BeEmpty();
	}

	/// <summary>Verifies that an action alone, with no agents, offers no picker.</summary>
	[Fact]
	public void An_action_without_agents_shows_no_picker()
		=> RenderChat(new FakeChatService { AgentPickerAction = CountingAction(() => { }) })
			.FindAll(".pdchat-agent-picker").Should().BeEmpty();
}
