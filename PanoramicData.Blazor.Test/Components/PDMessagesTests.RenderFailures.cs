using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that a message which fails to render is shown as a notice in its place, without taking down the others or the
/// circuit, and is tried again when the list renders.
/// </summary>
public partial class PDMessagesTests
{
	/// <summary>Verifies that a failing message shows a notice, the others still show, and it recovers once it can render.</summary>
	[Fact]
	public void A_message_that_fails_to_render_shows_a_notice_and_recovers()
	{
		var failing = true;
		List<ChatMessage> messages = [CreateMessage("good"), CreateMessage("bad")];
		Func<ChatMessage, string?> icon = message => failing && message.Message == "bad"
			? throw new InvalidOperationException("icon failed")
			: "*";
		var component = Render<PDMessages>(parameters => parameters
			.Add(p => p.Messages, messages)
			.Add(p => p.UserIconSelector, icon));

		component.FindAll(".pdchat-message-failed").Should().ContainSingle()
			.Which.TextContent.Should().Be("This message could not be shown.");
		component.Find(".pdchat-messages").TextContent.Should().Contain("good");

		failing = false;
		component.Render(parameters => parameters.Add(p => p.Messages, messages));

		component.FindAll(".pdchat-message-failed").Should().BeEmpty();
		component.Find(".pdchat-messages").TextContent.Should().Contain("bad");
	}
}
