using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that the chat service's choice of <see cref="ChatProgressDisplayMode"/> reaches the messages.
/// </summary>
public partial class PDChatTests
{
	/// <summary>Verifies that the service's choice of progress display reaches a message in every layout.</summary>
	[Theory]
	[InlineData(PDChatDockMode.BottomRight, ChatProgressDisplayMode.List)]
	[InlineData(PDChatDockMode.BottomRight, ChatProgressDisplayMode.Ticker)]
	[InlineData(PDChatDockMode.Right, ChatProgressDisplayMode.Ticker)]
	[InlineData(PDChatDockMode.FullScreen, ChatProgressDisplayMode.Ticker)]
	public async Task The_services_progress_display_reaches_the_message(PDChatDockMode dockMode, ChatProgressDisplayMode mode)
	{
		var service = new FakeChatService { DockMode = dockMode, ProgressDisplayMode = mode };
		var component = RenderChat(service);
		var typing = Message("...", MessageType.Typing);
		typing.ProgressSteps = ["Searching", "Reading"];

		await component.InvokeAsync(() => service.Receive(typing));

		component.WaitForAssertion(() =>
		{
			component.FindAll(".pdchat-progress-ticker").Should().HaveCount(mode == ChatProgressDisplayMode.Ticker ? 1 : 0);
			component.FindAll(".pdchat-progress-steps").Should().HaveCount(mode == ChatProgressDisplayMode.List ? 1 : 0);
		}, Patience);
	}
}
