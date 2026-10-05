using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Voice Mode: offered in the input area only when the service supplies endpoints, off by default. When on, words are
/// dictated into the text box, sent after a pause unless the user is editing, and the first finished reply is read aloud.
/// </summary>
public partial class PDChatTests
{
	private const string VoiceModulePath = "./_content/PanoramicData.Blazor/js/pdchat-voice.js";
	private static readonly PDChatVoiceEndpoints _voiceEndpoints = new("/voice/listen", "/voice/speak");

	/// <summary>Long enough that a send which should not happen would have.</summary>
	private static readonly TimeSpan _settle = TimeSpan.FromMilliseconds(400);

	/// <summary>Verifies that a service without voice endpoints offers no Voice Mode at all.</summary>
	[Fact]
	public void Without_voice_endpoints_there_is_no_Voice_Mode_control()
		=> RenderChat(new FakeChatService()).FindAll(".pdchat-voice-toggle").Should().BeEmpty();

	/// <summary>Verifies that Voice Mode is off until the user turns it on, and its control says what it does.</summary>
	[Fact]
	public void Voice_Mode_is_offered_but_off_by_default()
	{
		var component = RenderChat(VoiceService());

		var toggle = component.Find(".pdchat-voice-toggle");
		toggle.GetAttribute("aria-label").Should().Be("Voice: speak instead of typing");
		toggle.GetAttribute("title").Should().Be("Voice: speak instead of typing");
		toggle.GetAttribute("aria-pressed").Should().Be("false");
		component.FindAll(".pdchat-voice-status").Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
	}

