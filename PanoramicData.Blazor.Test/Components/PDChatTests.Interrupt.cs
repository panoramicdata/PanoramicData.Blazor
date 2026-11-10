using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// MS-27777: with interrupt phrases set, the microphone stays open while an answer is read aloud, but what it hears goes
/// only to a matcher for those phrases. Hearing one stops the answer and starts listening; an answer that itself says one
/// is read half duplex so it cannot interrupt itself.
/// </summary>
public partial class PDChatTests
{
	private const string Merlin = "Merlin";

	private static FakeChatService InterruptService(bool readAloud = true)
	{
		var service = VoiceService(TimeSpan.Zero, readAloud);
		((IChatService)service).InterruptPhrases = [Merlin];
		return service;
	}

	private static List<object?> PauseCalls(BunitJSModuleInterop module)
		=> [.. module.Invocations["pause"].Select(call => call.Arguments[0])];

	// A spoken question, answered, so that the answer is playing and the microphone is as the host left it.
	private async Task<IRenderedComponent<PDChat>> RenderSpeakingAsync(FakeChatService service, string answer = "Yes.")
	{
		var component = await RenderListeningAsync(service);
		await AskAsync(component, service, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message(answer)));
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);
		return component;
	}

	/// <summary>Verifies that the interrupt phrases are off by default and can be set on a service that does not implement them.</summary>
	[Fact]
	public void Interrupt_phrases_are_off_by_default()
	{
		IChatService service = new FakeChatService();

		service.InterruptPhrases.Should().BeNull();

		service.InterruptPhrases = [Merlin];
		service.InterruptPhrases.Should().Equal(Merlin);
	}

	/// <summary>Verifies that without interrupt phrases the microphone stays paused while the answer plays, as before.</summary>
	[Fact]
	public async Task Without_interrupt_phrases_the_microphone_stays_paused_while_the_answer_plays()
	{
		var module = SetUpVoiceModule();
		var component = await RenderSpeakingAsync(VoiceService());

		await HearAsync(component, Merlin);

		PauseCalls(module).Should().Equal(true);
		module.Invocations["stopSpeaking"].Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);
	}

	/// <summary>Verifies that with interrupt phrases the microphone is open while an answer that does not say one plays.</summary>
	[Fact]
	public async Task With_interrupt_phrases_the_microphone_is_open_while_the_answer_plays()
	{
		var module = SetUpVoiceModule();

		await RenderSpeakingAsync(InterruptService());

		PauseCalls(module).Should().Equal(true, false);
	}

	/// <summary>Verifies that saying the phrase while an answer plays stops it and starts listening.</summary>
	[Fact]
	public async Task Saying_Merlin_while_an_answer_plays_stops_it_and_starts_listening()
	{
		var module = SetUpVoiceModule();
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service);

		await component.InvokeAsync(() => component.Instance.OnVoiceWord("Merlin,"));

		module.Invocations["stopSpeaking"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.Find(".pdchat-voice-status").TextContent.Trim().Should().Be("Listening. Ask your question, then pause.");
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		service.Sent.Should().ContainSingle();
	}

	/// <summary>Verifies that the pause after the interrupting word sends nothing, and the next utterance is dictated.</summary>
	[Fact]
	public async Task After_an_interruption_the_next_utterance_is_dictated()
	{
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service);

		await SayAsync(component, Merlin);
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		service.Sent.Should().ContainSingle("the interrupting utterance is not a question");

		await SayAsync(component, "What", "about", "tomorrow?");

		component.WaitForAssertion(() => service.Sent.Should().HaveCount(2), Patience);
		service.Sent[1].Message.Should().Be("What about tomorrow?");
	}

	/// <summary>Verifies that words in the same utterance after the phrase are discarded, and the next utterance is dictated.</summary>
	[Fact]
	public async Task The_rest_of_the_interrupting_utterance_is_discarded()
	{
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service);

		await component.InvokeAsync(() => component.Instance.OnVoiceWord("merlin stop it"));
		await component.InvokeAsync(() => component.Instance.OnVoiceWord("please"));
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("merlin stop it please"));
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);
		service.Sent.Should().ContainSingle();

		await component.InvokeAsync(() => component.Instance.OnVoiceWord("Hello"));

		component.Find("textarea").GetAttribute("value").Should().Be("Hello");
	}

	/// <summary>Verifies that interrupting leaves whatever was in the text box as it was, and sends nothing.</summary>
	[Fact]
	public async Task Interrupting_leaves_the_text_box_unchanged()
	{
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service);
		await component.Find("textarea").InputAsync(new ChangeEventArgs { Value = "A draft" });

		await SayAsync(component, Merlin);
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		component.Find("textarea").GetAttribute("value").Should().Be("A draft");
		service.Sent.Should().ContainSingle();
	}

	/// <summary>Verifies that a host that reports only pauses can interrupt, through the turn's text.</summary>
	[Fact]
	public async Task A_host_that_reports_only_pauses_can_interrupt()
	{
		var module = SetUpVoiceModule();
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service);

		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("Not now."));
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);
		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("Hey merlin, be quiet."));

		module.Invocations["stopSpeaking"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("What about tomorrow?"));
		component.WaitForAssertion(() => service.Sent.Should().HaveCount(2), Patience);
		service.Sent[1].Message.Should().Be("What about tomorrow?");
	}

	/// <summary>Verifies that other words heard while the answer plays are ignored: not typed, not sent, and it plays on.</summary>
	[Fact]
	public async Task Other_words_heard_while_the_answer_plays_are_ignored()
	{
		var module = SetUpVoiceModule();
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service);

		await SayAsync(component, "Hey", "DumbBot,", "delete", "everything");
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		service.Sent.Should().ContainSingle();
		module.Invocations["stopSpeaking"].Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);
	}

	/// <summary>Verifies that what was heard during playback, still being finished when it ends, is not dictated.</summary>
	[Fact]
	public async Task The_end_of_an_utterance_heard_while_the_answer_played_is_not_dictated()
	{
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service);

		await component.InvokeAsync(() => component.Instance.OnVoiceWord("and"));
		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());
		await component.InvokeAsync(() => component.Instance.OnVoiceWord("that"));
		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("and that"));
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		service.Sent.Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);

		await component.InvokeAsync(() => component.Instance.OnVoiceWord("Hello"));

		component.Find("textarea").GetAttribute("value").Should().Be("Hello");
	}

	/// <summary>Verifies that a phrase of several words interrupts only when all of its words are heard together.</summary>
	[Fact]
	public async Task A_phrase_of_several_words_interrupts_only_when_complete()
	{
		var service = VoiceService(TimeSpan.Zero);
		((IChatService)service).InterruptPhrases = ["Hey Merlin"];
		var component = await RenderSpeakingAsync(service);

		await SayAsync(component, Merlin);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);

		await SayAsync(component, "Hey", "MERLIN!");

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that an answer that says the phrase is read half duplex and cannot interrupt itself.</summary>
	[Theory]
	[InlineData("Merlin is done.")]
	[InlineData("Ask MERLIN again.")]
	[InlineData("It is Merlin's.")]
	[InlineData("The Merlin-based agent.")]
	public async Task An_answer_that_says_the_phrase_cannot_interrupt_itself(string answer)
	{
		var module = SetUpVoiceModule();
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service, answer);

		await SayAsync(component, Merlin);
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		PauseCalls(module).Should().Equal(true);
		module.Invocations["stopSpeaking"].Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
	}

	/// <summary>Verifies that the read-aloud control still stops an answer that cannot be interrupted by voice.</summary>
	[Fact]
	public async Task The_stop_control_still_stops_an_answer_that_says_the_phrase()
	{
		var module = SetUpVoiceModule();
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service, "Merlin is done.");

		await component.Find(".pdchat-read-aloud-toggle").ClickAsync(new());

		module.Invocations["stopSpeaking"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		PauseCalls(module).Should().Equal(true, false);
	}

	/// <summary>Verifies that the read-aloud control still stops an answer being listened through, and ends the listening.</summary>
	[Fact]
	public async Task The_stop_control_stops_an_answer_that_can_be_interrupted()
	{
		var module = SetUpVoiceModule();
		var service = InterruptService();
		var component = await RenderSpeakingAsync(service);

		await component.Find(".pdchat-read-aloud-toggle").ClickAsync(new());
		await SayAsync(component, "Is", "it", "done?");

		module.Invocations["stopSpeaking"].Should().ContainSingle("only the control stopped it");
		component.WaitForAssertion(() => service.Sent.Should().HaveCount(2), Patience);
	}

	/// <summary>Verifies that a question typed while dormant is interruptible, goes to listening, and ignores the wake phrase meanwhile.</summary>
	[Fact]
	public async Task A_dormant_microphone_hears_the_interrupt_phrase_but_not_the_wake_phrase_while_an_answer_plays()
	{
		var module = SetUpVoiceModule();
		var service = WakeService(TimeSpan.Zero);
		((IChatService)service).InterruptPhrases = [Merlin];
		var component = await RenderDormantAsync(service);
		await TypeAndSendAsync(component, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);

		await SayAsync(component, "Hey", "DumbBot,", "delete", "everything");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);

		await SayAsync(component, Merlin);

		module.Invocations["stopSpeaking"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		service.Sent.Should().ContainSingle();
	}

	/// <summary>Verifies that a dormant microphone that is not interrupted is dormant again once the answer has been spoken.</summary>
	[Fact]
	public async Task A_dormant_microphone_is_dormant_again_after_an_uninterrupted_answer()
	{
		var service = WakeService(TimeSpan.Zero);
		((IChatService)service).InterruptPhrases = [Merlin];
		var component = await RenderDormantAsync(service);
		await TypeAndSendAsync(component, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
	}

	/// <summary>Verifies that in Wake mode an interruption starts listening, and the exchange then ends dormant as usual.</summary>
	[Fact]
	public async Task In_Wake_mode_an_interruption_starts_listening_and_the_exchange_ends_dormant()
	{
		var service = WakeService(TimeSpan.Zero);
		((IChatService)service).InterruptPhrases = [Merlin];
		var component = await RenderInModeAsync(service, PDChatVoiceMode.Wake);
		await TypeAndSendAsync(component, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		await SayAsync(component, Merlin);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		await SayAsync(component, "Is", "it", "tomorrow?");
		component.WaitForAssertion(() => service.Sent.Should().HaveCount(2), Patience);

		await component.InvokeAsync(() => service.Receive(Message("No.")));
		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
	}

	/// <summary>Verifies that turning Voice Mode off mid-answer ends the listening, so nothing is matched afterwards.</summary>
	[Fact]
	public async Task Turning_Voice_Mode_off_mid_answer_ends_the_interrupt_listening()
	{
		var module = SetUpVoiceModule();
		var component = await RenderSpeakingAsync(InterruptService());

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());
		await SayAsync(component, Merlin);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		module.Invocations["stopSpeaking"].Should().BeEmpty("only stop was called, not an interruption");
	}

	/// <summary>Verifies that with Voice Mode off, interrupt phrases change nothing: an answer is read with no microphone.</summary>
	[Fact]
	public async Task With_Voice_Mode_off_interrupt_phrases_change_nothing()
	{
		var module = SetUpVoiceModule();
		var service = InterruptService();
		var component = RenderChat(service);

		await TypeAndSendAsync(component, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		module.Invocations["speak"].Should().ContainSingle();
		module.Invocations["start"].Should().BeEmpty();
		module.Invocations["pause"].Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
	}
}
