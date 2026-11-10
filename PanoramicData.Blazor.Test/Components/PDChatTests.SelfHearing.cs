using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// MS-27776: while an answer is read aloud nothing the microphone hears reaches the text box or is sent, whichever
/// state the microphone was in when the question was asked, and afterwards it returns to that state.
/// </summary>
public partial class PDChatTests
{
	private static readonly string[] _heardWhileSpeaking = ["Hey", "DumbBot,", "delete", "everything"];

	/// <summary>Verifies that a question typed while waiting for the wake phrase pauses the microphone for the answer.</summary>
	[Fact]
	public async Task A_typed_question_while_dormant_pauses_the_microphone_until_the_answer_is_spoken()
	{
		var module = SetUpVoiceModule();
		var component = await RenderDormantAsync(WakeService(TimeSpan.Zero));

		await TypeAndSendAsync(component, "Is it done?");

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Thinking);
		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true);
	}

	/// <summary>
	/// Verifies that words heard while an answer is spoken to a microphone that was waiting for the wake phrase are not
	/// typed or sent, and that the answer saying the wake phrase does not wake it.
	/// </summary>
	[Fact]
	public async Task Nothing_heard_while_an_answer_is_spoken_reaches_the_text_box_when_the_microphone_was_dormant()
	{
		var service = WakeService(TimeSpan.Zero);
		var component = await RenderDormantAsync(service);
		await TypeAndSendAsync(component, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Hey DumbBot, yes it is done.")));
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);

		await HearAsync(component, _heardWhileSpeaking);

		await AssertNothingHeardAsync(component, service);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);
	}

	/// <summary>Verifies that a dormant microphone is dormant again once the answer has been spoken, and still wakes.</summary>
	[Fact]
	public async Task A_microphone_that_was_dormant_is_dormant_again_after_the_answer()
	{
		var module = SetUpVoiceModule();
		var service = WakeService(TimeSpan.Zero);
		var component = await RenderDormantAsync(service);
		await TypeAndSendAsync(component, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true, false);
		await SayAsync(component, "Hey", "DumbBot");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that in Wake mode, waiting for the phrase, a typed question's answer is not heard either.</summary>
	[Fact]
	public async Task Nothing_heard_while_an_answer_is_spoken_reaches_the_text_box_in_Wake_mode()
	{
		var service = WakeService(TimeSpan.Zero);
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Wake);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
		await TypeAndSendAsync(component, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		await HearAsync(component, _heardWhileSpeaking);

		await AssertNothingHeardAsync(component, service);
		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
	}

	/// <summary>Verifies that in HAL, listening, the answer to a spoken question is not heard.</summary>
	[Fact]
	public async Task Nothing_heard_while_an_answer_is_spoken_reaches_the_text_box_in_HAL()
	{
		var service = VoiceService();
		var component = await RenderListeningAsync(service);
		await AskAsync(component, service, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);

		await HearAsync(component, _heardWhileSpeaking);

		await AssertNothingHeardAsync(component, service);
		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that words heard while an answer is still being awaited are not typed either.</summary>
	[Fact]
	public async Task Nothing_heard_while_the_answer_is_awaited_reaches_the_text_box()
	{
		var service = VoiceService();
		var component = await RenderListeningAsync(service);
		await AskAsync(component, service, "Is it done?");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Thinking);

		await HearAsync(component, _heardWhileSpeaking);

		await AssertNothingHeardAsync(component, service);
	}

	/// <summary>
	/// Verifies that in Dictate, with the text box unfocused, focusing it while the answer plays does not open the
	/// microphone, and that it listens as Dictate does once the answer has been spoken.
	/// </summary>
	[Fact]
	public async Task Focusing_the_text_box_while_an_answer_is_spoken_does_not_open_the_microphone_in_Dictate()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService(readAloud: true);
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Dictate);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Paused);
		await TypeAndSendAsync(component, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		await HearAsync(component, _heardWhileSpeaking);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true);

		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true, false);
	}

	/// <summary>Verifies that a microphone that was not asked to wait, because nothing is read aloud, is left as it was.</summary>
	[Fact]
	public async Task Without_read_aloud_a_dormant_microphone_is_left_dormant_by_a_typed_question()
	{
		var module = SetUpVoiceModule();
		var component = await RenderDormantAsync(WakeService(TimeSpan.Zero, readAloud: false));

		await TypeAndSendAsync(component, "Is it done?");

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
		module.Invocations["pause"].Should().BeEmpty();
	}

	private static async Task HearAsync(IRenderedComponent<PDChat> component, params string[] words)
		=> await SayAsync(component, words);

	private static async Task AssertNothingHeardAsync(IRenderedComponent<PDChat> component, FakeChatService service)
	{
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		service.Sent.Should().ContainSingle("only the question was sent");
	}

	private static async Task TypeAndSendAsync(IRenderedComponent<PDChat> component, string text)
	{
		await component.Find("textarea").InputAsync(new ChangeEventArgs { Value = text });
		await component.Find(".chat-input-accessories .chat-send").ClickAsync(new MouseEventArgs());
	}
}
