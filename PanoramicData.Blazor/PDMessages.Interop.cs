namespace PanoramicData.Blazor;

/// <summary>
/// PDMessages: the JavaScript interop that scrolls the messages and sends on Enter.
/// </summary>
public partial class PDMessages : IAsyncDisposable
{
	private IJSObjectReference? _module;
	private DotNetObjectReference<PDMessages>? _dotNetRef;
	private bool _enterHandlerAttached;

	/// <summary>
	/// Attaches the Enter handler again on the next render, e.g. to a new input element.
	/// </summary>
	private void ResetEnterHandler()
	{
		_enterHandlerAttached = false;
	}

	/// <inheritdoc />
	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module =
				await JSRuntime.InvokeAsync<IJSObjectReference>(
					"import",
					"./_content/PanoramicData.Blazor/PDMessages.razor.js")
				.ConfigureAwait(true);
		}

		await ScrollToBottomAsync();

		if (!_enterHandlerAttached && _module is not null && InputRef.Context != null)
		{
			try
			{
				_dotNetRef ??= DotNetObjectReference.Create(this);
				await _module.InvokeVoidAsync("attachEnterHandler", InputRef, _dotNetRef);
				_enterHandlerAttached = true;
				if (IsInputAutoFocused)
				{
					await InputRef.FocusAsync();
				}
			}
			catch (Exception)
			{
				// Ignore JS errors if the module or element is not yet available; will retry on next render
			}
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		try
		{
			if (_module is not null && InputRef.Context != null)
			{
				await _module.InvokeVoidAsync("detachEnterHandler", InputRef);
			}
		}
		catch (Exception)
		{
			// Do nothing - if the module or element is already gone, we can't detach the handler, but that's not a big deal
		}

		_dotNetRef?.Dispose();

		try
		{
			if (_module is not null)
			{
				await _module.DisposeAsync();
			}
		}
		catch (Exception)
		{
			// Do nothing - if the module is already gone, we can't dispose it, but that's not a big deal
		}
	}

	private async Task ScrollToBottomAsync()
	{
		if (_module is null)
		{
			return;
		}

		await Task.Delay(10);

		try
		{
			await _module.InvokeVoidAsync("scrollToBottom", MessagesContainer);
		}
		catch (Exception)
		{
			// Ignore JS errors if element is not yet available
		}
	}
}