	/// <summary>Verifies that the Voice control sits in the input area beside Send, not in the header.</summary>
	[Fact]
	public void The_Voice_control_is_in_the_input_area()
	{
		var component = RenderChat(VoiceService());

		component.FindAll(".pdchat-header .pdchat-voice-toggle").Should().BeEmpty();
		component.FindAll(".chat-input-accessories .pdchat-voice-toggle").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that the microphone is not asked for until Voice is first turned on: rendering the chat, its input area
	/// and the agent picker loads no voice module and starts nothing.
	/// </summary>
	[Fact]
	public async Task The_microphone_is_not_requested_until_Voice_is_turned_on()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		service.Agents = [new("merlin", "Merlin"), new("alice", "Alice")];
		var component = RenderChat(service);
		component.Find(".pdchat-voice-toggle");
		await component.Find(".pdchat-agent-picker select").ChangeAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "alice" });

		VoiceModuleImports().Should().BeEmpty();
		module.Invocations.Should().BeEmpty();

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		VoiceModuleImports().Should().ContainSingle();
		module.Invocations["start"].Should().ContainSingle();
	}

	private IEnumerable<JSRuntimeInvocation> VoiceModuleImports()
		=> JSInterop.Invocations["import"].Where(call => VoiceModulePath.Equals(call.Arguments[0]));

	/// <summary>Verifies that a chat whose input is not permitted offers no Voice Mode either.</summary>
	[Fact]
	public void Voice_Mode_is_not_offered_where_typing_is_not_permitted()
	{
		var service = VoiceService();
		((IChatService)service).IsInputPermitted = false;

		RenderChat(service).FindAll(".pdchat-voice-toggle").Should().BeEmpty();
	}

	/// <summary>Verifies that turning Voice Mode on opens the host's listening endpoint and says how to use it.</summary>
	[Fact]
	public async Task Turning_Voice_Mode_on_starts_listening()
	{
		var module = SetUpVoiceModule();
		var component = RenderChat(VoiceService());

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		module.Invocations["start"].Should().ContainSingle().Which.Arguments[0].Should().Be("/voice/listen");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.Find(".pdchat-voice-toggle").GetAttribute("aria-pressed").Should().Be("true");
		component.Find(".pdchat-voice-toggle").GetAttribute("title").Should().Be("Voice: stop listening");
		component.Find(".pdchat-voice-status").TextContent.Trim().Should().Be("Listening. Ask your question, then pause.");
	}

	/// <summary>Verifies that the auto-send delay defaults to one second and can be set on a service that does not implement it.</summary>
	[Fact]
	public void The_auto_send_delay_defaults_to_one_second()
	{
		IChatService service = new FakeChatService();

		service.VoiceAutoSendDelay.Should().Be(TimeSpan.FromMilliseconds(1000));

		service.VoiceAutoSendDelay = TimeSpan.FromMilliseconds(250);
		service.VoiceAutoSendDelay.Should().Be(TimeSpan.FromMilliseconds(250));
	}

	/// <summary>Verifies that recognised words are dictated into the text box, and nothing is sent as they arrive.</summary>
	[Fact]
	public async Task Dictated_words_fill_the_text_box()
	{
		SetUpVoiceModule();
		var service = VoiceService(TimeSpan.FromMinutes(10));
		var component = await RenderListeningAsync(service);

		await component.InvokeAsync(() => component.Instance.OnVoiceWord("Is"));
		await component.InvokeAsync(() => component.Instance.OnVoiceWord("MS-26473"));
		await component.InvokeAsync(() => component.Instance.OnVoiceWord("done?"));

		component.Find("textarea").GetAttribute("value").Should().Be("Is MS-26473 done?");
		service.Sent.Should().BeEmpty();
	}

	/// <summary>Verifies that the pause does not repeat words already dictated, and a host that reports only pauses still fills the box.</summary>
	[Fact]
	public async Task A_pause_adds_only_what_was_not_dictated()
	{
		SetUpVoiceModule();
		var component = await RenderListeningAsync(VoiceService(TimeSpan.FromMinutes(10)));

		await SayAsync(component, "Is", "it", "done?");
		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("And the other one?"));

		component.Find("textarea").GetAttribute("value").Should().Be("Is it done? And the other one?");
	}

	/// <summary>Verifies that after a pause, with the text box not focused, the text is sent as the user once the delay has passed.</summary>
	[Fact]
	public async Task Dictation_is_sent_after_the_delay_when_the_text_box_is_not_focused()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService(TimeSpan.FromMilliseconds(150));
		var component = await RenderListeningAsync(service);

		await SayAsync(component, "Is", "it", "done?");
		service.Sent.Should().BeEmpty("the delay has not passed");

		component.WaitForAssertion(() => service.Sent.Should().ContainSingle(), Patience);
		var sent = service.Sent[0];
		sent.Message.Should().Be("Is it done?");
		sent.Sender.Should().BeSameAs(_user);
		module.Invocations["pause"].Should().ContainSingle().Which.Arguments[0].Should().Be(true);
		component.WaitForAssertion(() => component.Find("textarea").GetAttribute("value").Should().BeNullOrEmpty(), Patience);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Thinking);
	}

	/// <summary>Verifies that while the user is editing the text box, a pause never sends; the user presses Send.</summary>
	[Fact]
	public async Task Dictation_is_never_auto_sent_while_the_text_box_has_focus()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = await RenderListeningAsync(service);
		await component.Find("textarea").FocusAsync(new FocusEventArgs());

		await SayAsync(component, "Is", "it", "done?");
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		service.Sent.Should().BeEmpty();
		component.Find(".pdchat-voice-status").TextContent.Trim().Should().Be("Listening. You are editing, so press Send when ready.");

		await component.Find(".chat-input-accessories .chat-send").ClickAsync(new MouseEventArgs());

		service.Sent.Should().ContainSingle().Which.Message.Should().Be("Is it done?");
		module.Invocations["pause"].Should().ContainSingle().Which.Arguments[0].Should().Be(true);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Thinking);
	}

	/// <summary>Verifies that focusing the text box while a send is pending cancels it.</summary>
	[Fact]
	public async Task Focusing_the_text_box_cancels_a_pending_send()
	{
		SetUpVoiceModule();
		var service = VoiceService(TimeSpan.FromMilliseconds(200));
		var component = await RenderListeningAsync(service);

		await SayAsync(component, "Is", "it", "done?");
		await component.Find("textarea").FocusAsync(new FocusEventArgs());
		await component.Find("textarea").BlurAsync(new FocusEventArgs());
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		service.Sent.Should().BeEmpty();
	}

	/// <summary>Verifies that speaking again before the delay has passed cancels the pending send until the next pause.</summary>
	[Fact]
	public async Task Speaking_again_cancels_a_pending_send()
	{
		SetUpVoiceModule();
		var service = VoiceService(TimeSpan.FromMilliseconds(200));
		var component = await RenderListeningAsync(service);

		await SayAsync(component, "Is", "it");
		await component.InvokeAsync(() => component.Instance.OnVoiceWord("done?"));
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);
		service.Sent.Should().BeEmpty();

		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("done?"));

		component.WaitForAssertion(() => service.Sent.Should().ContainSingle().Which.Message.Should().Be("Is it done?"), Patience);
	}

	/// <summary>Verifies that turning Voice Mode off cancels a pending send.</summary>
	[Fact]
	public async Task Turning_Voice_Mode_off_cancels_a_pending_send()
	{
		SetUpVoiceModule();
		var service = VoiceService(TimeSpan.FromMilliseconds(200));
		var component = await RenderListeningAsync(service);

		await SayAsync(component, "Is", "it", "done?");
		await component.Find(".pdchat-voice-toggle").ClickAsync(new());
		await Task.Delay(_settle, Xunit.TestContext.Current.CancellationToken);

		service.Sent.Should().BeEmpty();
		component.Find("textarea").GetAttribute("value").Should().Be("Is it done?", "dictation stays for the user to send or edit");
	}

	/// <summary>Verifies that only the finished answer is read aloud, as plain text, and listening then resumes.</summary>
	[Fact]
	public async Task The_finished_answer_is_spoken_and_listening_resumes()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = await RenderListeningAsync(service);
		await AskAsync(component, service, "Is it done?");

		var typing = Message("Looking it up", MessageType.Typing);
		await component.InvokeAsync(() => service.Receive(typing));
		module.Invocations["speak"].Should().BeEmpty("a typing placeholder is not the answer");

		var answer = Message("<p>Yes, it is <b>done</b>.</p>");
		answer.IsMessageHtml = true;
		await component.InvokeAsync(() => service.Receive(answer));

		var speak = module.Invocations["speak"].Should().ContainSingle().Subject;
		speak.Arguments[0].Should().Be("/voice/speak");
		((string)speak.Arguments[1]!).Trim().Should().Be("Yes, it is  done .");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);

		await component.InvokeAsync(() => component.Instance.OnVoiceSpoken());

		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true, false);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that only the first finished reply to a spoken question is spoken, never later ones.</summary>
	[Fact]
	public async Task Only_the_first_reply_to_a_spoken_question_is_spoken()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = await RenderListeningAsync(service);
		await AskAsync(component, service, "Is it done?");

		await component.InvokeAsync(() => service.Receive(Message("Yes.")));
		await component.InvokeAsync(() => service.Receive(Message("An unrelated notification")));

		module.Invocations["speak"].Should().ContainSingle();
	}

	/// <summary>Verifies that with Voice Mode off, answers are never spoken.</summary>
	[Fact]
	public async Task With_Voice_Mode_off_nothing_is_spoken()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("An answer")));

		module.Invocations["speak"].Should().BeEmpty();
	}

	/// <summary>Verifies that turning Voice Mode off releases the microphone and removes the status line.</summary>
	[Fact]
	public async Task Turning_Voice_Mode_off_stops_it()
	{
		var module = SetUpVoiceModule();
		var component = await RenderListeningAsync(VoiceService());

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		module.Invocations["stop"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		component.FindAll(".pdchat-voice-status").Should().BeEmpty();
	}

	/// <summary>Verifies that a refused microphone leaves Voice Mode off and says what to check.</summary>
	[Fact]
	public async Task A_refused_microphone_is_explained()
	{
		var module = SetUpVoiceModule();
		_ = module.SetupVoid("start", _ => true).SetException(new JSException("NotAllowedError"));
		var component = RenderChat(VoiceService());

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		component.Find(".pdchat-voice-status").TextContent.Should().Contain("microphone could not be opened");
	}

	/// <summary>Verifies that answers are not read aloud by default: dictation is sent and the microphone keeps listening.</summary>
	[Fact]
	public async Task Without_read_aloud_dictation_is_sent_and_nothing_is_spoken()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService(readAloud: false);
		var component = await RenderListeningAsync(service);
		component.Find(".pdchat-read-aloud-toggle").GetAttribute("aria-pressed").Should().Be("false");

		await AskAsync(component, service, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		module.Invocations["speak"].Should().BeEmpty();
		module.Invocations["pause"].Should().BeEmpty("the microphone only pauses while an answer is to be spoken");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that read-aloud works without the microphone: the answer to a typed message is spoken.</summary>
	[Fact]
	public async Task With_read_aloud_and_no_microphone_a_typed_question_is_answered_aloud()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService(readAloud: false);
		var component = RenderChat(service);
		await component.Find(".pdchat-read-aloud-toggle").ClickAsync(new());
		((IChatService)service).IsReadAloudEnabled.Should().BeTrue();

		await component.Find("textarea").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "Is it done?" });
		await component.Find(".chat-input-accessories .chat-send").ClickAsync(new MouseEventArgs());
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));

		module.Invocations["speak"].Should().ContainSingle().Which.Arguments[1].Should().Be("Yes.");
		module.Invocations["start"].Should().BeEmpty("reading aloud never opens the microphone");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
	}

	/// <summary>Verifies that turning read-aloud off mid-answer stops speaking and the microphone listens again.</summary>
	[Fact]
	public async Task Turning_read_aloud_off_mid_answer_stops_it_and_listening_resumes()
	{
		var module = SetUpVoiceModule();
		var service = VoiceService();
		var component = await RenderListeningAsync(service);
		await AskAsync(component, service, "Is it done?");
		await component.InvokeAsync(() => service.Receive(Message("Yes.")));
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Speaking);

		await component.Find(".pdchat-read-aloud-toggle").ClickAsync(new());

		module.Invocations["stopSpeaking"].Should().ContainSingle();
		module.Invocations["pause"].Select(call => call.Arguments[0]).Should().Equal(true, false);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	/// <summary>Verifies that read-aloud and notification sounds are separate choices, so all four combinations exist.</summary>
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Read_aloud_and_notification_sounds_are_independent(bool muted, bool readAloud)
	{
		var service = VoiceService(readAloud: false);
		var component = RenderChat(service);

		if (muted)
		{
			await component.Find(".pdchat-mute-toggle").ClickAsync(new());
		}

		if (readAloud)
		{
			await component.Find(".pdchat-read-aloud-toggle").ClickAsync(new());
		}

		service.IsMuted.Should().Be(muted);
		((IChatService)service).IsReadAloudEnabled.Should().Be(readAloud);
		component.Find(".pdchat-mute-toggle").TextContent.Should().Be(muted ? "🔕" : "🔔");
		component.Find(".pdchat-read-aloud-toggle").GetAttribute("aria-pressed").Should().Be(readAloud ? "true" : "false");
	}

	/// <summary>Verifies that the microphone control is an icon alone, named for screen readers by its label.</summary>
	[Fact]
	public void The_microphone_control_is_an_icon_only()
	{
		var toggle = RenderChat(VoiceService()).Find(".pdchat-voice-toggle");

		toggle.TextContent.Trim().Should().Be("🎙️");
		toggle.GetAttribute("aria-label").Should().Be("Voice: speak instead of typing");
	}

	/// <summary>Verifies that the toolbar sits above the input, leaving the text box and Send the whole row.</summary>
	[Fact]
	public void The_toolbar_sits_above_a_full_width_input_row()
	{
		var component = RenderChat(VoiceService());

		var toolbar = component.Find(".chat-input-accessories");
		toolbar.NextElementSibling!.ClassList.Should().Contain("chat-input-container");
		component.Find(".chat-input-container").Children.Select(child => child.LocalName).Should().Equal("textarea");
		toolbar.LastElementChild!.ClassList.Should().Contain("chat-send");
		component.FindAll(".pdchat-header .pdchat-mute-toggle, .pdchat-header-btn[title^='Notification']").Should().BeEmpty();
		toolbar.QuerySelector(".pdchat-mute-toggle").Should().NotBeNull();
	}

	/// <summary>Verifies that notification sounds can still be switched where typing is not permitted.</summary>
	[Fact]
	public void Notification_sounds_stay_reachable_where_typing_is_not_permitted()
	{
		var service = VoiceService();
		((IChatService)service).IsInputPermitted = false;
		var component = RenderChat(service);

		component.FindAll(".chat-input-accessories .pdchat-mute-toggle").Should().ContainSingle();
		component.FindAll(".pdchat-voice-toggle, .pdchat-read-aloud-toggle").Should().BeEmpty();
	}

	private static FakeChatService VoiceService(TimeSpan? autoSendDelay = null, bool readAloud = true)
	{
		var service = new FakeChatService { VoiceEndpoints = _voiceEndpoints };
		((IChatService)service).VoiceAutoSendDelay = autoSendDelay ?? TimeSpan.Zero;
		((IChatService)service).IsReadAloudEnabled = readAloud;
		return service;
	}

	private async Task<IRenderedComponent<PDChat>> RenderListeningAsync(FakeChatService service)
	{
		var component = RenderChat(service);
		await component.Find(".pdchat-voice-toggle").ClickAsync(new());
		return component;
	}

	private static async Task SayAsync(IRenderedComponent<PDChat> component, params string[] words)
	{
		foreach (var word in words)
		{
			await component.InvokeAsync(() => component.Instance.OnVoiceWord(word));
		}

		await component.InvokeAsync(() => component.Instance.OnVoiceTurn(string.Join(' ', words)));
	}

	private static async Task AskAsync(IRenderedComponent<PDChat> component, FakeChatService service, string question)
	{
		await SayAsync(component, question.Split(' '));
		component.WaitForAssertion(() => service.Sent.Should().ContainSingle(), Patience);
	}

	/// <summary>Verifies that endpoints naming their own module have Voice Mode driven by it, not the standard one.</summary>
	[Fact]
	public async Task Simulated_Voice_Mode_uses_its_own_module()
	{
		var simulated = PDChatVoiceEndpoints.Simulated;
		var module = JSInterop.SetupModule(simulated.ModulePath!);
		module.Mode = JSRuntimeMode.Loose;
		var service = VoiceService();
		service.VoiceEndpoints = simulated;
		var component = RenderChat(service);

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		VoiceModuleImports().Should().BeEmpty();
		module.Invocations["start"].Should().ContainSingle();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
	}

	private BunitJSModuleInterop SetUpVoiceModule()
	{
		var module = JSInterop.SetupModule(VoiceModulePath);
		module.Mode = JSRuntimeMode.Loose;
		return module;
	}
}
