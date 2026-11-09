using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that a browser call behind a chat button failing, or timing out while a prompt or picker is left open, is shown
/// to the user rather than ending the circuit.
/// </summary>
public partial class PDChatTests
{
	private const string MessagesModulePath = "./_content/PanoramicData.Blazor/PDMessages.razor.js";

	/// <summary>Verifies that the microphone permission prompt timing out turns Voice off and says why.</summary>
	[Fact]
	public async Task A_microphone_prompt_that_times_out_turns_Voice_off_and_says_so()
	{
		var module = SetUpVoiceModule();
		module.SetupVoid("start", _ => true).SetException(new TaskCanceledException());
		var component = RenderChat(VoiceService());

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		component.Find(".pdchat-voice-toggle").GetAttribute("aria-pressed").Should().Be("false");
		component.Markup.Should().Contain("The microphone could not be opened");
	}

	/// <summary>Verifies that the screen-share picker timing out shows a notice.</summary>
	[Fact]
	public async Task A_screenshot_that_times_out_shows_a_notice()
	{
		var module = JSInterop.SetupModule(MessagesModulePath);
		module.Mode = JSRuntimeMode.Loose;
		module.SetupVoid("captureTab", _ => true).SetException(new TaskCanceledException());
		var component = RenderChat(new FakeChatService { IsImageInputOffered = true });

		await component.Find(".pdchat-image-screenshot").ClickAsync(new());

		component.WaitForAssertion(
			() => component.Find(".pdchat-image-notice").TextContent.Should().Contain("could not be added"),
			Patience);
	}

	/// <summary>Verifies that reading the picked files failing in the browser shows a notice.</summary>
	[Fact]
	public async Task Picked_files_that_cannot_be_read_show_a_notice()
	{
		var module = JSInterop.SetupModule(MessagesModulePath);
		module.Mode = JSRuntimeMode.Loose;
		module.SetupVoid("readPickedFiles", _ => true).SetException(new JSException("read failed"));
		var component = RenderChat(new FakeChatService { IsImageInputOffered = true });

		await component.Find(".pdchat-image-file-input").ChangeAsync(new ChangeEventArgs());

		component.WaitForAssertion(
			() => component.Find(".pdchat-image-notice").TextContent.Should().Contain("could not be added"),
			Patience);
	}
}
