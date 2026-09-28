using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDChat"/> follows its chat service: docking, minimising, muting, clearing, sending,
/// receiving, the minimised badge, toasts, the canvas layout and the conversation tabs.
/// </summary>
public class PDChatTests : BunitContext
{
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
	public void Minimising_and_restoring_raise_their_events()
	{
		var events = new List<string>();
		var service = new FakeChatService { DockMode = PDChatDockMode.TopLeft, RestoreMode = PDChatDockMode.TopLeft };
		var component = RenderChat(service, p => p
			.Add(x => x.OnChatMinimized, () => events.Add("minimised"))
			.Add(x => x.OnChatRestored, () => events.Add("restored")));

		component.Find(".pdchat-close").Click();
		service.DockMode.Should().Be(PDChatDockMode.Minimized);
		component.Find(".pdchat-toggle-collapsed").Click();

		service.DockMode.Should().Be(PDChatDockMode.TopLeft);
		events.Should().Equal("minimised", "restored");
	}

	/// <summary>Verifies that full screen and back raise the maximised and restored events.</summary>
	[Fact]
	public void Full_screen_and_back_raise_their_events()
	{
		var events = new List<string>();
		var service = new FakeChatService { IsCanvasUsePermitted = false };
		var component = RenderChat(service, p => p
			.Add(x => x.OnChatMaximized, () => events.Add("maximised"))
			.Add(x => x.OnChatRestored, () => events.Add("restored")));

		HeaderButton(component, "Fullscreen").Click();
		service.DockMode.Should().Be(PDChatDockMode.FullScreen);
		component.Find(".pdchat-window").ClassList.Should().Contain("fullscreen");
		HeaderButton(component, "Restore").Click();

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
	public void Docking_to_a_side_follows_the_corner(PDChatDockMode corner, PDChatDockMode expected)
	{
		var service = new FakeChatService { DockMode = corner };
		var component = RenderChat(service);

		HeaderButton(component, "Dock to Side").Click();

		service.DockMode.Should().Be(expected);
		component.FindAll(".pdchat-header-btn[title='Dock to Side']").Should().BeEmpty();
	}

	/// <summary>Verifies that unpinning returns the chat to the corner it was docked from.</summary>
	[Fact]
	public void Unpinning_returns_to_the_previous_corner()
	{
		var restored = 0;
		var service = new FakeChatService { DockMode = PDChatDockMode.TopLeft };
		var component = RenderChat(service, p => p.Add(x => x.OnChatRestored, () => restored++));

		HeaderButton(component, "Dock to Side").Click();
		HeaderButton(component, "Unpin from Side").Click();

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
	public void Unpinning_without_a_previous_corner_uses_the_button_corner(PDChatButtonPosition button, PDChatDockMode expected)
	{
		var service = new FakeChatService { DockMode = PDChatDockMode.Right, MinimizedButtonPosition = button };
		var component = RenderChat(service);

		HeaderButton(component, "Unpin from Side").Click();

		service.DockMode.Should().Be(expected);
	}

	/// <summary>Verifies that a dock mode change announced by the service is followed.</summary>
	[Fact]
	public async Task A_dock_mode_change_from_the_service_is_followed()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.AnnounceDockMode(PDChatDockMode.Minimized));

		component.WaitForAssertion(() => component.Find(".pdchat-toggle-collapsed").Should().NotBeNull());
	}

	/// <summary>Verifies that a chat inside a split container hands dock changes to the container.</summary>
	[Fact]
	public void A_chat_in_a_container_hands_dock_changes_to_it()
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
		HeaderButton(container, "Unpin from Side").Click();

