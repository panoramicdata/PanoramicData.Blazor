using System.Net;

namespace PanoramicData.Blazor;

/// <summary>Where <see cref="PDChat"/>'s Voice Mode is in a spoken exchange.</summary>
public enum PDChatVoiceState
{
	/// <summary>Voice Mode is off.</summary>
	Off,

	/// <summary>The microphone is being opened.</summary>
	Starting,

	/// <summary>Waiting for the user to speak.</summary>
	Listening,

	/// <summary>The question has been sent and the answer is awaited.</summary>
	Thinking,

	/// <summary>The answer is being spoken.</summary>
	Speaking,
}

/// <summary>
/// Voice Mode for <see cref="PDChat"/>: the user speaks a question, pauses, and hears the answer. Off by default, and
/// offered only when the chat service supplies <see cref="IChatService.VoiceEndpoints"/>.
/// </summary>
/// <remarks>
/// A spoken question goes through the same path as a typed one, so the question and the written answer are both in
/// the transcript. Only the first finished reply to a spoken question is read aloud.
/// </remarks>
public partial class PDChat
{
	private const string _voiceModulePath = "./_content/PanoramicData.Blazor/js/pdchat-voice.js";

	private IJSObjectReference? _voiceModule;
	private DotNetObjectReference<PDChat>? _voiceReference;
	private bool _isAwaitingSpokenAnswer;
	private string _voiceHeard = string.Empty;

	/// <summary>Gets where Voice Mode is in a spoken exchange.</summary>
	public PDChatVoiceState VoiceState { get; private set; } = PDChatVoiceState.Off;

	/// <summary>Gets the last problem Voice Mode reported, shown in place of its status.</summary>
	public string? VoiceError { get; private set; }

	private bool IsVoiceModeOffered => ChatService.VoiceEndpoints is not null && ChatService.IsInputPermitted;

	private bool IsVoiceModeOn => VoiceState != PDChatVoiceState.Off;

	private string VoiceButtonTitle => IsVoiceModeOn
		? "Turn Voice Mode off"
		: "Turn Voice Mode on: ask out loud and hear the answer";

	private string VoiceStatusCssClass => $"pdchat-voice-status pdchat-voice-{VoiceState.ToString().ToLowerInvariant()}" + (VoiceError is null ? string.Empty : " pdchat-voice-error");

	private string VoiceStatusText => VoiceError ?? VoiceState switch
	{
		PDChatVoiceState.Starting => "Opening the microphone…",
		PDChatVoiceState.Listening => _voiceHeard.Length > 0 ? _voiceHeard : "Listening. Ask your question, then pause.",
		PDChatVoiceState.Thinking => "Thinking…",
		PDChatVoiceState.Speaking => "Speaking. Turn Voice Mode off to stop.",
		_ => string.Empty,
	};

	private async Task ToggleVoiceModeAsync()
	{
		if (IsVoiceModeOn)
		{
			await StopVoiceModeAsync();
			return;
		}

		if (ChatService.VoiceEndpoints is not { } endpoints)
		{
			return;
		}

		VoiceError = null;
		VoiceState = PDChatVoiceState.Starting;
		try
		{
			_voiceModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", _voiceModulePath);
			_voiceReference ??= DotNetObjectReference.Create(this);
			await _voiceModule.InvokeVoidAsync("start", endpoints.ListenUrl, _voiceReference);
			VoiceState = PDChatVoiceState.Listening;
		}
		catch (JSException)
		{
			VoiceState = PDChatVoiceState.Off;
			VoiceError = "The microphone could not be opened. Check that this site may use it.";
		}
	}

	private async Task StopVoiceModeAsync()
	{
		VoiceState = PDChatVoiceState.Off;
		_isAwaitingSpokenAnswer = false;
		_voiceHeard = string.Empty;
		if (_voiceModule is not null)
		{
			await _voiceModule.InvokeVoidAsync("stop");
		}
	}

	/// <summary>Called by the voice module as words are recognised.</summary>
	/// <param name="text">The word just heard.</param>
	[JSInvokable]
	public Task OnVoiceWord(string text)
	{
		_voiceHeard = $"{_voiceHeard} {text}".Trim();
		return InvokeAsync(StateHasChanged);
	}

	/// <summary>Called by the voice module when the speaker has finished; sends what they said as a question.</summary>
	/// <param name="text">The whole question.</param>
	[JSInvokable]
	public async Task OnVoiceTurn(string text)
	{
		if (VoiceState != PDChatVoiceState.Listening || string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		_voiceHeard = string.Empty;
		_currentInput = text;
		VoiceState = PDChatVoiceState.Thinking;
		_isAwaitingSpokenAnswer = true;

		// Half duplex: while Merlin thinks and speaks, the microphone sends nothing, so it never hears itself.
		await (_voiceModule?.InvokeVoidAsync("pause", true) ?? ValueTask.CompletedTask);
		await SendCurrentMessageAsync();
		await InvokeAsync(StateHasChanged);
	}

	/// <summary>Called by the voice module when speech recognition reports a problem.</summary>
	/// <param name="text">What went wrong, for the user.</param>
	[JSInvokable]
	public Task OnVoiceError(string text)
	{
		VoiceError = text;
		return InvokeAsync(StateHasChanged);
	}

	/// <summary>Called by the voice module when the listening connection has closed.</summary>
	[JSInvokable]
	public Task OnVoiceClosed()
	{
		VoiceState = PDChatVoiceState.Off;
		_isAwaitingSpokenAnswer = false;
		VoiceError ??= "Voice Mode stopped: the connection closed.";
		return InvokeAsync(StateHasChanged);
	}

	/// <summary>Reads the first finished reply to a spoken question aloud, then listens again.</summary>
	private async Task SpeakAnswerIfAwaitedAsync(ChatMessage message)
	{
		if (!_isAwaitingSpokenAnswer || message.Sender.IsUser || message.Type == MessageType.Typing
			|| ChatService.VoiceEndpoints is not { } endpoints || _voiceModule is null)
		{
			return;
		}

		_isAwaitingSpokenAnswer = false;
		VoiceState = PDChatVoiceState.Speaking;
		await InvokeAsync(StateHasChanged);

		await _voiceModule.InvokeVoidAsync("speak", endpoints.SpeakUrl, ToSpeakableText(message));

		if (VoiceState == PDChatVoiceState.Speaking)
		{
			VoiceState = PDChatVoiceState.Listening;
			await _voiceModule.InvokeVoidAsync("pause", false);
			await InvokeAsync(StateHasChanged);
		}
	}

	// The host turns the text into speech; this only removes HTML markup, which would otherwise be read out.
	private static string ToSpeakableText(ChatMessage message)
		=> message.IsMessageHtml ? WebUtility.HtmlDecode(HtmlTag().Replace(message.Message, " ")) : message.Message;

	[GeneratedRegex("<[^>]+>")]
	private static partial Regex HtmlTag();

	private async ValueTask DisposeVoiceAsync()
	{
		if (_voiceModule is not null)
		{
			try
			{
				await _voiceModule.InvokeVoidAsync("stop");
				await _voiceModule.DisposeAsync();
			}
			catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
			{
				// The circuit is going away; the browser releases the microphone with the page.
			}
		}

		_voiceReference?.Dispose();
	}
}
