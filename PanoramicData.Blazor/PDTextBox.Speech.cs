namespace PanoramicData.Blazor;

/// <summary>
/// PDTextBox: entering the text by speech recognition.
/// </summary>
public partial class PDTextBox
{
	private IJSObjectReference? _module;
	private static string _activeListener = string.Empty;

	private async Task InitializeSpeechAsync()
	{
		_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDTextBox.razor.js").ConfigureAwait(true);
		if (_module != null)
		{
			await _module.InvokeVoidAsync("initSpeech", SpeechLang).ConfigureAwait(true);
		}
	}

	private async Task DisposeSpeechAsync()
	{
		if (_module != null)
		{
			await _module.InvokeVoidAsync("termSpeech").ConfigureAwait(true);
			await _module.DisposeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnListenForSpeech()
	{
		if (_module != null)
		{
			if (_activeListener == Id)
			{
				await _module.InvokeVoidAsync("abortListenForSpeech", _objRef).ConfigureAwait(true);
			}
			else
			{
				await _module.InvokeVoidAsync("abortListenForSpeech", _objRef).ConfigureAwait(true);
				await Task.Delay(100).ConfigureAwait(true);
				Volatile.Write(ref _activeListener, Id);
				await _module.InvokeVoidAsync("startListenForSpeech", _objRef).ConfigureAwait(true);
			}
		}
	}

	/// <summary>
	/// Receives speech recognition results from JavaScript.
	/// </summary>
	/// <param name="value">Recognized text.</param>
	[JSInvokable]
	public async Task OnSpeechResult(string value)
	{
		Value = value;
		await ValueChanged.InvokeAsync(value).ConfigureAwait(true);
		StateHasChanged();
	}

	/// <summary>
	/// Indicates that speech recognition has started.
	/// </summary>
	[JSInvokable]
	public void OnListeningStarted() => StateHasChanged();

	/// <summary>
	/// Indicates that speech recognition has stopped.
	/// </summary>
	[JSInvokable]
	public void OnListeningStopped()
	{
		Volatile.Write(ref _activeListener, string.Empty);
		StateHasChanged();
	}
}
