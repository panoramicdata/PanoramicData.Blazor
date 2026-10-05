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
/// Voice for <see cref="PDChat"/>: two independent choices, both off by default and offered only when the chat service
/// supplies <see cref="IChatService.VoiceEndpoints"/>. Dictation (the microphone) types into the text box; read-aloud
/// (<see cref="IChatService.IsReadAloudEnabled"/>) speaks the answer to each message the user sends.
/// </summary>
/// <remarks>
/// Recognised words are appended to the text box, where they can be edited. When the speaker pauses and the box does
/// not have focus, the text is sent after <see cref="IChatService.VoiceAutoSendDelay"/> as if Send had been pressed;
/// while the user is editing, they press Send themselves. While an answer is read aloud the microphone sends nothing.
/// </remarks>
public partial class PDChat
{
	private const string _voiceModulePath = "./_content/PanoramicData.Blazor/js/pdchat-voice.js";

	private IJSObjectReference? _voiceModule;
	private DotNetObjectReference<PDChat>? _voiceReference;
	private bool _isAwaitingSpokenAnswer;
	private bool _hasDictatedSinceTurn;
	private CancellationTokenSource? _autoSendCancellation;

	/// <summary>Gets where Voice Mode is in a spoken exchange.</summary>
	public PDChatVoiceState VoiceState { get; private set; } = PDChatVoiceState.Off;

	/// <summary>Gets the last problem Voice Mode reported, shown in place of its status.</summary>
	public string? VoiceError { get; private set; }

	// Dictation goes through the same text box as typing, via its IChatInput members.
	private PDMessages? VoiceInput => MessagesComponent;

	private bool IsVoiceModeOffered => ChatService.VoiceEndpoints is not null && ChatService.IsInputPermitted;

	private bool IsVoiceModeOn => VoiceState != PDChatVoiceState.Off;

	private bool IsReadAloudOn => IsVoiceModeOffered && ChatService.IsReadAloudEnabled;

	private string VoiceButtonTitle => IsVoiceModeOn
		? "Voice: stop listening"
		: "Voice: speak instead of typing";

	private string ReadAloudButtonTitle => IsReadAloudOn
		? "Read answers aloud: on"
		: "Read answers aloud: off";

	private string VoiceStatusCssClass => $"pdchat-voice-status pdchat-voice-{VoiceState.ToString().ToLowerInvariant()}" + (VoiceError is null ? string.Empty : " pdchat-voice-error");

