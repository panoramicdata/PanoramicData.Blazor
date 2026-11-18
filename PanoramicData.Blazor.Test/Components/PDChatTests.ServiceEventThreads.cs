using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that the chat handles a service event raised from another thread, as a timer, a background poll or another
/// circuit sharing a singleton service raises it, on its own dispatcher, and that a failure there is logged rather than
/// ending the circuit.
/// </summary>
public partial class PDChatTests
{
	private ListLogger<PDChat> UseRecordingChatLogger()
	{
		var logger = new ListLogger<PDChat>();
		Services.AddSingleton<ILogger<PDChat>>(logger);
		return logger;
	}

	/// <summary>Verifies that a message raised from a thread-pool thread is shown.</summary>
	[Fact]
	public async Task A_message_raised_from_another_thread_is_shown()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await Task.Run(() => service.Receive(Message("From a background poll")), Xunit.TestContext.Current.CancellationToken);

		component.WaitForAssertion(() => component.Markup.Should().Contain("From a background poll"), Patience);
	}

	/// <summary>
	/// Verifies that the host's message callback throwing, from another thread, is logged and the chat goes on working.
	/// </summary>
	[Fact]
	public async Task A_failing_host_callback_is_logged_and_the_chat_goes_on()
	{
		var logger = UseRecordingChatLogger();
		var service = new FakeChatService();
		var component = RenderChat(service, p => p.Add(
			x => x.OnMessageReceivedEvent,
			(ChatMessage _) => throw new InvalidOperationException("host failed")));

		await Task.Run(() => service.Receive(Message("First")), Xunit.TestContext.Current.CancellationToken);
		component.WaitForAssertion(() => logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
			.Which.Exception.Should().BeOfType<InvalidOperationException>()
			.Which.Message.Should().Be("host failed"), Patience);

		await Task.Run(() => service.Receive(Message("Second")), Xunit.TestContext.Current.CancellationToken);
		component.WaitForAssertion(() => component.Markup.Should().Contain("First").And.Contain("Second"), Patience);
	}

	/// <summary>Verifies that a dock mode change raised from another thread re-renders the chat in its new place.</summary>
	[Fact]
	public async Task A_dock_mode_change_raised_from_another_thread_moves_the_chat()
	{
		var service = new FakeChatService();
		var component = RenderChat(service);

		await Task.Run(() => service.AnnounceDockMode(PDChatDockMode.TopLeft), Xunit.TestContext.Current.CancellationToken);

		component.WaitForAssertion(
			() => component.Find(".pdchat-container").ClassList.Should().Contain("dock-top-left"),
			Patience);
	}
}
