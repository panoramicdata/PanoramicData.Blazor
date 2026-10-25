using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Issue #265: Voice Mode's modes. HAL listens at once and sends on a pause; Dictate listens only while the text box has
/// focus and never sends by itself; Wake starts dormant and returns there after each exchange. Issue #266: a host may
/// supply the text read aloud.
/// </summary>
public partial class PDChatTests
{
	/// <summary>Verifies that HAL is the default, so a host that sets nothing keeps the behaviour it had.</summary>
	[Fact]
	public void HAL_is_the_default_voice_mode()
	{
		IChatService service = new FakeChatService();

		service.VoiceMode.Should().Be(PDChatVoiceMode.Hal);
		RenderChat(VoiceService()).Instance.VoiceMode.Should().Be(PDChatVoiceMode.Hal);
	}

	/// <summary>Verifies that the mode picker offers Wake only when the host has wake phrases.</summary>
	[Fact]
	public void The_wake_mode_is_offered_only_with_wake_phrases()
	{
		RenderChat(VoiceService()).FindAll(".pdchat-voice-mode-picker option").Select(option => option.GetAttribute("value"))
			.Should().Equal("MicOff", "Hal", "Dictate");

		var withWake = RenderChat(WakeService()).FindAll(".pdchat-voice-mode-picker option");
		withWake.Select(option => option.GetAttribute("value")).Should().Equal("MicOff", "Hal", "Dictate", "Wake");
		withWake[3].TextContent.Should().Be("Hey DumbBot");
	}

	/// <summary>Verifies that a chosen Wake mode with no wake phrases behaves, and shows, as HAL.</summary>
	[Fact]
	public void Wake_without_wake_phrases_is_HAL()
	{
		var service = VoiceService();
		((IChatService)service).VoiceMode = PDChatVoiceMode.Wake;

		var component = RenderChat(service);

		component.Instance.VoiceMode.Should().Be(PDChatVoiceMode.Hal);
		component.FindAll(".pdchat-voice-toggle .pdchat-hal-eye").Should().ContainSingle();
	}

	/// <summary>Verifies that each mode has its own icon, so the mode never depends on colour alone.</summary>
	[Theory]
	[InlineData(PDChatVoiceMode.Dictate, "🎙️")]
	[InlineData(PDChatVoiceMode.Wake, "👂")]
	public void Each_mode_has_its_own_icon(PDChatVoiceMode mode, string icon)
	{
		var service = WakeService();
		((IChatService)service).VoiceMode = mode;

		var toggle = RenderChat(service).Find(".pdchat-voice-toggle");

		toggle.TextContent.Trim().Should().Be(icon);
		toggle.QuerySelector(".pdchat-hal-eye").Should().BeNull();
	}

	/// <summary>Verifies that the toggle carries its mode and state, for the indicator's styling.</summary>
	[Fact]
	public async Task The_toggle_carries_its_mode_and_state()
	{
		SetUpVoiceModule();
		var component = RenderChat(VoiceService());
		component.Find(".pdchat-voice-toggle").ClassList.Should().Contain(["pdchat-voice-mode-hal", "pdchat-voice-state-off"]);

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		component.Find(".pdchat-voice-toggle").ClassList.Should().Contain(["pdchat-voice-mode-hal", "pdchat-voice-state-listening", "active"]);
	}

	/// <summary>Verifies that choosing a mode while Voice is off starts it in that mode and records the choice.</summary>
	[Fact]
	public async Task Choosing_a_mode_starts_Voice_in_it()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = RenderChat(service);

		await component.Find(".pdchat-voice-mode-picker select").ChangeAsync(new ChangeEventArgs { Value = "Dictate" });

