using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Sending, receiving, sound, unread badge and auto-restore tests for <see cref="PDChat"/>.
/// </summary>
public partial class PDChatTests
{
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

		component.WaitForAssertion(() => component.FindAll(".pdchat-message").Should().ContainSingle(), Patience);
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

		component.WaitForAssertion(() => component.Find(".pdchat-text").TextContent.Should().Contain("Ping"), Patience);
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

	/// <summary>Verifies that a typing indicator on its own leaves the minimised badge unmarked (#190).</summary>
	[Fact]
	public async Task A_typing_indicator_does_not_mark_the_badge_unread()
	{
		var service = Minimised();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("...", MessageType.Typing)));

		component.Find(".pdchat-toggle-collapsed").ClassList.Should().NotContain(["pdchat-info", "pulsate"]);
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
}
