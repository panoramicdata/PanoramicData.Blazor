using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDChat"/> follows its chat service: docking, minimising, muting, clearing, sending,
/// receiving, the minimised badge, toasts, the canvas layout and the conversation tabs.
/// </summary>
public partial class PDChatTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that another thread or a timer brings about. Generous because a busy
	/// machine (the whole suite under coverage) can hold the renderer's dispatcher well past bUnit's default.
	/// </summary>
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	private const string ModulePath = "./_content/PanoramicData.Blazor/PDChat.razor.js";

	private static readonly ChatMessageSender _user = new() { Name = "Tester", IsUser = true, IsHuman = true };
	private static readonly ChatMessageSender _bot = new() { Name = "Merlin" };

	/// <summary>Sets up the rendering context.</summary>
	public PDChatTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	// ------------------------------------------------------------------------------------------
	// Window and header (docking is in PDChatTests.Docking.cs)
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that an open chat shows its title, the service's messages, and is initialised once.</summary>
	[Fact]
	public void An_open_chat_shows_its_title_and_messages()
	{
		var service = new FakeChatService();
		service.Store.Add(Message("Hello there"));

		var component = RenderChat(service);

		component.Find(".pdchat-title").TextContent.Should().Be("Test Chat");
		component.Find(".pdchat-text").TextContent.Should().Contain("Hello there");
		component.Find(".pdchat-container").ClassList.Should().Contain("open").And.Contain("dock-bottom-right");
		service.InitializeCount.Should().Be(1);
	}

	/// <summary>Verifies that an offline service is marked as such in the title.</summary>
	[Fact]
	public void An_offline_chat_says_so()
	{
		var component = RenderChat(new FakeChatService { IsLive = false });

		component.Find(".pdchat-title").TextContent.Should().Be("Test Chat (Offline)");
	}

	// ------------------------------------------------------------------------------------------
	// Helpers
	// ------------------------------------------------------------------------------------------

	private IRenderedComponent<PDChat> RenderChat(FakeChatService service, Action<ComponentParameterCollectionBuilder<PDChat>>? configure = null)
		=> Render<PDChat>(parameters =>
		{
			parameters.Add(p => p.ChatService, service).Add(p => p.User, _user);
			configure?.Invoke(parameters);
		});

	private static ChatFormContext GetCascadedFormContext(IRenderedComponent<PDChat> component)
		=> component.FindComponent<CascadingValue<ChatFormContext>>().Instance.Value!;

	private static FakeChatService Minimised(PDChatButtonPosition position = PDChatButtonPosition.BottomRight)
		=> new() { DockMode = PDChatDockMode.Minimized, MinimizedButtonPosition = position };

	private static FakeChatService Toasting(PDChatButtonPosition position = PDChatButtonPosition.BottomRight)
	{
		var service = Minimised(position);
		service.ToastEnabled = true;
		service.ToastAutoDismiss = false;
		service.ToastAnimationDurationMs = 1;
		return service;
	}

	private static IElement HeaderButton<TComponent>(IRenderedComponent<TComponent> component, string title)
		where TComponent : IComponent
		=> component.Find($".pdchat-header-btn[title='{title}']");

	private static IElement InputToolbarButton<TComponent>(IRenderedComponent<TComponent> component, string title)
		where TComponent : IComponent
		=> component.Find($".chat-input-accessories .pdchat-toolbar-btn[title='{title}']");

	private static ChatMessage Message(string text, MessageType type = MessageType.Normal) => new()
	{
		Id = Guid.NewGuid(),
		Sender = _bot,
		Message = text,
		Type = type,
		Timestamp = DateTimeOffset.UtcNow.AddMinutes(1)
	};

	private static ChatMessage Update(ChatMessage original, string text) => new()
	{
		Id = original.Id,
		Sender = original.Sender,
		Message = text,
		Type = original.Type,
		Title = "Updated",
		PartialMessage = "partial"
	};

	private static ChatFormSubmission Submission(params (string Question, string? Value, bool Skipped)[] answers) => new()
	{
		FormId = Guid.NewGuid(),
		Answers = [.. answers.Select((a, i) => new ChatFormAnswer
		{
			QuestionId = $"q{i}",
			Question = a.Question,
			Value = a.Value,
			WasSkipped = a.Skipped
		})]
	};
}