		((IChatService)service).VoiceMode.Should().Be(PDChatVoiceMode.Dictate);
		module.Invocations["start"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Paused);
	}

	/// <summary>Verifies that Dictate without focus sends no audio and types nothing, and says how to start.</summary>
	[Fact]
	public async Task Dictate_is_paused_until_the_text_box_has_focus()
	{
		var module = SetUpVoiceModule();
		var component = await RenderInModeAsync(VoiceService(), PDChatVoiceMode.Dictate);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Paused);
		module.Invocations["pause"].Should().ContainSingle().Which.Arguments[0].Should().Be(true);
		component.Find(".pdchat-voice-status").TextContent.Trim().Should().Be("Dictation paused. Click in the text box to dictate.");

		await SayAsync(component, "Is", "it", "done?");

		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
	}

	/// <summary>Verifies that in Dictate a pause never sends, even with the box focused: the user presses Send.</summary>
	[Fact]
	public async Task Dictate_dictates_while_focused_and_never_sends_by_itself()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService(TimeSpan.Zero, readAloud: false);
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Dictate);

		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true, false);

		await SayAsync(component, "Is", "it", "done?");
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		service.Sent.Should().BeEmpty();
		component.Find("textarea").GetAttribute("value").Should().Be("Is it done?");
		component.Find(".pdchat-voice-status").TextContent.Trim().Should().Be("Dictating. Press Send when ready.");

		await component.Find(".chat-input-accessories .chat-send").ClickAsync(new MouseEventArgs());

		service.Sent.Should().ContainSingle().Which.Message.Should().Be("Is it done?");
	}

	/// <summary>Verifies that leaving the text box in Dictate pauses the microphone again.</summary>
	[Fact]
	public async Task Dictate_pauses_when_the_text_box_loses_focus()
	{
		var module = SetUpVoiceModule();
		var component = await RenderInModeAsync(VoiceService(), PDChatVoiceMode.Dictate);

		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		await component.Find("textarea").BlurAsync(new FocusEventArgs());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Paused);
		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true, false, true);
	}

	/// <summary>Verifies that Dictate never goes dormant, even with wake phrases and a short idle timeout.</summary>
	[Fact]
	public async Task Dictate_never_goes_dormant()
	{
		SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).VoiceIdleTimeout = _briefIdle;
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Dictate);
		await component.Find("textarea").FocusAsync(new FocusEventArgs());

		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that Wake starts dormant, and once woken sends what follows on a pause.</summary>
	[Fact]
	public async Task Wake_starts_dormant_and_sends_on_a_pause_once_woken()
	{
		SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).VoiceAutoSendDelay = TimeSpan.Zero;
		((IChatService)service).IsReadAloudEnabled = false;
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Wake);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);

		await SayAsync(component, "Hey", "DumbBot", "is", "it", "done?");

		component.WaitForAssertion(() => service.Sent.Should().ContainSingle().Which.Message.Should().Be("is it done?"), Patience);
	}

	/// <summary>Verifies that Wake returns to dormant after an exchange with nothing to read aloud.</summary>
	[Fact]
	public async Task Wake_is_dormant_again_after_sending()
	{
		SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).VoiceAutoSendDelay = TimeSpan.Zero;
		((IChatService)service).IsReadAloudEnabled = false;
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Wake);

		await SayAsync(component, "Hey", "DumbBot", "is", "it", "done?");

		component.WaitForAssertion(() => service.Sent.Should().ContainSingle(), Patience);
		component.WaitForAssertion(() => component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant), Patience);
	}

	/// <summary>Verifies that Wake returns to dormant once a spoken answer has been read aloud.</summary>
	[Fact]
	public async Task Wake_is_dormant_again_after_the_answer_is_spoken()
	{
		var module = SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).VoiceAutoSendDelay = TimeSpan.Zero;
		((IChatService)service).IsReadAloudEnabled = true;
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Wake);
		await SayAsync(component, "Hey", "DumbBot", "is", "it", "done?");
		component.WaitForAssertion(() => service.Sent.Should().ContainSingle(), Patience);

		await component.InvokeAsync(() => service.Receive(Message("Yes.")));
		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true, false);
	}

	/// <summary>
	/// Verifies that clicking in the text box in HAL switches to manual: dictation still streams in, a pause sends
	/// nothing, Send sends, and the toggle shows the user is editing.
	/// </summary>
	[Fact]
	public async Task Editing_in_HAL_switches_to_manual_send_while_dictation_continues()
	{
		SetUpVoiceModule();
		var service = VoiceService(TimeSpan.Zero, readAloud: false);
		var component = await RenderListeningAsync(service);

		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		await SayAsync(component, "Is", "it", "done?");
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		service.Sent.Should().BeEmpty();
		component.Instance.IsVoiceEditing.Should().BeTrue();
		component.Find(".pdchat-voice-toggle").ClassList.Should().Contain("pdchat-voice-editing");

		await component.Find(".chat-input-accessories .chat-send").ClickAsync(new MouseEventArgs());
		await component.Find("textarea").BlurAsync(new FocusEventArgs());

		service.Sent.Should().ContainSingle();
		component.Instance.IsVoiceEditing.Should().BeFalse();

		await SayAsync(component, "And", "the", "other?");

		component.WaitForAssertion(() => service.Sent.Should().HaveCount(2), Patience);
	}

	/// <summary>Verifies that switching mode while listening applies at once.</summary>
	[Fact]
	public async Task Switching_mode_while_listening_applies_at_once()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = await RenderListeningAsync(service);

		await component.Find(".pdchat-voice-mode-picker select").ChangeAsync(new ChangeEventArgs { Value = "Dictate" });

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Paused);
		module.Invocations["start"].Should().ContainSingle("the microphone is already open");
		module.Invocations["pause"].Should().ContainSingle().Which.Arguments[0].Should().Be(true);
	}

	/// <summary>Issue #266: verifies that a host's spoken text is read aloud in place of the message, which is unchanged.</summary>
	[Fact]
	public async Task A_spoken_message_is_read_aloud_in_place_of_the_message()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = await RenderListeningAsync(service);
		await AskAsync(component, service, "Is it done?");

		var answer = Message("| Ticket | Status |\n|---|---|\n| MS-1 | Done |\n\n_Answered by a model_");
		answer.SpokenMessage = "Yes, it's done. I've tabulated the detail for you.";
		await component.InvokeAsync(() => service.Receive(answer));

		module.Invocations["speak"].Should().ContainSingle().Which.Arguments[1].Should().Be("Yes, it's done. I've tabulated the detail for you.");
		component.Markup.Should().Contain("MS-1", "the written answer is shown in full");
	}

	/// <summary>Issue #266: verifies that an empty spoken text reads nothing, and the microphone listens again.</summary>
	[Fact]
	public async Task An_empty_spoken_message_reads_nothing()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = await RenderListeningAsync(service);
		await AskAsync(component, service, "Is it done?");

		var answer = Message("Here is a chart.");
		answer.SpokenMessage = string.Empty;
		await component.InvokeAsync(() => service.Receive(answer));

		module.Invocations["speak"].Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Issue #266: verifies that an updated message keeps its spoken text, which is copied by hand on update.</summary>
	[Fact]
	public async Task An_update_carries_the_spoken_message()
	{
		var service = VoiceService();
		var component = RenderChat(service);
		var first = Message("Working on it");
		await component.InvokeAsync(() => service.Receive(first));

		var update = new ChatMessage { Id = first.Id, Sender = first.Sender, Message = "Done.", Type = MessageType.Normal, SpokenMessage = "All done." };
		await component.InvokeAsync(() => service.Receive(update));

		first.SpokenMessage.Should().Be("All done.");
	}

	private async Task<IRenderedComponent<PDChat>> RenderInModeAsync(FakeChatService service, PDChatVoiceMode mode)
	{
		((IChatService)service).VoiceMode = mode;
		return await RenderListeningAsync(service);
	}
}
