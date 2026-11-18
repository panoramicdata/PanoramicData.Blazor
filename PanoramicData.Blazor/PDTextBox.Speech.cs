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
		try
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDTextBox.razor.js").ConfigureAwait(true);
		}
		catch (JSDisconnectedException)
		{
			// The circuit is gone, so there is nothing left to do.
		}
		if (_module != null)
		{
			try
			{
				await _module.InvokeVoidAsync("initSpeech", SpeechLang).ConfigureAwait(true);
			}
			catch (JSDisconnectedException)
			{
				// The circuit is gone, so there is nothing left to do.
			}
		}
	}

	private async Task DisposeSpeechAsync()
	{
		if (_module != null)
		{
			try
			{
				await _module.InvokeVoidAsync("termSpeech").ConfigureAwait(true);
			}
			catch (JSDisconnectedException)
			{
				// The circuit is gone, so there is nothing left to do.
			}
			await _module.DisposeAsync().ConfigureAwait(true);
		}
	}

	private async Task OnListenForSpeech()
	{
		if (_module != null)
		{
			if (_activeListener == Id)
			{
				try
				{
					await _module.InvokeVoidAsync("abortListenForSpeech", _objRef).ConfigureAwait(true);
				}
				catch (JSDisconnectedException)
				{
					// The circuit is gone, so there is nothing left to do.
				}
			}
			else
			{
				try
				{
					await _module.InvokeVoidAsync("abortListenForSpeech", _objRef).ConfigureAwait(true);
				}
				catch (JSDisconnectedException)
				{
					// The circuit is gone, so there is nothing left to do.
				}
				await Task.Delay(100).ConfigureAwait(true);
				Volatile.Write(ref _activeListener, Id);
				try
				{
					await _module.InvokeVoidAsync("startListenForSpeech", _objRef).ConfigureAwait(true);
				}
				catch (JSDisconnectedException)
				{
					// The circuit is gone, so there is nothing left to do.
				}
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
