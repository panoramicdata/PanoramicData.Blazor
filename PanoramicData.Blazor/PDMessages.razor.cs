namespace PanoramicData.Blazor;

public partial class PDMessages : IAsyncDisposable
{
	[Inject] public required IJSRuntime JSRuntime { get; set; }

	/// <summary>
	/// Gets or sets the list of chat messages to display.
	/// </summary>
	[Parameter] public List<ChatMessage>? Messages { get; set; }

	/// <summary>
	/// Gets or sets the current user input.
	/// </summary>
	[Parameter] public string CurrentInput { get; set; } = string.Empty;

	/// <summary>
	/// An event callback that is invoked when the user input changes.
	/// </summary>
	[Parameter] public EventCallback<string> CurrentInputChanged { get; set; }

	/// <summary>
	/// Gets or sets whether the message stream is live.
	/// </summary>
	[Parameter] public bool IsLive { get; set; }

	/// <summary>
	/// Gets or sets whether the user can send a message.
	/// </summary>
	[Parameter] public bool CanSend { get; set; }

	/// <summary>
	/// An event callback that is invoked when the send button is clicked.
	/// </summary>
	[Parameter] public EventCallback OnSendClicked { get; set; }

	/// <summary>
	/// A function to select a user icon for a given message.
	/// </summary>
	[Parameter] public Func<ChatMessage, string?>? UserIconSelector { get; set; }

	/// <summary>
	/// Gets or sets whether messages should use the full width of the container.
	/// </summary>
	[Parameter] public bool UseFullWidthMessages { get; set; } = true;

	/// <summary>
	/// Gets or sets how message metadata is displayed.
	/// </summary>
	[Parameter] public MessageMetadataDisplayMode MessageMetadataDisplayMode { get; set; } = MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;

	/// <summary>
	/// Gets or sets whether to show the user icon for each message.
	/// </summary>
	[Parameter] public bool ShowMessageUserIcon { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the user name for each message.
	/// </summary>
	[Parameter] public bool ShowMessageUserName { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the timestamp for each message.
	/// </summary>
	[Parameter] public bool ShowMessageTimestamp { get; set; } = true;

	/// <summary>
	/// Gets or sets the format for the message timestamp.
	/// </summary>
	[Parameter] public string MessageTimestampFormat { get; set; } = "HH:mm:ss";

	private ElementReference MessagesContainer { get; set; }
	private IJSObjectReference? _module;
	private ElementReference _inputRef;
	private string _localInput = string.Empty;
	private string _inputKey = Guid.NewGuid().ToString();
	private DotNetObjectReference<PDMessages>? _dotNetRef;
	private bool _enterHandlerAttached;

	private bool CanSendLocal => IsLive && !string.IsNullOrWhiteSpace(_localInput);

	/// <summary>
	/// Clears the textarea. Called by the parent after a message is sent.
	/// </summary>
	public void ClearInput()
	{
		_localInput = string.Empty;
		_inputKey = Guid.NewGuid().ToString();
		_enterHandlerAttached = false;
		StateHasChanged();
	}

	/// <summary>
	/// Called from JavaScript when Enter is pressed in the textarea.
	/// </summary>
	[JSInvokable]
	public async Task OnEnterPressed()
	{
		await OnSendClickedInternal();
	}

	protected override async Task OnParametersSetAsync()
	{
		await ScrollToBottomAsync();
	}

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

		if (!_enterHandlerAttached && _module is not null && _inputRef.Context != null)
		{
			try
			{
				_dotNetRef ??= DotNetObjectReference.Create(this);
				await _module.InvokeVoidAsync("attachEnterHandler", _inputRef, _dotNetRef);
				_enterHandlerAttached = true;
				await _inputRef.FocusAsync();
			}
			catch (Exception)
			{
				// Ignore JS errors if the module or element is not yet available; will retry on next render
			}
		}
	}

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		try
		{
			if (_module is not null && _inputRef.Context != null)
			{
				await _module.InvokeVoidAsync("detachEnterHandler", _inputRef);
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

	private void OnInputChanged(ChangeEventArgs e)
	{
		_localInput = e.Value?.ToString() ?? string.Empty;
	}

	private async Task OnSendClickedInternal()
	{
		if (!CanSendLocal || !OnSendClicked.HasDelegate)
		{
			return;
		}

		// Push current text to parent before invoking send
		await CurrentInputChanged.InvokeAsync(_localInput);
		await OnSendClicked.InvokeAsync();
	}
}