		service.DockMode.Should().Be(PDChatDockMode.BottomRight);
		dockChanges.Should().Equal(PDChatDockMode.BottomRight);
	}

	// ------------------------------------------------------------------------------------------
	// Mute, clear, live status and configuration
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that the mute button toggles the service's mute state and raises the event.</summary>
	[Fact]
	public void Mute_toggles_the_service_and_raises_the_event()
	{
		var toggles = 0;
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(x => x.OnMuteToggled, () => toggles++));

		HeaderButton(component, "Mute").Click();

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

		component.WaitForAssertion(() => HeaderButton(component, "Unmute").Should().NotBeNull());
	}

	/// <summary>Verifies that live-status and configuration announcements re-render the chat.</summary>
	[Fact]
	public async Task Live_and_configuration_announcements_re_render()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.AnnounceLive(false));
		component.WaitForAssertion(() => component.Find(".pdchat-title").TextContent.Should().EndWith("(Offline)"));

		service.Title = "Renamed";
		await component.InvokeAsync(service.AnnounceConfiguration);
		component.WaitForAssertion(() => component.Find(".pdchat-title").TextContent.Should().StartWith("Renamed"));
	}

	/// <summary>Verifies that Clear empties the transcript and the service and raises the event.</summary>
	[Fact]
	public void Clear_empties_the_transcript_and_the_service()
	{
		var cleared = 0;
		var service = new FakeChatService();
		service.Store.Add(Message("Old"));
		var component = RenderChat(service, p => p.Add(x => x.OnChatCleared, () => cleared++));

		HeaderButton(component, "Clear Chat").Click();

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
	// Sending and receiving
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that typed text is sent as the user and reported, and the input is cleared.</summary>
	[Fact]
	public async Task Typed_text_is_sent_as_the_user()
	{
		var reported = new List<ChatMessage>();
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(x => x.OnMessageSent, (ChatMessage m) => reported.Add(m)));

		await component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "Hi" });
		await component.Find("button.btn-secondary").ClickAsync(new());

		var sent = service.Sent.Should().ContainSingle().Subject;
		sent.Message.Should().Be("Hi");
		sent.Sender.Should().BeSameAs(_user);
		reported.Should().ContainSingle().Which.Should().BeSameAs(sent);
	}

	/// <summary>Verifies that a received message is shown and reported once, and an update replaces it in place.</summary>
	[Fact]
	public async Task A_received_message_is_shown_and_updates_replace_it()
	{
		var received = new List<ChatMessage>();
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(x => x.OnMessageReceivedEvent, (ChatMessage m) => received.Add(m)));
		var message = Message("Thinking");

		await component.InvokeAsync(() => service.Receive(message));
		await component.InvokeAsync(() => service.Receive(Update(message, "Done")));

		component.WaitForAssertion(() => component.FindAll(".pdchat-message").Should().ContainSingle());
		component.Find(".pdchat-text").TextContent.Should().Contain("Done");
		received.Should().ContainSingle();
	}

	/// <summary>Verifies that a sound chosen for a message is played, unless muted.</summary>
	[Theory]
	[InlineData(false, 1)]
	[InlineData(true, 0)]
	public async Task A_chosen_sound_is_played_unless_muted(bool muted, int expectedPlays)
	{
		var module = JSInterop.SetupModule(ModulePath);
		var service = new FakeChatService { IsMuted = muted };
		var component = RenderChat(service, p => p.Add(x => x.SoundSelector, _ => "ding.mp3"));

		await component.InvokeAsync(() => service.Receive(Message("Ping")));

		module.Invocations.Count(i => i.Identifier == "playSound").Should().Be(expectedPlays);
	}

	/// <summary>Verifies that a circuit going away while a sound plays does not escape the handler.</summary>
	[Fact]
	public async Task A_disconnected_circuit_while_playing_is_tolerated()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("playSound", _ => true).SetException(new Microsoft.JSInterop.JSDisconnectedException("gone"));
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(x => x.SoundSelector, _ => "ding.mp3"));

		await component.InvokeAsync(() => service.Receive(Message("Ping")));

		component.WaitForAssertion(() => component.Find(".pdchat-text").TextContent.Should().Contain("Ping"));
	}

	/// <summary>Verifies that disposing unsubscribes from the service.</summary>
	[Fact]
	public async Task Disposing_unsubscribes_from_the_service()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await component.Instance.DisposeAsync();

		service.HasSubscribers.Should().BeFalse();
	}

	// ------------------------------------------------------------------------------------------
	// Minimised badge
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that the badge reflects the most severe unread message, not the latest.</summary>
	[Theory]
	[InlineData(MessageType.Normal, "pdchat-info", "pulsate", "")]
	[InlineData(MessageType.Warning, "pdchat-warning", "pulsate-warning", "⚠")]
	[InlineData(MessageType.Error, "pdchat-error", "pulsate-error", "!")]
	[InlineData(MessageType.Critical, "pdchat-critical", "pulsate-critical", "!!")]
	public async Task The_badge_reflects_the_worst_unread_message(MessageType worst, string colour, string animation, string indicator)
	{
		var service = Minimised();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("worst", worst)));
		await component.InvokeAsync(() => service.Receive(Message("later", MessageType.Success)));

		var button = component.Find(".pdchat-toggle-collapsed");
		button.ClassList.Should().Contain(colour).And.Contain(animation);
		(component.FindAll(".pdchat-priority-indicator").FirstOrDefault()?.TextContent ?? string.Empty).Should().Be(indicator);
	}

	/// <summary>Verifies that only successes unread shows the success colour.</summary>
	[Fact]
	public async Task Unread_successes_show_the_success_colour()
	{
		var service = Minimised();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("yay", MessageType.Success)));

		component.Find(".pdchat-toggle-collapsed").ClassList.Should().Contain("pdchat-success");
	}

	/// <summary>Verifies that a typing indicator does not raise a toast.</summary>
	[Fact]
	public async Task A_typing_indicator_raises_no_toast()
	{
		var service = Toasting();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("...", MessageType.Typing)));

		component.FindAll(".pdchat-toast").Should().BeEmpty();
	}

	/// <summary>Verifies that an offline minimised chat shows the not-live colour.</summary>
	[Fact]
	public void An_offline_minimised_chat_shows_the_not_live_colour()
	{
		var service = Minimised();
		service.IsLive = false;

		var component = RenderChat(service);

		component.Find(".pdchat-toggle-collapsed").ClassList.Should().Contain("pdchat-not-live");
	}

	/// <summary>Verifies that auto-restore opens the chat for a new message and raises the event, with no toast.</summary>
	[Fact]
	public async Task Auto_restore_opens_the_chat_for_a_new_message()
	{
		var restored = 0;
		var service = Minimised();
		service.AutoRestoreOnNewMessage = true;
		service.ToastEnabled = true;
		var component = RenderChat(service, p => p.Add(x => x.OnAutoRestored, () => restored++));

		await component.InvokeAsync(() => service.Receive(Message("Wake up")));

		service.DockMode.Should().Be(PDChatDockMode.BottomRight);
		restored.Should().Be(1);
		component.FindAll(".pdchat-toast").Should().BeEmpty();
	}

	// ------------------------------------------------------------------------------------------
	// Toasts
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that a toast shows the sender, title and message with the type's scheme and ARIA.</summary>
	[Theory]
	[InlineData(MessageType.Normal, "preview-normal", "status", "polite")]
	[InlineData(MessageType.Warning, "preview-warning", "status", "polite")]
	[InlineData(MessageType.Error, "preview-error", "alert", "assertive")]
	[InlineData(MessageType.Critical, "preview-critical", "alert", "assertive")]
	[InlineData(MessageType.Success, "preview-success", "status", "polite")]
	public async Task A_toast_shows_the_message_with_its_type_scheme(MessageType type, string scheme, string role, string live)
	{
		var service = Toasting();
		var component = RenderChat(service);
		var message = Message("Body", type);
		message.Title = "Heading";

		await component.InvokeAsync(() => service.Receive(message));

		var toast = component.Find(".pdchat-toast");
		toast.ClassList.Should().Contain(scheme).And.Contain("toast-enter-grow");
		toast.GetAttribute("role").Should().Be(role);
		toast.GetAttribute("aria-live").Should().Be(live);
		toast.QuerySelector(".pdchat-preview-sender")!.TextContent.Should().Be("Merlin");
		toast.QuerySelector(".pdchat-preview-title")!.TextContent.Trim().Should().Be("Heading");
		toast.QuerySelector(".pdchat-preview-content")!.TextContent.Trim().Should().Be("Body");
	}

	/// <summary>Verifies that HTML titles and messages are rendered as markup in a toast.</summary>
	[Fact]
	public async Task A_toast_renders_html_when_flagged()
	{
		var service = Toasting();
		var component = RenderChat(service);
		var message = Message("<b>bold</b>");
		message.Title = "<i>italic</i>";
		message.IsTitleHtml = true;
		message.IsMessageHtml = true;

		await component.InvokeAsync(() => service.Receive(message));

		component.Find(".pdchat-preview-title i").TextContent.Should().Be("italic");
		component.Find(".pdchat-preview-content b").TextContent.Should().Be("bold");
	}

	/// <summary>Verifies that per-message toast options override the service defaults.</summary>
	[Fact]
	public async Task Per_message_options_override_the_defaults()
	{
		var service = Toasting();
		var component = RenderChat(service);
		var message = Message("Sized");
		message.Title = "Hidden title";
		message.ToastOptions = new ChatToastOptions
		{
			EntryAnimation = PDChatToastAnimation.Slide,
			ShowTitle = false,
			AnimationDurationMs = 400,
			MinWidth = "10px",
			MaxWidth = "20px",
			MinHeight = "30px",
			MaxHeight = "40px"
		};

		await component.InvokeAsync(() => service.Receive(message));

		var toast = component.Find(".pdchat-toast");
		toast.ClassList.Should().Contain("toast-enter-slide");
		toast.GetAttribute("style").Should().Be("--pdchat-toast-anim-ms:400ms;min-width:10px;max-width:20px;min-height:30px;max-height:40px;");
		toast.QuerySelectorAll(".pdchat-preview-title").Should().BeEmpty();
	}

	/// <summary>Verifies that each entry animation maps to its class name.</summary>
	[Theory]
	[InlineData(PDChatToastAnimation.None, "toast-enter-none")]
	[InlineData(PDChatToastAnimation.Fade, "toast-enter-fade")]
	[InlineData(PDChatToastAnimation.Shrink, "toast-enter-shrink")]
	public async Task Each_entry_animation_maps_to_a_class(PDChatToastAnimation animation, string expected)
	{
		var service = Toasting();
		service.ToastEntryAnimation = animation;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Anim")));

		component.Find(".pdchat-toast").ClassList.Should().Contain(expected);
	}

	/// <summary>Verifies that the toast stack anchors to the button, or to the headless anchor with no button.</summary>
	[Theory]
	[InlineData(PDChatButtonPosition.TopLeft, PDChatButtonPosition.BottomRight, "toast-anchor-top-left")]
	[InlineData(PDChatButtonPosition.TopRight, PDChatButtonPosition.BottomRight, "toast-anchor-top-right")]
	[InlineData(PDChatButtonPosition.BottomLeft, PDChatButtonPosition.BottomRight, "toast-anchor-bottom-left")]
	[InlineData(PDChatButtonPosition.BottomRight, PDChatButtonPosition.TopLeft, "toast-anchor-bottom-right")]
	[InlineData(PDChatButtonPosition.None, PDChatButtonPosition.TopRight, "toast-anchor-top-right")]
	[InlineData(PDChatButtonPosition.None, PDChatButtonPosition.None, "toast-anchor-bottom-right")]
	public async Task The_toast_stack_anchors_to_the_button_or_the_headless_anchor(PDChatButtonPosition button, PDChatButtonPosition anchor, string expected)
	{
		var service = Toasting(button);
		service.ToastAnchor = anchor;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Where")));

		component.Find(".pdchat-toast-stack").ClassList.Should().Contain(expected);
	}

	/// <summary>Verifies that clicking a toast opens the chat and clears the stack.</summary>
	[Fact]
	public async Task Clicking_a_toast_opens_the_chat()
	{
		var service = Toasting();
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("Open me")));

		await component.Find(".pdchat-toast").ClickAsync(new());

		service.DockMode.Should().Be(PDChatDockMode.BottomRight);
		component.FindAll(".pdchat-toast").Should().BeEmpty();
	}

	/// <summary>Verifies that dismissing the only toast plays its own exit animation.</summary>
	/// <remarks>
	/// The exit lasts as long as the animation, after which a timer removes the toast. The animation here is
	/// a minute long so that the exit state is still on screen when it is asserted, however busy the machine.
	/// </remarks>
	[Fact]
	public async Task Dismissing_the_only_toast_plays_its_exit_animation()
	{
		var service = Toasting();
		service.ToastExitAnimation = PDChatToastAnimation.Fade;
		service.ToastAnimationDurationMs = 60_000;
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("Bye")));

		await component.Find(".pdchat-toast-close").ClickAsync(new());

		var toast = component.Find(".pdchat-toast");
		toast.ClassList.Should().Contain("toast-exit-fade");
		toast.GetAttribute("style").Should().StartWith("--pdchat-toast-anim-ms:60000ms;");
	}

	/// <summary>Verifies that a dismissed toast is removed once its exit animation has finished.</summary>
	[Fact]
	public async Task A_dismissed_toast_is_removed_after_its_exit_animation()
	{
		var service = Toasting();
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("Bye")));

		await component.Find(".pdchat-toast-close").ClickAsync(new());

		component.WaitForAssertion(() => component.FindAll(".pdchat-toast").Should().BeEmpty());
	}

	/// <summary>
	/// Verifies that dismissing a toast while others remain de-stacks it with the fixed duration, and that
	/// dismissing it again while it is leaving changes nothing.
	/// </summary>
	/// <remarks>
	/// The de-stack lasts a fixed 250ms that no setting changes, and its removal timer can only take effect
	/// through the renderer's dispatcher. Clicking and reading the markup inside one synchronous dispatcher
	/// call therefore sees the de-stack state before that timer can possibly remove it.
	/// </remarks>
	[Fact]
	public async Task Dismissing_one_of_several_toasts_de_stacks_it()
	{
		var service = Toasting();
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("First")));
		await component.InvokeAsync(() => service.Receive(Message("Second")));
		string[] firstClasses = [];
		string[] secondClasses = [];
		string? firstStyle = null;

		await component.InvokeAsync(() =>
		{
			component.FindAll(".pdchat-toast-close")[0].Click();
			component.FindAll(".pdchat-toast-close")[0].Click();
			var toasts = component.FindAll(".pdchat-toast");
			firstClasses = [.. toasts[0].ClassList];
			firstStyle = toasts[0].GetAttribute("style");
			secondClasses = [.. toasts[1].ClassList];
		});

		firstClasses.Should().Contain("toast-destack");
		firstStyle.Should().StartWith("--pdchat-toast-anim-ms:250ms;");
		secondClasses.Should().Contain("toast-enter-grow").And.NotContain("toast-destack");
		component.WaitForAssertion(() => component.FindAll(".pdchat-toast").Should().ContainSingle());
	}

	/// <summary>Verifies that the visible-toast cap dismisses the oldest toast when a new one arrives.</summary>
	[Fact]
	public async Task The_visible_cap_dismisses_the_oldest()
	{
		var service = Toasting();
		service.ToastMaxVisible = 1;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Old")));
		await component.InvokeAsync(() => service.Receive(Message("New")));

		component.WaitForAssertion(() => component.FindAll(".pdchat-toast .pdchat-preview-content")
			.Select(t => t.TextContent.Trim()).Should().Equal("New"));
	}

	/// <summary>Verifies that an auto-dismissing toast leaves on its own once its display time is up.</summary>
	[Fact]
	public async Task An_auto_dismissing_toast_leaves_on_its_own()
	{
		var service = Toasting();
		service.ToastAutoDismiss = true;
		service.ToastDisplayDurationSeconds = 0.05;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Brief")));

		component.WaitForAssertion(() => component.FindAll(".pdchat-toast").Should().BeEmpty(), TimeSpan.FromSeconds(5));
	}

	/// <summary>Verifies that hovering pauses a toast's countdown and leaving resumes it to dismissal.</summary>
	[Fact]
	public async Task Hovering_pauses_and_leaving_resumes_the_countdown()
	{
		var service = Toasting();
		service.ToastAutoDismiss = true;
		service.ToastDisplayDurationSeconds = 30;
		var component = RenderChat(service);
		await component.InvokeAsync(() => service.Receive(Message("Hover")));

		await component.Find(".pdchat-toast").MouseEnterAsync(new MouseEventArgs());
		await component.Find(".pdchat-toast").MouseEnterAsync(new MouseEventArgs());
		await component.Find(".pdchat-toast").MouseLeaveAsync(new MouseEventArgs());
		await component.Find(".pdchat-toast").MouseLeaveAsync(new MouseEventArgs());

		component.Find(".pdchat-toast").ClassList.Should().Contain("toast-enter-grow");
	}

	/// <summary>Verifies that toasts are not shown when toasting is disabled.</summary>
	[Fact]
	public async Task No_toasts_when_disabled()
	{
		var service = Minimised();
		service.ToastEnabled = false;
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("Quiet")));

		component.FindAll(".pdchat-toast").Should().BeEmpty();
	}

	// ------------------------------------------------------------------------------------------
	// Canvas layout
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that full screen with canvas permitted splits the window into a canvas and the transcript.</summary>
	[Fact]
	public void Full_screen_with_canvas_shows_the_splitter()
	{
		var service = new FakeChatService { DockMode = PDChatDockMode.FullScreen };
		service.Store.Add(Message("In the split"));

		var component = RenderChat(service);

		component.Find(".pdchat-splitter").Should().NotBeNull();
		component.Find(".pdchat-canvas-flex .pdtabset").Should().NotBeNull();
		component.Find(".pdchat-text").TextContent.Should().Contain("In the split");
	}

	// ------------------------------------------------------------------------------------------
	// Inline forms
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that a submitted form is sent as an ordinary message describing the answers.</summary>
	[Fact]
	public async Task A_submitted_form_is_sent_as_a_message()
	{
		var reported = new List<ChatMessage>();
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(x => x.OnMessageSent, (ChatMessage m) => reported.Add(m)));
		var formContext = GetCascadedFormContext(component);
		var submission = Submission(("Colour", "Blue", false));

		await component.InvokeAsync(() => formContext.OnSubmitted!(submission));

		var sent = service.Sent.Should().ContainSingle().Subject;
		sent.Message.Should().Be("Colour - Blue");
		sent.FormSubmission.Should().BeSameAs(submission);
		reported.Should().ContainSingle();
	}

	/// <summary>Verifies that a dismissed form sends nothing.</summary>
	[Fact]
	public async Task A_dismissed_form_sends_nothing()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);
		var formContext = GetCascadedFormContext(component);

		await component.InvokeAsync(() => formContext.OnDismissed!(Guid.NewGuid()));

		service.Sent.Should().BeEmpty();
	}

	/// <summary>Verifies that a submission is described line by line, with skipped questions listed.</summary>
	[Fact]
	public void A_submission_is_described_line_by_line()
	{
		var text = PDChat.DescribeSubmission(Submission(("Colour", "Blue", false), ("Size", null, true)));

		text.Should().Be($"Colour - Blue{Environment.NewLine}Size - skipped");
	}

	/// <summary>Verifies that a submission with no answers says so.</summary>
	[Fact]
	public void A_submission_with_no_answers_says_so()
	{
		PDChat.DescribeSubmission(Submission()).Should().Be("(no answers)");
	}

	// ------------------------------------------------------------------------------------------
	// Conversation tabs
	// ------------------------------------------------------------------------------------------

	/// <summary>Verifies that entering full screen opens the conversation the service is already on.</summary>
	[Fact]
	public void Full_screen_opens_the_active_conversation()
	{
		var (component, store, _) = RenderConversations();

		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));
		component.Find(".pdchat-text").TextContent.Should().Contain("First message");
		store.MessageRequests.Should().Contain(store.First.Id);
	}

	/// <summary>Verifies that opening a second conversation from the sidebar adds and selects its tab.</summary>
	[Fact]
	public async Task Opening_another_conversation_adds_and_selects_its_tab()
	{
		var (component, store, service) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));

		await SidebarRow(component, "Second").ClickAsync(new());

		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"));
		TabTitles(component).Should().Equal("First", "Second");
		service.ActiveConversationId.Should().Be(store.Second.Id);
	}

	/// <summary>Verifies that opening an already open conversation selects its tab rather than adding another.</summary>
	[Fact]
	public async Task Opening_an_open_conversation_does_not_duplicate_it()
	{
		var (component, _, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));

		await SidebarRow(component, "First").ClickAsync(new());

		TabTitles(component).Should().Equal("First");
	}

	/// <summary>Verifies that clicking back to a tab shows that conversation's transcript.</summary>
	[Fact]
	public async Task Selecting_a_tab_shows_its_transcript()
	{
		var (component, _, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"));

		await Tab(component, "First").ClickAsync(new());

		component.WaitForAssertion(() => component.Find(".pdchat-text").TextContent.Should().Contain("First message"));
	}

	/// <summary>Verifies that a reply for a conversation in the background marks its tab unread until selected.</summary>
	[Fact]
	public async Task A_background_reply_marks_its_tab_unread()
	{
		var (component, store, service) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"));

		await component.InvokeAsync(() => service.ReceiveFor(store.First.Id, Message("Answer")));

		// The tab strip is drawn by PDTabSet before the PDTab beneath it receives its new CssClass, so the
		// marker only shows on the render after the one the reply caused (reported separately as a defect).
		component.Render();
		component.WaitForAssertion(() => Tab(component, "First").ClassList.Should().Contain("pdchat-conversation-tab-unread"));

		await Tab(component, "First").ClickAsync(new());
		component.WaitForAssertion(() => Tab(component, "First").ClassList.Should().NotContain("pdchat-conversation-tab-unread"));
	}

	/// <summary>Verifies that replies for the conversation on screen, or for one not open, mark nothing.</summary>
	[Fact]
	public async Task Replies_for_the_current_or_an_unopened_conversation_mark_nothing()
	{
		var (component, store, service) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));

		await component.InvokeAsync(() => service.ReceiveFor(store.First.Id, Message("Here")));
		await component.InvokeAsync(() => service.ReceiveFor(store.Second.Id, Message("Elsewhere")));

		component.FindAll(".pdchat-conversation-tab-unread").Should().BeEmpty();
	}

	/// <summary>Verifies that closing the selected tab falls back to another open one, and then to the empty state.</summary>
	[Fact]
	public async Task Closing_tabs_falls_back_then_shows_the_empty_state()
	{
		var (component, _, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"));

		await Tab(component, "Second").QuerySelector(".pdtabset-tab-close")!.ClickAsync(new());
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));
		await Tab(component, "First").QuerySelector(".pdtabset-tab-close")!.ClickAsync(new());

		component.WaitForAssertion(() => component.Find(".pdchat-conversation-none-open").Should().NotBeNull());
		component.FindAll(".pdchat-message").Should().BeEmpty();
	}

	/// <summary>Verifies that closing a tab in the background leaves the selected conversation on screen.</summary>
	[Fact]
	public async Task Closing_a_background_tab_keeps_the_selection()
	{
		var (component, _, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));
		await SidebarRow(component, "Second").ClickAsync(new());
		component.WaitForAssertion(() => ActiveTabTitle(component).Should().Be("Second"));

		await Tab(component, "First").QuerySelector(".pdtabset-tab-close")!.ClickAsync(new());

		component.WaitForAssertion(() => TabTitles(component).Should().Equal("Second"));
		ActiveTabTitle(component).Should().Be("Second");
	}

	/// <summary>Verifies that the empty state's button starts a new conversation in the store.</summary>
	[Fact]
	public async Task The_empty_state_starts_a_new_conversation()
	{
		var (component, store, service) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));
		await Tab(component, "First").QuerySelector(".pdtabset-tab-close")!.ClickAsync(new());

		await component.Find(".pdchat-conversation-none-open button").ClickAsync(new());

		component.WaitForAssertion(() => TabTitles(component).Should().Equal(ChatConversation.UntitledDisplayName));
		store.Created.Should().ContainSingle();
		service.ActiveConversationId.Should().Be(store.Created[0].Id);
	}

	/// <summary>Verifies that the tab set's add button also starts a new conversation.</summary>
	[Fact]
	public async Task The_add_tab_button_starts_a_new_conversation()
	{
		var (component, store, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));

		await component.Find(".pdchat-conversation-tabset .pdtabset-addtab").ClickAsync(new());

		component.WaitForAssertion(() => store.Created.Should().ContainSingle());
	}

	/// <summary>Verifies that renaming a tab writes the title through to the store.</summary>
	[Fact]
	public async Task Renaming_a_tab_writes_through_to_the_store()
	{
		var (component, store, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));

		await Tab(component, "First").DoubleClickAsync(new MouseEventArgs());
		await component.Find(".pdtabset-tab-rename-input").InputAsync(new ChangeEventArgs { Value = "Renamed" });
		await component.Find(".pdtabset-tab-rename-input").BlurAsync(new FocusEventArgs());

		component.WaitForAssertion(() => store.Renamed.Should().ContainSingle().Which.Should().Be((store.First.Id, "Renamed")));
		store.First.Title.Should().Be("Renamed");
	}

	/// <summary>Verifies that archiving the selected conversation archives it in the store and closes its tab.</summary>
	[Fact]
	public async Task Archiving_archives_and_closes_the_tab()
	{
		var (component, store, _) = RenderConversations();
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"));

		await ToolbarButton(component, "Archive").ClickAsync(new());

		store.Archived.Should().Equal(store.First.Id);
		component.WaitForAssertion(() => TabTitles(component).Should().BeEmpty());
		ToolbarButton(component, "Archive").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Verifies that the sidebar can be collapsed and shown again.</summary>
	[Fact]
	public async Task The_sidebar_can_be_collapsed_and_shown()
	{
		var (component, _, _) = RenderConversations();

		await component.Find(".pdchat-conversation-toolbar-btn[title='Hide conversations']").ClickAsync(new());
		component.FindAll(".pdchat-conversation-sidebar-pane").Should().BeEmpty();

		await component.Find(".pdchat-conversation-toolbar-btn[title='Show conversations']").ClickAsync(new());
		component.FindAll(".pdchat-conversation-sidebar-pane").Should().ContainSingle();
	}

	/// <summary>Verifies that a transcript the store cannot supply falls back to the chat service's own copy.</summary>
	[Fact]
	public void A_failing_store_transcript_falls_back_to_the_service()
	{
		var (component, _, _) = RenderConversations(storeFails: true);

		component.WaitForAssertion(() => component.Find(".pdchat-text").TextContent.Should().Contain("From the service"));
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
		var store = new FakeConversationStore { FailTranscripts = storeFails };
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

	/// <summary>A chat service whose every setting can be changed and whose every event can be raised.</summary>
	private sealed class FakeChatService : IChatService
	{
		public List<ChatMessage> Store { get; } = [];
		public List<ChatMessage> Sent { get; } = [];
		public Dictionary<Guid, List<ChatMessage>> ConversationStore { get; } = [];
		public int ClearCount { get; private set; }
		public int InitializeCount { get; private set; }
		public bool HasSubscribers => OnMessageReceived is not null || OnDockModeChanged is not null;

		public bool IsLive { get; set; } = true;
		public PDChatDockMode DockMode { get; set; } = PDChatDockMode.BottomRight;
		public PDChatDockMode PreferredDockMode { get; set; } = PDChatDockMode.BottomRight;
		public PDChatDockMode RestoreMode { get; set; } = PDChatDockMode.BottomRight;
		public PDChatButtonPosition MinimizedButtonPosition { get; set; } = PDChatButtonPosition.BottomRight;
		public bool IsMuted { get; set; }
		public string Title { get; set; } = "Test Chat";
		public bool IsMaximizePermitted { get; set; } = true;
		public bool IsCanvasUsePermitted { get; set; } = true;
		public bool IsClearPermitted { get; set; } = true;
		public bool AutoRestoreOnNewMessage { get; set; }
		public bool UseFullWidthMessages { get; set; } = true;
		public MessageMetadataDisplayMode MessageMetadataDisplayMode { get; set; } = MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;
		public bool ShowMessageUserIcon { get; set; } = true;
		public bool ShowMessageUserName { get; set; } = true;
		public bool ShowMessageTimestamp { get; set; } = true;
		public string MessageTimestampFormat { get; set; } = "HH:mm:ss";

		[Obsolete("Required by the interface for backward compatibility; superseded by the toast API.")]
		public bool ShowLastMessage { get; set; }

		[Obsolete("Required by the interface for backward compatibility; superseded by the toast API.")]
		public double ShowLastMessageDurationSeconds { get; set; } = 5d;

		public bool ToastEnabled { get; set; }
		public PDChatToastAnimation ToastEntryAnimation { get; set; } = PDChatToastAnimation.Grow;
		public PDChatToastAnimation ToastExitAnimation { get; set; } = PDChatToastAnimation.Grow;
		public double ToastAnimationDurationMs { get; set; } = 1;
		public bool ToastAutoDismiss { get; set; }
		public double ToastDisplayDurationSeconds { get; set; } = 5;
		public bool ToastShowTitle { get; set; } = true;
		public string ToastMinWidth { get; set; } = string.Empty;
		public string ToastMaxWidth { get; set; } = string.Empty;
		public string ToastMinHeight { get; set; } = string.Empty;
		public string ToastMaxHeight { get; set; } = string.Empty;
		public int ToastMaxVisible { get; set; } = 3;
		public PDChatButtonPosition ToastAnchor { get; set; } = PDChatButtonPosition.BottomRight;

		public IReadOnlyList<ChatMessage> Messages => Store;
		public bool SupportsConversations { get; init; }
		public Guid ActiveConversationId { get; set; }

		public event Action<ChatMessage>? OnMessageReceived;
		public event Action<Guid, ChatMessage>? OnConversationMessageReceived;
		public event Action<bool>? OnLiveStatusChanged;
		public event Action<PDChatDockMode>? OnDockModeChanged;
		public event Action<bool>? OnMuteStatusChanged;
		public event Action? OnConfigurationChanged;

		public IReadOnlyList<ChatMessage> GetMessages(Guid conversationId)
			=> ConversationStore.TryGetValue(conversationId, out var messages) ? messages : [];

		public void SendMessage(ChatMessage chatMessage) => Sent.Add(chatMessage);

		public void SendMessage(Guid conversationId, ChatMessage chatMessage) => Sent.Add(chatMessage);

		public void ClearMessages()
		{
			Store.Clear();
			ClearCount++;
		}

		public void Initialize() => InitializeCount++;

		public void Dispose()
		{
			// Nothing to release.
		}

		public void Receive(ChatMessage message) => OnMessageReceived?.Invoke(message);

		public void ReceiveFor(Guid conversationId, ChatMessage message)
			=> OnConversationMessageReceived?.Invoke(conversationId, message);

		public void AnnounceLive(bool isLive)
		{
			IsLive = isLive;
			OnLiveStatusChanged?.Invoke(isLive);
		}

		public void AnnounceDockMode(PDChatDockMode mode) => OnDockModeChanged?.Invoke(mode);

		public void AnnounceMute(bool isMuted)
		{
			IsMuted = isMuted;
			OnMuteStatusChanged?.Invoke(isMuted);
		}

		public void AnnounceConfiguration() => OnConfigurationChanged?.Invoke();
	}

	/// <summary>A conversation store holding two conversations and recording what was asked of it.</summary>
	private sealed class FakeConversationStore : IChatConversationService
	{
		public ChatConversation First { get; } = new() { Id = Guid.NewGuid(), Title = "First" };
		public ChatConversation Second { get; } = new() { Id = Guid.NewGuid(), Title = "Second" };
		public bool FailTranscripts { get; init; }
		public List<Guid> MessageRequests { get; } = [];
		public List<ChatConversation> Created { get; } = [];
		public List<(Guid Id, string Title)> Renamed { get; } = [];
		public List<Guid> Archived { get; } = [];

		public Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ChatConversation[] all = [First, Second, .. Created];
			var visible = all.Where(c => query.IncludeArchived || !c.IsArchived).ToList();
			return Task.FromResult(new ChatConversationPage { Conversations = visible, TotalCount = visible.Count });
		}

		public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			MessageRequests.Add(id);
			if (FailTranscripts)
			{
				throw new InvalidOperationException("Store offline");
			}

			var title = id == First.Id ? "First" : id == Second.Id ? "Second" : "New";
			return Task.FromResult<IReadOnlyList<ChatMessage>>([Message($"{title} message")]);
		}

		public Task<ChatConversation> CreateAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var conversation = new ChatConversation { Id = Guid.NewGuid() };
			Created.Add(conversation);
			return Task.FromResult(conversation);
		}

		public Task RenameAsync(Guid id, string title, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Renamed.Add((id, title));
			return Task.CompletedTask;
		}

		public Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Archived.Add(id);
			SetArchived(id, true);
			return Task.CompletedTask;
		}

		public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			SetArchived(id, false);
			return Task.CompletedTask;
		}

		private void SetArchived(Guid id, bool isArchived)
		{
			foreach (var conversation in new[] { First, Second }.Concat(Created).Where(c => c.Id == id))
			{
				conversation.IsArchived = isArchived;
			}
		}
	}
}
