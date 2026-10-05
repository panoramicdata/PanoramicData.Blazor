using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The model picker: offered in the input toolbar only when the service lists two or more models, and choosing one sets
/// <see cref="IChatService.SelectedModelId"/>.
/// </summary>
public partial class PDChatTests
{
	private static readonly PDChatModelOption _quick = new("quick", "Quick", "Small and fast");
	private static readonly PDChatModelOption _thorough = new("thorough", "Thorough");

	/// <summary>Verifies that no model picker is shown with no models, or with only one to choose.</summary>
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	public void The_model_picker_is_hidden_with_fewer_than_two_models(int count)
		=> RenderChat(new FakeChatService { Models = [.. new[] { _quick, _thorough }.Take(count)] })
			.FindAll(".pdchat-model-picker").Should().BeEmpty();

	/// <summary>Verifies that two or more models are offered in the toolbar, labelled, the first selected by default.</summary>
	[Fact]
	public void The_model_picker_is_shown_in_the_toolbar_with_two_models()
	{
		var picker = RenderChat(new FakeChatService { Models = [_quick, _thorough] }).Find(".chat-input-accessories .pdchat-model-picker");

		picker.QuerySelector(".pdchat-toolbar-label")!.TextContent.Should().Be("Model");
		var options = picker.QuerySelectorAll("option");
		options.Select(option => option.TextContent).Should().Equal("Quick", "Thorough");
		options[0].GetAttribute("title").Should().Be("Small and fast");
		options[0].HasAttribute("selected").Should().BeTrue();
	}

	/// <summary>Verifies that choosing a model sets the service's selected model, and an unknown one is ignored.</summary>
	[Fact]
	public async Task Choosing_a_model_sets_the_selected_model()
	{
		var service = new FakeChatService { Models = [_quick, _thorough] };
		var component = RenderChat(service);

		await component.Find(".pdchat-model-picker select").ChangeAsync(new ChangeEventArgs { Value = "mallory" });
		((IChatService)service).SelectedModelId.Should().BeNull();

		await component.Find(".pdchat-model-picker select").ChangeAsync(new ChangeEventArgs { Value = "thorough" });
		((IChatService)service).SelectedModelId.Should().Be("thorough");
		component.Find(".pdchat-model-picker option[value=thorough]").HasAttribute("selected").Should().BeTrue();
	}

	/// <summary>Verifies that the agent and model pickers are separate, labelled choices in the same toolbar.</summary>
	[Fact]
	public void The_agent_and_model_pickers_are_both_labelled()
	{
		var toolbar = RenderChat(new FakeChatService { Agents = [_merlin, _alice], Models = [_quick, _thorough] })
			.Find(".chat-input-accessories");

		toolbar.QuerySelectorAll(".pdchat-toolbar-label").Select(label => label.TextContent).Should().Equal("Agent", "Model");
	}

	/// <summary>Verifies that the demo service offers models, agents and simulated voice.</summary>
	[Fact]
	public void The_demo_service_offers_the_whole_toolbar()
	{
		using var service = new PanoramicData.Blazor.Services.DumbChatService();
		IChatService chat = service;

		chat.Models.Should().HaveCountGreaterThan(1);
		chat.Agents.Should().HaveCountGreaterThan(1);
		chat.VoiceEndpoints.Should().Be(PDChatVoiceEndpoints.Simulated);
	}
}
