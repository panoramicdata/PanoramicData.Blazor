using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The agent picker: offered in the input area only when the service lists two or more agents, and choosing one sets
/// <see cref="IChatService.SelectedAgentId"/>.
/// </summary>
public partial class PDChatTests
{
	private static readonly PDChatAgentOption _merlin = new("merlin", "Merlin", "Answers about the product", "/merlin.png");
	private static readonly PDChatAgentOption _alice = new("alice", "Alice", "Development help");

	/// <summary>Verifies that no picker is shown with no agents, or with only one to choose.</summary>
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	public void The_agent_picker_is_hidden_with_fewer_than_two_agents(int count)
	{
		var component = RenderChat(new FakeChatService { Agents = [.. new[] { _merlin, _alice }.Take(count)] });

		component.FindAll(".pdchat-agent-picker").Should().BeEmpty();
		component.Find(".chat-input-accessories").QuerySelectorAll("select").Should().BeEmpty();
	}

	/// <summary>Verifies that no picker is shown when the service does not list agents at all.</summary>
	[Fact]
	public void The_agent_picker_is_hidden_without_agents()
		=> RenderChat(new FakeChatService()).FindAll(".pdchat-agent-picker").Should().BeEmpty();

	/// <summary>Verifies that two or more agents are offered in the input area, the first selected by default.</summary>
	[Fact]
	public void The_agent_picker_is_shown_in_the_input_area_with_two_agents()
	{
		var component = RenderChat(new FakeChatService { Agents = [_merlin, _alice] });

		var select = component.Find(".chat-input-accessories .pdchat-agent-picker select");
		select.GetAttribute("title").Should().Be("Who you are talking to");
		var options = select.QuerySelectorAll("option");
		options.Select(option => option.TextContent).Should().Equal("Merlin", "Alice");
		options.Select(option => option.GetAttribute("value")).Should().Equal("merlin", "alice");
		options[0].GetAttribute("title").Should().Be("Answers about the product");
		options[0].HasAttribute("selected").Should().BeTrue();
		component.Find(".pdchat-agent-icon").GetAttribute("src").Should().Be("/merlin.png");
	}

	/// <summary>Verifies that choosing an agent sets the service's selected agent, and the picker follows it.</summary>
	[Fact]
	public async Task Choosing_an_agent_sets_the_selected_agent()
	{
		var service = new FakeChatService { Agents = [_merlin, _alice] };
		var component = RenderChat(service);
		((IChatService)service).SelectedAgentId.Should().BeNull();

		await component.Find(".pdchat-agent-picker select").ChangeAsync(new ChangeEventArgs { Value = "alice" });

		((IChatService)service).SelectedAgentId.Should().Be("alice");
		component.WaitForAssertion(() => component.Find("option[value='alice']").HasAttribute("selected").Should().BeTrue(), Patience);
		component.FindAll(".pdchat-agent-icon").Should().BeEmpty("Alice has no icon");
	}

	/// <summary>Verifies that a value that is not one of the agents is ignored.</summary>
	[Fact]
	public async Task An_unknown_agent_is_not_selected()
	{
		var service = new FakeChatService { Agents = [_merlin, _alice] };
		var component = RenderChat(service);

		await component.Find(".pdchat-agent-picker select").ChangeAsync(new ChangeEventArgs { Value = "mallory" });

		((IChatService)service).SelectedAgentId.Should().BeNull();
	}

	/// <summary>Verifies that the picker and the Voice control share the input area.</summary>
	[Fact]
	public void The_agent_picker_sits_beside_the_Voice_control()
	{
		var service = VoiceService();
		service.Agents = [_merlin, _alice];

		var accessories = RenderChat(service).Find(".chat-input-accessories");

		accessories.QuerySelector(".pdchat-agent-picker").Should().NotBeNull();
		accessories.QuerySelector(".pdchat-voice-toggle").Should().NotBeNull();
	}
}
