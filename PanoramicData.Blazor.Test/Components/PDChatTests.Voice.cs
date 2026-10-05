using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Voice Mode: offered only when the service supplies endpoints, off by default, and when on, a spoken question is
/// sent like a typed one and the first finished reply is read aloud.
/// </summary>
public partial class PDChatTests
{
	private const string VoiceModulePath = "./_content/PanoramicData.Blazor/js/pdchat-voice.js";
	private static readonly PDChatVoiceEndpoints _voiceEndpoints = new("/voice/listen", "/voice/speak");

	/// <summary>Verifies that a service without voice endpoints offers no Voice Mode at all.</summary>
	[Fact]
	public void Without_voice_endpoints_there_is_no_Voice_Mode_control()
		=> RenderChat(new FakeChatService()).FindAll(".pdchat-voice-toggle").Should().BeEmpty();

	/// <summary>Verifies that Voice Mode is off until the user turns it on, and its control says what it does.</summary>
	[Fact]
	public void Voice_Mode_is_offered_but_off_by_default()
	{
		var component = RenderChat(new FakeChatService { VoiceEndpoints = _voiceEndpoints });

		var toggle = component.Find(".pdchat-voice-toggle");
		toggle.TextContent.Should().Contain("Voice");
		toggle.GetAttribute("title").Should().Be("Turn Voice Mode on: ask out loud and hear the answer");
		toggle.GetAttribute("aria-pressed").Should().Be("false");
		component.FindAll(".pdchat-voice-status").Should().BeEmpty();
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
	}

	/// <summary>Verifies that a chat whose input is not permitted offers no Voice Mode either.</summary>
	[Fact]
	public void Voice_Mode_is_not_offered_where_typing_is_not_permitted()
	{
		var service = new FakeChatService { VoiceEndpoints = _voiceEndpoints };
		((IChatService)service).IsInputPermitted = false;

		RenderChat(service).FindAll(".pdchat-voice-toggle").Should().BeEmpty();
	}

	/// <summary>Verifies that turning Voice Mode on opens the host's listening endpoint and says how to use it.</summary>
	[Fact]
	public async Task Turning_Voice_Mode_on_starts_listening()
	{
		var module = SetUpVoiceModule();
		var component = RenderChat(new FakeChatService { VoiceEndpoints = _voiceEndpoints });

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		module.Invocations["start"].Should().ContainSingle().Which.Arguments[0].Should().Be("/voice/listen");
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Listening);
		component.Find(".pdchat-voice-toggle").GetAttribute("aria-pressed").Should().Be("true");
		component.Find(".pdchat-voice-status").TextContent.Trim().Should().Be("Listening. Ask your question, then pause.");
	}

	/// <summary>Verifies that a spoken question is sent as the user, as typed text is, and the microphone pauses.</summary>
	[Fact]
	public async Task A_spoken_question_is_sent_like_a_typed_one()
	{
		var module = SetUpVoiceModule();
		var service = new FakeChatService { VoiceEndpoints = _voiceEndpoints };
		var component = RenderChat(service);
		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("Is MS-26473 done?"));

		var sent = service.Sent.Should().ContainSingle().Subject;
		sent.Message.Should().Be("Is MS-26473 done?");
		sent.Sender.Should().BeSameAs(_user);
		module.Invocations["pause"].Should().ContainSingle().Which.Arguments[0].Should().Be(true);
		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Thinking);
	}

	/// <summary>Verifies that only the finished answer is read aloud, as plain text, and listening then resumes.</summary>
	[Fact]
	public async Task The_finished_answer_is_spoken_and_listening_resumes()
	{
		var module = SetUpVoiceModule();
		var service = new FakeChatService { VoiceEndpoints = _voiceEndpoints };
		var component = RenderChat(service);
		await component.Find(".pdchat-voice-toggle").ClickAsync(new());
		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("Is it done?"));

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
		var service = new FakeChatService { VoiceEndpoints = _voiceEndpoints };
		var component = RenderChat(service);
		await component.Find(".pdchat-voice-toggle").ClickAsync(new());
		await component.InvokeAsync(() => component.Instance.OnVoiceTurn("Is it done?"));

		await component.InvokeAsync(() => service.Receive(Message("Yes.")));
		await component.InvokeAsync(() => service.Receive(Message("An unrelated notification")));

		module.Invocations["speak"].Should().ContainSingle();
	}

	/// <summary>Verifies that with Voice Mode off, answers are never spoken.</summary>
	[Fact]
	public async Task With_Voice_Mode_off_nothing_is_spoken()
	{
		var module = SetUpVoiceModule();
		var service = new FakeChatService { VoiceEndpoints = _voiceEndpoints };
		var component = RenderChat(service);

		await component.InvokeAsync(() => service.Receive(Message("An answer")));

		module.Invocations["speak"].Should().BeEmpty();
	}

	/// <summary>Verifies that turning Voice Mode off releases the microphone and removes the status line.</summary>
	[Fact]
	public async Task Turning_Voice_Mode_off_stops_it()
	{
		var module = SetUpVoiceModule();
		var component = RenderChat(new FakeChatService { VoiceEndpoints = _voiceEndpoints });
		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

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
		var component = RenderChat(new FakeChatService { VoiceEndpoints = _voiceEndpoints });

		await component.Find(".pdchat-voice-toggle").ClickAsync(new());

		component.Instance.VoiceState.Should().Be(PDChatVoiceState.Off);
		component.Find(".pdchat-voice-status").TextContent.Should().Contain("microphone could not be opened");
	}

	private BunitJSModuleInterop SetUpVoiceModule()
	{
		var module = JSInterop.SetupModule(VoiceModulePath);
		module.Mode = JSRuntimeMode.Loose;
		return module;
	}
}
