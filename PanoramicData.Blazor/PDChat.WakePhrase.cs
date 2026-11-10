namespace PanoramicData.Blazor;

/// <summary>
/// PDChat: the wake phrase. With <see cref="IChatService.WakePhrases"/> set, Voice Mode goes
/// <see cref="PDChatVoiceState.Dormant"/> after <see cref="IChatService.VoiceIdleTimeout"/> without a word, and listens
/// again only once one of the phrases has been heard.
/// </summary>
public partial class PDChat
{
	private readonly List<string> _wakeBuffer = [];
	private CancellationTokenSource? _idleCancellation;

	private string FirstWakePhrase => ChatService.WakePhrases?.FirstOrDefault(phrase => !string.IsNullOrWhiteSpace(phrase))?.Trim()
		?? string.Empty;

	private List<string[]> WakePhraseWords() => ToPhraseWords(ChatService.WakePhrases);

	private static List<string[]> ToPhraseWords(IReadOnlyList<string>? phrases) => [.. (phrases ?? [])
		.Select(phrase => ToWakeWords(phrase ?? string.Empty))
		.Where(words => words.Length > 0)];

	private static string[] ToWakeWords(string text) => [.. text
		.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
		.Select(NormaliseWakeWord)
		.Where(word => word.Length > 0)];

	private static string NormaliseWakeWord(string word)
		=> new([.. word.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

	/// <summary>
	/// Listens for a wake phrase in what was heard while dormant. Returns the words after the phrase, once it wakes,
	/// or nothing while it stays dormant.
	/// </summary>
	private string HearWhileDormant(string text)
	{
		var phrases = WakePhraseWords();
		if (phrases.Count == 0)
		{
			// The host has removed its wake phrases, so there is nothing to wait for.
			Wake();
			return text;
		}

		var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		var next = FindPhraseEnd(words, phrases, _wakeBuffer);
		if (next < 0)
		{
			return string.Empty;
		}

		Wake();
		return string.Join(' ', words[next..]);
	}

	/// <summary>
	/// Adds the words to the buffer of what was just heard and returns the index of the word after the first one that
	/// completes one of the phrases, or -1 when none does. Matching ignores case and punctuation.
	/// </summary>
	private static int FindPhraseEnd(string[] words, List<string[]> phrases, List<string> buffer)
	{
		var longest = phrases.Max(phrase => phrase.Length);
		for (var index = 0; index < words.Length; index++)
		{
			var word = NormaliseWakeWord(words[index]);
			if (word.Length == 0)
			{
				continue;
			}

			buffer.Add(word);
			if (buffer.Count > longest)
			{
				buffer.RemoveAt(0);
			}

			if (phrases.Any(phrase => EndsWithPhrase(buffer, phrase)))
			{
				return index + 1;
			}
		}

		return -1;
	}

	private static bool EndsWithPhrase(List<string> buffer, string[] phrase)
		=> buffer.Count >= phrase.Length
			&& buffer.Skip(buffer.Count - phrase.Length).SequenceEqual(phrase, StringComparer.Ordinal);

	private void Wake()
	{
		_wakeBuffer.Clear();
		VoiceState = PDChatVoiceState.Listening;
		RestartIdleTimer();
		StateHasChanged();
	}

	private void RestartIdleTimer()
	{
		CancelIdleTimer();
		if (VoiceState != PDChatVoiceState.Listening || WakePhraseWords().Count == 0 || VoiceMode == PDChatVoiceMode.Dictate)
		{
			return;
		}

		var cancellation = new CancellationTokenSource();
		_idleCancellation = cancellation;
		_ = GoDormantWhenIdleAsync(cancellation.Token);
	}

	private void CancelIdleTimer()
	{
		if (_idleCancellation is null)
		{
			return;
		}

		_idleCancellation.Cancel();
		_idleCancellation.Dispose();
		_idleCancellation = null;
	}

	private async Task GoDormantWhenIdleAsync(CancellationToken cancellationToken)
	{
		try
		{
			var timeout = ChatService.VoiceIdleTimeout;
			await Task.Delay(timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout, cancellationToken);
			await InvokeAsync(() =>
			{
				if (cancellationToken.IsCancellationRequested || VoiceState != PDChatVoiceState.Listening)
				{
					return;
				}

				// The user is editing, or dictation is about to be sent: wait for another quiet spell.
				if (VoiceInput?.IsFocused == true || _autoSendCancellation is not null)
				{
					RestartIdleTimer();
					return;
				}

				CancelIdleTimer();
				_wakeBuffer.Clear();
				VoiceState = PDChatVoiceState.Dormant;
				StateHasChanged();
			});
		}
		catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
		{
			// Cancelled by a word or by Voice going off, or the circuit is going away.
		}
	}
}
