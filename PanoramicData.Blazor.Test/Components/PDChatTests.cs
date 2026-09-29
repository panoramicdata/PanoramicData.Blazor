using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
	// Window, header and docking
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

	/// <summary>Verifies that each dock mode maps to its container class.</summary>
	[Theory]
	[InlineData(PDChatDockMode.TopRight, "dock-top-right")]
	[InlineData(PDChatDockMode.BottomLeft, "dock-bottom-left")]
	[InlineData(PDChatDockMode.TopLeft, "dock-top-left")]
	[InlineData(PDChatDockMode.FullScreen, "dock-fullscreen")]
	[InlineData(PDChatDockMode.Left, "dock-left")]
	[InlineData(PDChatDockMode.Right, "dock-right")]
	public void Each_dock_mode_maps_to_a_class(PDChatDockMode mode, string expected)
	{
		var component = RenderChat(new FakeChatService { DockMode = mode, IsCanvasUsePermitted = false });

		component.Find(".pdchat-container").ClassList.Should().Contain(expected);
	}

	/// <summary>Verifies that each minimised button position maps to its container class.</summary>
	[Theory]
	[InlineData(PDChatButtonPosition.BottomRight, "dock-bottom-right")]
	[InlineData(PDChatButtonPosition.TopRight, "dock-top-right")]
	[InlineData(PDChatButtonPosition.BottomLeft, "dock-bottom-left")]
	[InlineData(PDChatButtonPosition.TopLeft, "dock-top-left")]
	[InlineData(PDChatButtonPosition.None, "dock-none")]
	public void Each_minimised_button_position_maps_to_a_class(PDChatButtonPosition position, string expected)
	{
		var component = RenderChat(Minimised(position));

		var container = component.Find(".pdchat-container");
		container.ClassList.Should().Contain(expected).And.Contain("dock-minimized").And.NotContain("open");
	}

	/// <summary>Verifies that a hidden minimised button renders no button at all.</summary>
	[Fact]
	public void A_hidden_minimised_button_is_not_rendered()
	{
		var component = RenderChat(Minimised(PDChatButtonPosition.None));

		component.FindAll(".pdchat-toggle-collapsed").Should().BeEmpty();
	}

	/// <summary>Verifies that the minimised button shows the collapsed icon and the message count.</summary>
	[Fact]
	public void The_minimised_button_shows_its_icon_and_count()
	{
		var service = Minimised();
		service.Store.Add(Message("One"));

		var component = RenderChat(service, p => p.Add(x => x.CollapsedIcon, "C"));

		var button = component.Find(".pdchat-toggle-collapsed");
		button.TextContent.Trim().Should().Be("C");
		button.GetAttribute("title").Should().Be("Open Chat (1 message)");
	}

	/// <summary>Verifies that minimising and restoring move the service between modes and raise the events.</summary>
	[Fact]
	public async Task Minimising_and_restoring_raise_their_events()
	{
		var events = new List<string>();
		var service = new FakeChatService { DockMode = PDChatDockMode.TopLeft, RestoreMode = PDChatDockMode.TopLeft };
		var component = RenderChat(service, p => p
			.Add(x => x.OnChatMinimized, () => events.Add("minimised"))
			.Add(x => x.OnChatRestored, () => events.Add("restored")));

		await component.Find(".pdchat-close").ClickAsync(new MouseEventArgs());
		service.DockMode.Should().Be(PDChatDockMode.Minimized);
		await component.Find(".pdchat-toggle-collapsed").ClickAsync(new MouseEventArgs());

		service.DockMode.Should().Be(PDChatDockMode.TopLeft);
		events.Should().Equal("minimised", "restored");
	}

	/// <summary>Verifies that full screen and back raise the maximised and restored events.</summary>
	[Fact]
	public async Task Full_screen_and_back_raise_their_events()
	{
		var events = new List<string>();
		var service = new FakeChatService { IsCanvasUsePermitted = false };
		var component = RenderChat(service, p => p
			.Add(x => x.OnChatMaximized, () => events.Add("maximised"))
			.Add(x => x.OnChatRestored, () => events.Add("restored")));

		await HeaderButton(component, "Fullscreen").ClickAsync(new MouseEventArgs());
		service.DockMode.Should().Be(PDChatDockMode.FullScreen);
		component.Find(".pdchat-window").ClassList.Should().Contain("fullscreen");
		await HeaderButton(component, "Restore").ClickAsync(new MouseEventArgs());

		service.DockMode.Should().Be(PDChatDockMode.BottomRight);
		events.Should().Equal("maximised", "restored");
	}

	/// <summary>Verifies that the maximise button is absent when maximising is not permitted.</summary>
	[Fact]
	public void Maximise_is_absent_when_not_permitted()
	{
		var component = RenderChat(new FakeChatService { IsMaximizePermitted = false });

		component.FindAll(".pdchat-header-btn[title=Fullscreen]").Should().BeEmpty();
	}

	/// <summary>Verifies that docking to a side goes to the side of the corner the chat is in.</summary>
	[Theory]
	[InlineData(PDChatDockMode.TopRight, PDChatDockMode.Right)]
	[InlineData(PDChatDockMode.BottomRight, PDChatDockMode.Right)]
	[InlineData(PDChatDockMode.TopLeft, PDChatDockMode.Left)]
	[InlineData(PDChatDockMode.BottomLeft, PDChatDockMode.Left)]
	public async Task Docking_to_a_side_follows_the_corner(PDChatDockMode corner, PDChatDockMode expected)
	{
		var service = new FakeChatService { DockMode = corner };
		var component = RenderChat(service);

		await HeaderButton(component, "Dock to Side").ClickAsync(new MouseEventArgs());

		service.DockMode.Should().Be(expected);
		component.FindAll(".pdchat-header-btn[title='Dock to Side']").Should().BeEmpty();
	}

	/// <summary>Verifies that unpinning returns the chat to the corner it was docked from.</summary>
	[Fact]
	public async Task Unpinning_returns_to_the_previous_corner()
	{
		var restored = 0;
		var service = new FakeChatService { DockMode = PDChatDockMode.TopLeft };
		var component = RenderChat(service, p => p.Add(x => x.OnChatRestored, () => restored++));

		await HeaderButton(component, "Dock to Side").ClickAsync(new MouseEventArgs());
		await HeaderButton(component, "Unpin from Side").ClickAsync(new MouseEventArgs());

		service.DockMode.Should().Be(PDChatDockMode.TopLeft);
		restored.Should().Be(1);
	}

	/// <summary>
	/// Verifies that unpinning a chat that opened straight into a side falls back to the corner its minimised
	/// button lives in, rather than staying put.
	/// </summary>
	[Theory]
	[InlineData(PDChatButtonPosition.TopLeft, PDChatDockMode.TopLeft)]
	[InlineData(PDChatButtonPosition.TopRight, PDChatDockMode.TopRight)]
	[InlineData(PDChatButtonPosition.BottomLeft, PDChatDockMode.BottomLeft)]
	[InlineData(PDChatButtonPosition.BottomRight, PDChatDockMode.BottomRight)]
	public async Task Unpinning_without_a_previous_corner_uses_the_button_corner(PDChatButtonPosition button, PDChatDockMode expected)
	{
		var service = new FakeChatService { DockMode = PDChatDockMode.Right, MinimizedButtonPosition = button };
		var component = RenderChat(service);

		await HeaderButton(component, "Unpin from Side").ClickAsync(new MouseEventArgs());

		service.DockMode.Should().Be(expected);
	}

	/// <summary>Verifies that a dock mode change announced by the service is followed.</summary>
	[Fact]
	public async Task A_dock_mode_change_from_the_service_is_followed()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.AnnounceDockMode(PDChatDockMode.Minimized));

		component.WaitForAssertion(() => component.Find(".pdchat-toggle-collapsed").Should().NotBeNull(), Patience);
	}

	/// <summary>Verifies that a chat inside a split container hands dock changes to the container.</summary>
	[Fact]
	public async Task A_chat_in_a_container_hands_dock_changes_to_it()
	{
		var service = new FakeChatService { DockMode = PDChatDockMode.Right };

		var dockChanges = new List<PDChatDockMode>();
		var container = Render<PDChatContainer>(parameters => parameters
			.Add(p => p.ChatService, service)
			.Add(p => p.InitialDockMode, PDChatDockMode.Right)
			.Add(p => p.DockModeChanged, (PDChatDockMode mode) => dockChanges.Add(mode))
			.Add(p => p.ChatContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<PDChat>(0);
				builder.AddComponentParameter(1, nameof(PDChat.ChatService), service);
				builder.AddComponentParameter(2, nameof(PDChat.User), _user);
				builder.CloseComponent();
			})));

		container.Find(".pdchat-container").ClassList.Should().Contain("dock-split-panel");
		await HeaderButton(container, "Unpin from Side").ClickAsync(new MouseEventArgs());

		service.DockMode.Should().Be(PDChatDockMode.BottomRight);
		dockChanges.Should().Equal(PDChatDockMode.BottomRight);
	}

	// ------------------------------------------------------------------------------------------
	// Mute, clear, live status and configuration
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that the mute button toggles the service's mute state and raises the event.</summary>
	[Fact]
	public async Task Mute_toggles_the_service_and_raises_the_event()
	{
		var toggles = 0;
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(x => x.OnMuteToggled, () => toggles++));

		await HeaderButton(component, "Mute").ClickAsync(new MouseEventArgs());

		service.IsMuted.Should().BeTrue();
		HeaderButton(component, "Unmute").TextContent.Should().Contain("🔇");
		toggles.Should().Be(1);
	}

	/// <summary>Verifies that a mute change announced by the service is reflected.</summary>
	[Fact]
	public async Task A_mute_change_from_the_service_is_reflected()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.AnnounceMute(true));

		component.WaitForAssertion(() => HeaderButton(component, "Unmute").Should().NotBeNull(), Patience);
	}

	/// <summary>Verifies that live-status and configuration announcements re-render the chat.</summary>
	[Fact]
	public async Task Live_and_configuration_announcements_re_render()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.AnnounceLive(false));
		component.WaitForAssertion(() => component.Find(".pdchat-title").TextContent.Should().EndWith("(Offline)"), Patience);

		service.Title = "Renamed";
		await component.InvokeAsync(service.AnnounceConfiguration);
		component.WaitForAssertion(() => component.Find(".pdchat-title").TextContent.Should().StartWith("Renamed"), Patience);
	}

	/// <summary>Verifies that Clear empties the transcript and the service and raises the event.</summary>
	[Fact]
	public async Task Clear_empties_the_transcript_and_the_service()
	{
		var cleared = 0;
		var service = new FakeChatService();
		service.Store.Add(Message("Old"));
		var component = RenderChat(service, p => p.Add(x => x.OnChatCleared, () => cleared++));

		await HeaderButton(component, "Clear Chat").ClickAsync(new MouseEventArgs());

		component.FindAll(".pdchat-message").Should().BeEmpty();
		service.ClearCount.Should().Be(1);
		cleared.Should().Be(1);
		component.FindAll(".pdchat-header-btn[title='Clear Chat']").Should().BeEmpty();
	}

	/// <summary>Verifies that Clear is not offered when not permitted.</summary>
	[Fact]
	public void Clear_is_absent_when_not_permitted()
	{
		var service = new FakeChatService { IsClearPermitted = false };
		service.Store.Add(Message("Old"));

		var component = RenderChat(service);

		component.FindAll(".pdchat-header-btn[title='Clear Chat']").Should().BeEmpty();
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

	private (IRenderedComponent<PDChat> Component, FakeConversationStore Store, FakeChatService Service) RenderConversations(bool storeFails = false)
	{
		var store = new FakeConversationStore(failTranscripts: storeFails);
		var service = new FakeChatService
		{
			DockMode = PDChatDockMode.FullScreen,
			SupportsConversations = true,
			ActiveConversationId = store.First.Id
		};
		service.ConversationStore[store.First.Id] = [Message("From the service")];
		var component = RenderChat(service, p => p.Add(x => x.ConversationService, store));
		return (component, store, service);
	}

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

	private static IElement ToolbarButton(IRenderedComponent<PDChat> component, string text)
		=> component.FindAll(".pdchat-conversation-toolbar-btn").Single(b => b.TextContent.Trim() == text);

	private static IElement SidebarRow(IRenderedComponent<PDChat> component, string title)
		=> component.FindAll(".pdchat-conversation-row").First(r => r.TextContent.Contains(title, StringComparison.Ordinal));

	private static IElement Tab(IRenderedComponent<PDChat> component, string title)
		=> component.FindAll(".pdchat-conversation-tabset button.pdtabset-tab")
			.Single(t => t.QuerySelector(".pdtabset-tab-title")?.TextContent == title);

	private static List<string> TabTitles(IRenderedComponent<PDChat> component)
		=> [.. component.FindAll(".pdchat-conversation-tabset .pdtabset-tab-title").Select(t => t.TextContent)];

	private static string ActiveTabTitle(IRenderedComponent<PDChat> component)
		=> component.Find(".pdchat-conversation-tabset .pdtabset-tab.active .pdtabset-tab-title").TextContent;

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
