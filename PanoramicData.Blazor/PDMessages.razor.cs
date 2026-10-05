namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that displays a list of chat messages with a send input area.
/// </summary>
public partial class PDMessages : IChatInput
{
	/// <summary>
	/// Gets or sets the injected JavaScript runtime.
	/// </summary>
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
	/// Gets or sets whether the message input (text area and send button) is rendered.
	/// When false, the input is hidden and, if <see cref="InputDisabledMessage"/> is set,
	/// that message is shown in its place. Defaults to true.
	/// </summary>
	[Parameter] public bool IsInputPermitted { get; set; } = true;

	/// <summary>
	/// Gets or sets an optional message shown where the input would normally appear when
	/// <see cref="IsInputPermitted"/> is false. When null or empty, nothing is rendered in
	/// place of the input. Rendered as plain text.
	/// </summary>
	[Parameter] public string? InputDisabledMessage { get; set; }

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

	/// <summary>
	/// Gets or sets the messages container element, set by the component markup.
	/// </summary>
	internal ElementReference MessagesContainer { get; set; }

	/// <summary>
	/// Gets or sets the message input element, set by the component markup.
	/// </summary>
	internal ElementReference InputRef { get; set; }

	/// <summary>
	/// Gets or sets optional controls shown in the input row beside the Send button, such as a voice or agent control.
	/// </summary>
	[Parameter] public RenderFragment? InputAccessories { get; set; }

	/// <summary>
	/// An event callback that is invoked when the text box gains (<c>true</c>) or loses (<c>false</c>) focus.
	/// </summary>
	[Parameter] public EventCallback<bool> InputFocusChanged { get; set; }

	/// <summary>
	/// Gets or sets whether the text box takes focus when first shown and after each send. Defaults to true.
	/// </summary>
	[Parameter] public bool IsInputAutoFocused { get; set; } = true;

	private string _localInput = string.Empty;
	private string _inputKey = Guid.NewGuid().ToString();

	private bool CanSendLocal => IsInputPermitted && IsLive && !string.IsNullOrWhiteSpace(_localInput);

	/// <inheritdoc />
	public string Text => _localInput;

	/// <inheritdoc />
	public bool IsFocused { get; private set; }

	/// <inheritdoc />
	public async Task AppendAsync(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		var addition = text.Trim();
		_localInput = _localInput.Length == 0 || char.IsWhiteSpace(_localInput[^1])
			? _localInput + addition
			: $"{_localInput} {addition}";
		await CurrentInputChanged.InvokeAsync(_localInput);
		StateHasChanged();
	}

	/// <inheritdoc />
	public Task SendAsync() => OnSendClickedInternal();

	/// <summary>
	/// Clears the textarea. Called by the parent after a message is sent.
	/// </summary>
	public void ClearInput()
	{
		_localInput = string.Empty;
		_inputKey = Guid.NewGuid().ToString();

		// The text box is replaced, and a removed element does not reliably report losing focus.
		IsFocused = false;
		ResetEnterHandler();
		StateHasChanged();
	}

	private async Task SetInputFocusedAsync(bool isFocused)
	{
		IsFocused = isFocused;
		await InputFocusChanged.InvokeAsync(isFocused);
	}

	/// <summary>
	/// Called from JavaScript when Enter is pressed in the textarea.
	/// </summary>
	[JSInvokable]
	public async Task OnEnterPressed()
	{
		await OnSendClickedInternal();
	}

	/// <inheritdoc />
	protected override async Task OnParametersSetAsync()
	{
		await ScrollToBottomAsync();
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