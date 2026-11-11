namespace PanoramicData.Blazor;

/// <summary>
/// PDChat: interrupting a spoken answer. With <see cref="IChatService.InterruptPhrases"/> set, the microphone stays open
/// while an answer is read aloud, but what it hears goes only to a matcher for those phrases and never to the text box.
/// Hearing one stops the answer and starts listening.
/// </summary>
public partial class PDChat
{
	private readonly List<string> _interruptBuffer = [];
	private bool _isListeningForInterrupt;
	private bool _discardNextTurn;

	/// <summary>
	/// Opens the microphone for an answer about to be read, when there are interrupt phrases and the answer does not
	/// itself contain one, since it would then interrupt itself. Otherwise the microphone stays paused.
	/// </summary>
	private async Task BeginInterruptListeningAsync(string spoken)
	{
		var phrases = ToPhraseWords(ChatService.InterruptPhrases);
		if (phrases.Count == 0 || ContainsPhrase(spoken, phrases))
		{
			return;
		}

		_interruptBuffer.Clear();
		_isListeningForInterrupt = true;
		await SetMicrophonePausedAsync(false);
	}

	// Split on everything that is not a letter or digit, so that "Merlin's" and "Merlin-based" count as saying Merlin.
	private static bool ContainsPhrase(string text, List<string[]> phrases)
	{
		var words = NonWordRun().Split(text.ToLowerInvariant()).Where(word => word.Length > 0).ToArray();
		return phrases.Any(phrase => Enumerable.Range(0, Math.Max(0, words.Length - phrase.Length + 1))
			.Any(start => words.Skip(start).Take(phrase.Length).SequenceEqual(phrase, StringComparer.Ordinal)));
	}

	[GeneratedRegex(@"[^\p{L}\p{N}]+")]
	private static partial Regex NonWordRun();

	/// <summary>Lets a word heard while an answer plays reach only the interrupt matcher. True when it is not dictation.</summary>
	private async Task<bool> IsWordDiscardedAsync(string text)
	{
		if (!_isListeningForInterrupt)
		{
			return _discardNextTurn;
		}

		// An utterance is under way, so the pause that ends it must not be taken for dictation either.
		_hasDictatedSinceTurn = true;
		await HearWhileSpeakingAsync(text, isUtteranceOver: false);
		return true;
	}

	/// <summary>Lets a turn heard while an answer plays reach only the interrupt matcher. True when it is not dictation.</summary>
	private async Task<bool> IsTurnDiscardedAsync(string text, bool hasHeardWords)
	{
		if (_isListeningForInterrupt)
		{
			_interruptBuffer.Clear();

			// A host that reports only pauses is listened to through them.
			if (!hasHeardWords)
			{
				await HearWhileSpeakingAsync(text, isUtteranceOver: true);
			}

			return true;
		}

		var isDiscarded = _discardNextTurn;
		_discardNextTurn = false;
		return isDiscarded;
	}

	private async Task HearWhileSpeakingAsync(string text, bool isUtteranceOver)
	{
		var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		if (FindPhraseEnd(words, ToPhraseWords(ChatService.InterruptPhrases), _interruptBuffer) < 0)
		{
			return;
		}

		await InterruptSpeechAsync(isUtteranceOver);
	}

	/// <summary>
	/// Stops the answer and listens, with the text box unchanged. What is left of the utterance that did it is discarded,
	/// so words that follow the phrase without a pause are not typed.
	/// </summary>
	private async Task InterruptSpeechAsync(bool isUtteranceOver)
	{
		EndInterruptListening();
		_returnToDormantAfterExchange = false;
		_discardNextTurn = !isUtteranceOver;
		_hasDictatedSinceTurn = false;
		if (_voiceModule is not null)
		{
			try
			{
				await _voiceModule.InvokeVoidAsync("stopSpeaking");
			}
			catch (JSDisconnectedException)
			{
				// The circuit is gone, so there is nothing left to do.
			}
		}

		await EnterListeningAsync();
		StateHasChanged();
	}

	private void EndInterruptListening()
	{
		_isListeningForInterrupt = false;
		_interruptBuffer.Clear();
	}

	private void ResetInterruptState()
	{
		EndInterruptListening();
		_discardNextTurn = false;
	}
}