	private string VoiceStatusText => VoiceError ?? VoiceState switch
	{
		PDChatVoiceState.Starting => "Opening the microphone…",
		PDChatVoiceState.Listening => VoiceInput?.IsFocused == true
			? "Listening. You are editing, so press Send when ready."
			: "Listening. Ask your question, then pause.",
		PDChatVoiceState.Thinking => "Thinking…",
		PDChatVoiceState.Speaking => "Reading the answer aloud.",
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
			var module = await GetVoiceModuleAsync(endpoints);
			await module.InvokeVoidAsync("start", endpoints.ListenUrl, _voiceReference);
			VoiceState = PDChatVoiceState.Listening;
		}
		catch (JSException)
		{
			VoiceState = PDChatVoiceState.Off;
			VoiceError = "The microphone could not be opened. Check that this site may use it.";
		}
	}

	// Loaded on first use, so the page asks for nothing until the user turns a voice control on.
	private async Task<IJSObjectReference> GetVoiceModuleAsync(PDChatVoiceEndpoints endpoints)
	{
		_voiceModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", endpoints.ModulePath ?? _voiceModulePath);
		_voiceReference ??= DotNetObjectReference.Create(this);
		return _voiceModule;
	}

	private async Task ToggleReadAloudAsync()
	{
		ChatService.IsReadAloudEnabled = !ChatService.IsReadAloudEnabled;
		if (ChatService.IsReadAloudEnabled)
		{
			return;
		}

		// Turned off mid-answer: stop talking, and give the microphone back if it was waiting.
		_isAwaitingSpokenAnswer = false;
		if (_voiceModule is not null)
		{
			await _voiceModule.InvokeVoidAsync("stopSpeaking");
		}

		if (VoiceState is PDChatVoiceState.Thinking or PDChatVoiceState.Speaking)
		{
			await ResumeListeningAsync();
		}
	}

	private async Task ResumeListeningAsync()
	{
		VoiceState = PDChatVoiceState.Listening;
		await (_voiceModule?.InvokeVoidAsync("pause", false) ?? ValueTask.CompletedTask);
	}

	private async Task StopVoiceModeAsync()
	{
		CancelAutoSend();
		VoiceState = PDChatVoiceState.Off;
		_isAwaitingSpokenAnswer = false;
		_hasDictatedSinceTurn = false;
		if (_voiceModule is not null)
		{
			await _voiceModule.InvokeVoidAsync("stop");
		}
	}

	/// <summary>Called by the voice module as words are recognised; appends them to the text box.</summary>
	/// <param name="text">The word just heard.</param>
	[JSInvokable]
	public Task OnVoiceWord(string text) => InvokeAsync(async () =>
	{
		if (VoiceState != PDChatVoiceState.Listening || VoiceInput is not { } input)
		{
			return;
		}

		// The speaker has carried on, so the pause that scheduled a send is over.
		CancelAutoSend();
		_hasDictatedSinceTurn = true;
		await input.AppendAsync(text);
	});

	/// <summary>
	/// Called by the voice module when the speaker pauses. Unless the user is editing the text box, its text is sent
	/// after <see cref="IChatService.VoiceAutoSendDelay"/>.
	/// </summary>
	/// <param name="text">Everything said since the last pause.</param>
	[JSInvokable]
	public Task OnVoiceTurn(string text) => InvokeAsync(async () =>
	{
		if (VoiceState != PDChatVoiceState.Listening || VoiceInput is not { } input)
		{
			return;
		}

		CancelAutoSend();

		// A host that reports no words still has its pause recorded.
		if (!_hasDictatedSinceTurn)
		{
			await input.AppendAsync(text);
		}

		_hasDictatedSinceTurn = false;
		if (!input.IsFocused && !string.IsNullOrWhiteSpace(input.Text))
		{
			ScheduleAutoSend();
		}

		StateHasChanged();
	});

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
		CancelAutoSend();
		VoiceState = PDChatVoiceState.Off;
		_isAwaitingSpokenAnswer = false;
		VoiceError ??= "Voice Mode stopped: the connection closed.";
		return InvokeAsync(StateHasChanged);
	}

	private Task OnInputFocusChangedAsync(bool isFocused)
	{
		// The user is editing, so whatever was dictated waits for them to press Send.
		if (isFocused)
		{
			CancelAutoSend();
		}

		return Task.CompletedTask;
	}

	private void ScheduleAutoSend()
	{
		var cancellation = new CancellationTokenSource();
		_autoSendCancellation = cancellation;
		_ = AutoSendAfterDelayAsync(cancellation.Token);
	}

	private void CancelAutoSend()
	{
		if (_autoSendCancellation is null)
		{
			return;
		}

		_autoSendCancellation.Cancel();
		_autoSendCancellation.Dispose();
		_autoSendCancellation = null;
	}

	private async Task AutoSendAfterDelayAsync(CancellationToken cancellationToken)
	{
		try
		{
			var delay = ChatService.VoiceAutoSendDelay;
			await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, cancellationToken);
			await InvokeAsync(async () =>
			{
				if (cancellationToken.IsCancellationRequested || VoiceState != PDChatVoiceState.Listening
					|| VoiceInput is not { IsFocused: false } input)
				{
					return;
				}

				await input.SendAsync();
			});
		}
		catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
		{
			// Cancelled by the speaker or the user, or the circuit is going away.
		}
	}

	/// <summary>
	/// Called as a message is about to be sent. The dictation turn is over; with read-aloud on, the answer is awaited,
	/// and a listening microphone pauses until it has been spoken.
	/// </summary>
	private async Task BeginSpokenExchangeAsync()
	{
		CancelAutoSend();
		_hasDictatedSinceTurn = false;
		_isAwaitingSpokenAnswer = IsReadAloudOn;
		if (!_isAwaitingSpokenAnswer || VoiceState != PDChatVoiceState.Listening)
		{
			return;
		}

		VoiceState = PDChatVoiceState.Thinking;

		// Half duplex: while Merlin thinks and speaks, the microphone sends nothing, so it never hears itself.
		await (_voiceModule?.InvokeVoidAsync("pause", true) ?? ValueTask.CompletedTask);
	}

	/// <summary>Reads the first finished reply to a sent message aloud, when read-aloud is on.</summary>
	private async Task SpeakAnswerIfAwaitedAsync(ChatMessage message)
	{
		if (!_isAwaitingSpokenAnswer || message.Sender.IsUser || message.Type == MessageType.Typing
			|| ChatService.VoiceEndpoints is not { } endpoints)
		{
			return;
		}

		_isAwaitingSpokenAnswer = false;
		if (VoiceState == PDChatVoiceState.Thinking)
		{
			VoiceState = PDChatVoiceState.Speaking;
			await InvokeAsync(StateHasChanged);
		}

		try
		{
			var module = await GetVoiceModuleAsync(endpoints);

			// The module calls OnVoiceSpoken when the last of the audio has played.
			await module.InvokeVoidAsync("speak", endpoints.SpeakUrl, ToSpeakableText(message), _voiceReference);
		}
		catch (JSException)
		{
			VoiceError = "The answer could not be read aloud.";
			await OnVoiceSpoken();
		}
	}

	/// <summary>Called by the voice module when an answer has finished playing; a paused microphone listens again.</summary>
	[JSInvokable]
	public async Task OnVoiceSpoken()
	{
		if (VoiceState is not (PDChatVoiceState.Speaking or PDChatVoiceState.Thinking))
		{
			return;
		}

		await ResumeListeningAsync();
		await InvokeAsync(StateHasChanged);
	}

	// The host turns the text into speech; this only removes HTML markup, which would otherwise be read out.
	private static string ToSpeakableText(ChatMessage message)
		=> message.IsMessageHtml ? WebUtility.HtmlDecode(HtmlTag().Replace(message.Message, " ")) : message.Message;

	[GeneratedRegex("<[^>]+>")]
	private static partial Regex HtmlTag();

	private async ValueTask DisposeVoiceAsync()
	{
		CancelAutoSend();
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
