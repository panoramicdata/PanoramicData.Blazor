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

	private List<string[]> WakePhraseWords() => [.. (ChatService.WakePhrases ?? [])
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

		var longest = phrases.Max(phrase => phrase.Length);
		var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		for (var index = 0; index < words.Length; index++)
		{
			var word = NormaliseWakeWord(words[index]);
			if (word.Length == 0)
			{
				continue;
			}

			_wakeBuffer.Add(word);
			if (_wakeBuffer.Count > longest)
			{
				_wakeBuffer.RemoveAt(0);
			}

			if (phrases.Any(EndsWithWakePhrase))
			{
				Wake();
				return string.Join(' ', words[(index + 1)..]);
			}
		}

		return string.Empty;
	}

	private bool EndsWithWakePhrase(string[] phrase)
		=> _wakeBuffer.Count >= phrase.Length
			&& _wakeBuffer.Skip(_wakeBuffer.Count - phrase.Length).SequenceEqual(phrase, StringComparer.Ordinal);

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
		if (VoiceState != PDChatVoiceState.Listening || WakePhraseWords().Count == 0)
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
