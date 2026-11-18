using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that the chat service or the host's conversation store failing behind a button is logged, and the chat goes on
/// working, rather than ending the circuit.
/// </summary>
public partial class PDChatTests
{
	/// <summary>Verifies that a failed send is logged and the typed text is kept, so it can be sent again.</summary>
	[Fact]
	public async Task A_failed_send_is_logged_and_keeps_the_text()
	{
		var logger = UseRecordingChatLogger();
		var service = new FakeChatService { SendFailure = new InvalidOperationException("send failed") };
		var component = RenderChat(service);

		await component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "Hi" });
		await component.Find(".chat-send").ClickAsync(new());

		logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
			.Which.Exception!.Message.Should().Be("send failed");
		service.SendFailure = null;
		await component.Find(".chat-send").ClickAsync(new());
		service.Sent.Should().ContainSingle().Which.Message.Should().Be("Hi");
	}

	/// <summary>Verifies that the store failing to archive is logged and leaves the conversation open.</summary>
	[Fact]
	public async Task A_failed_archive_is_logged_and_leaves_the_conversation_open()
	{
		var logger = UseRecordingChatLogger();
		var (component, store, _) = RenderConversations();
		store.FailArchive = true;
		component.WaitForAssertion(() => TabTitles(component).Should().Equal("First"), Patience);

		await ToolbarButton(component, "Archive").ClickAsync(new());

		logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
			.Which.Exception!.Message.Should().Be("Archive failed");
		TabTitles(component).Should().Equal("First");
		ToolbarButton(component, "Archive").HasAttribute("disabled").Should().BeFalse();
	}
}
