namespace PanoramicData.Blazor;
public partial class PDMessages
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

	private ElementReference _messagesContainer { get; set; }

	private IJSObjectReference? _module;
	private ElementReference _inputRef;
	private string _textareaKey = Guid.NewGuid().ToString();

	protected override async Task OnParametersSetAsync()
	{
		// Always try to scroll to bottom when parameters change (new messages)
		await ScrollToBottomAsync();
	}

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDMessages.razor.js").ConfigureAwait(true);
		}

		await ScrollToBottomAsync();


		if (_inputRef.Context != null)
		{
			await _inputRef.FocusAsync();
		}
	}

	private async Task ScrollToBottomAsync()
	{
		if (_module is null)
		{
			return;
		}

		// Small delay to ensure DOM is updated
		await Task.Delay(10);

		try
		{
			await _module.InvokeVoidAsync("scrollToBottom", _messagesContainer);
		}
		catch (Exception)
		{
			// Ignore JavaScript errors if the element is not available
		}
	}

	private async Task OnInputChanged(ChangeEventArgs e)
	{
		var eventValue = e.Value?.ToString() ?? string.Empty;
		if (CurrentInput == eventValue.TrimEnd())
		{
			return; // No change, do nothing
		}

		var value = eventValue;
		// Remove single return character
		if (value == "\n" || value == "\r" || value == "\r\n")
		{
			value = string.Empty;
		}

		CurrentInput = value;
		await CurrentInputChanged.InvokeAsync(value);
	}

	private async Task OnSendClickedInternal()
	{
		if (!CanSend || !OnSendClicked.HasDelegate)
		{
			return;
		}

		// Force textarea to re-render with a new key (C#-only double buffering)
		_textareaKey = Guid.NewGuid().ToString();
		CurrentInput = string.Empty;

		// Force a re-render to create a new textarea element
		StateHasChanged();

		await OnSendClicked.InvokeAsync();
	}

	private async Task OnInputKeyDown(KeyboardEventArgs e)
	{
		if (e.Key != "Enter" || e.ShiftKey || e.AltKey || e.CtrlKey)
		{
			return;
		}

		await OnSendClickedInternal();
	}
}