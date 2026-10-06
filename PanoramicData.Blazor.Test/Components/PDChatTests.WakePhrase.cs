using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// The wake phrase: with wake phrases set, a microphone that hears nothing for the idle timeout goes dormant, types and
/// sends nothing, and listens again only once a wake phrase is heard.
/// </summary>
public partial class PDChatTests
{
	private static readonly TimeSpan _briefIdle = TimeSpan.FromMilliseconds(50);

	/// <summary>Verifies that wake phrases are off and the idle timeout is ten seconds by default.</summary>
	[Fact]
	public void Wake_phrases_are_off_by_default()
	{
		IChatService service = new FakeChatService();

		service.WakePhrases.Should().BeNull();
		service.VoiceIdleTimeout.Should().Be(TimeSpan.FromSeconds(10));
	}

	/// <summary>Verifies that with wake phrases set, listening without a word goes dormant and the microphone stays on.</summary>
	[Fact]
	public async Task With_wake_phrases_an_idle_microphone_goes_dormant()
	{
		var module = SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).VoiceIdleTimeout = _briefIdle;

		var component = await RenderListeningAsync(service);

		component.WaitForAssertion(() => component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant), Patience);
		module.Invocations["stop"].Should().BeEmpty();
		component.Find(".pdchat-voice-toggle").GetAttribute("aria-pressed").Should().Be("true");
	}

	/// <summary>Verifies that without wake phrases the microphone never goes dormant.</summary>
	[Fact]
	public async Task Without_wake_phrases_the_microphone_never_goes_dormant()
	{
		SetUpVoiceModule();
		var service = VoiceService(TimeSpan.FromMinutes(10));
		((IChatService)service).VoiceIdleTimeout = _briefIdle;

		var component = await RenderListeningAsync(service);
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that the status line and the microphone control say which phrase is awaited.</summary>
	[Fact]
	public async Task A_dormant_microphone_says_what_it_is_waiting_for()
	{
		SetUpVoiceModule();
		var component = await RenderDormantAsync(WakeService());

		component.Find(".pdchat-voice-status").TextContent.Trim().Should().Be("Waiting for \"Hey DumbBot\"…");
		component.Find(".pdchat-voice-status").ClassList.Should().Contain("pdchat-voice-dormant");
		var toggle = component.Find(".pdchat-voice-toggle");
		toggle.GetAttribute("title").Should().Be("Voice: waiting for \"Hey DumbBot\". Press to stop listening");
		toggle.GetAttribute("aria-label").Should().Be(toggle.GetAttribute("title"));
	}

	/// <summary>Verifies that while dormant, words are not typed and a pause never sends.</summary>
	[Fact]
	public async Task Dormant_words_are_not_typed_and_a_dormant_turn_never_sends()
	{
		SetUpVoiceModule();
		var service = WakeService(TimeSpan.Zero);
		var component = await RenderDormantAsync(service);

		await SayAsync(component, "What", "time", "is", "it?");
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		service.Sent.Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
	}

	/// <summary>Verifies that the wake phrase, in any case and with punctuation, wakes it and only the words after it are sent.</summary>
	[Fact]
	public async Task The_wake_phrase_wakes_it_and_only_the_words_after_it_are_sent()
	{
		SetUpVoiceModule();
		var service = WakeService(TimeSpan.Zero, readAloud: false);
		var component = await RenderDormantAsync(service);

		await SayAsync(component, "Ignore", "this.", "HEY", "dumbbot,", "is", "it", "done?");

		component.WaitForAssertion(() => service.Sent.Should().ContainSingle().Which.Message.Should().Be("is it done?"), Patience);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that a phrase of several words wakes it whether heard word by word or in one piece.</summary>
	[Theory]
	[InlineData("OK", "Dumb", "Bot.", "Is", "it", "done?")]
	[InlineData("OK Dumb Bot. Is", "it", "done?")]
	public async Task A_phrase_of_several_words_wakes_it(params string[] words)
	{
		SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).WakePhrases = ["Hey DumbBot", "OK Dumb Bot"];
		var component = await RenderDormantAsync(service);

		foreach (var word in words)
		{
			await component.InvokeAsync(() => component.Instance.OnVoiceWord(word));
		}

		component.Find("textarea").GetAttribute("value").Should().Be("Is it done?");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that the words of a phrase heard apart, or only in part, do not wake it.</summary>
	[Theory]
	[InlineData("Hey", "there", "DumbBot")]
	[InlineData("Hey")]
	[InlineData("DumbBot")]
	public async Task A_partial_phrase_does_not_wake_it(params string[] words)
	{
		SetUpVoiceModule();
		var component = await RenderDormantAsync(WakeService());

		await SayAsync(component, words);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
	}

	/// <summary>Verifies that a wake phrase followed by a pause types nothing, so the phrase itself is never sent.</summary>
	[Fact]
	public async Task The_wake_phrase_alone_is_never_typed()
	{
		SetUpVoiceModule();
		var service = WakeService(TimeSpan.Zero);
		var component = await RenderDormantAsync(service);

		await SayAsync(component, "Hey", "DumbBot");
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty();
		service.Sent.Should().BeEmpty();
	}

	/// <summary>Verifies that a host reporting only pauses can wake it, and the words after the phrase are typed.</summary>
	[Fact]
	public async Task A_host_that_reports_only_pauses_can_wake_it()
	{
		SetUpVoiceModule();
		var component = await RenderDormantAsync(WakeService());

		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("Nothing to see."));
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant);

		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("Hey DumbBot, is it done?"));

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.Find("textarea").GetAttribute("value").Should().Be("is it done?");
	}

	/// <summary>Verifies that each word restarts the idle timer, so a steady speaker never goes dormant.</summary>
	[Fact]
	public async Task Each_word_restarts_the_idle_timer()
	{
		SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).VoiceIdleTimeout = TimeSpan.FromMilliseconds(400);
		var component = await RenderListeningAsync(service);

		for (var word = 0; word < 6; word++)
		{
			await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
			await component.InvokeAsync(() => component.Instance.OnVoiceWord("more"));
		}

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.WaitForAssertion(() => component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant), Patience);
	}

	/// <summary>Verifies that it never goes dormant while the user is editing the text box, and does once they stop.</summary>
	[Fact]
	public async Task It_does_not_go_dormant_while_the_text_box_has_focus()
	{
		SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).VoiceIdleTimeout = _briefIdle;
		var component = RenderChat(service);
		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);

		await component.Find("textarea").BlurAsync(new FocusEventArgs());

		component.WaitForAssertion(() => component.Instance.VoiceState.Should().Be(PDChatVoiceState.Dormant), Patience);
	}

	/// <summary>Verifies that turning Voice off cancels the idle timer, so it stays off rather than going dormant.</summary>
	[Fact]
	public async Task Turning_Voice_off_cancels_the_idle_timer()
	{
		SetUpVoiceModule();
		var service = WakeService();
		((IChatService)service).VoiceIdleTimeout = TimeSpan.FromMilliseconds(150);
		var component = await RenderListeningAsync(service);

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		component.FindAll(".pdchat-voice-status").Should().BeEmpty();
	}

	/// <summary>Verifies that pressing the microphone while dormant turns Voice off.</summary>
	[Fact]
	public async Task Pressing_the_microphone_while_dormant_turns_Voice_off()
	{
		var module = SetUpVoiceModule();
		var component = await RenderDormantAsync(WakeService());

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		module.Invocations["stop"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
	}

	private static FakeChatService WakeService(TimeSpan? autoSendDelay = null, bool readAloud = true)
	{
		var service = VoiceService(autoSendDelay ?? TimeSpan.FromMinutes(10), readAloud);
		((IChatService)service).WakePhrases = ["Hey DumbBot"];
		return service;
	}

	// Once dormant, the idle timeout is lengthened so a woken microphone does not doze off again mid-test.
	private async Task<IRenderedComponent<PDChat>> RenderDormantAsync(FakeChatService service)
	{
		((IChatService)service).VoiceIdleTimeout = _briefIdle;
		var component = await RenderListeningAsync(service);

		// Wait for the render, not just the state, so tests reading the markup see the dormant status.
		component.WaitForAssertion(() => component.Find(".pdchat-voice-status").ClassList.Should().Contain("pdchat-voice-dormant"), Patience);
		((IChatService)service).VoiceIdleTimeout = TimeSpan.FromMinutes(10);
		return component;
	}
}
