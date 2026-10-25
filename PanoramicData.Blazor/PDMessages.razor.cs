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
	/// Gets or sets optional controls shown in the thin toolbar above the text box, before Send. The toolbar is shown
	/// even where input is not permitted, so a control such as notification sounds stays reachable.
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

	/// <summary>The most images one message may carry.</summary>
	public const int MaxImages = 5;

	/// <summary>
	/// Gets or sets whether pasting, dropping, picking and screenshot controls for images are offered. False by default.
	/// </summary>
	[Parameter] public bool IsImageInputOffered { get; set; }

	/// <summary>
	/// Gets or sets why images cannot be sent now. While set, Send is disabled and this is shown beside the images.
	/// </summary>
	[Parameter] public string? ImageBlockedReason { get; set; }

	/// <summary>Gets or sets the images in the message being composed, oldest first.</summary>
	[Parameter] public List<ChatImage> Images { get; set; } = [];

	/// <summary>An event callback invoked when an image is added to or removed from <see cref="Images"/>.</summary>
	[Parameter] public EventCallback<List<ChatImage>> ImagesChanged { get; set; }

	private string _localInput = string.Empty;
	private string _inputKey = Guid.NewGuid().ToString();

	private bool CanSendLocal => IsInputPermitted && IsLive && ImageBlockedReason is null
		&& (!string.IsNullOrWhiteSpace(_localInput) || (IsImageInputOffered && Images.Count > 0));

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

	/// <summary>Called from JavaScript for each image pasted, dropped, picked or captured.</summary>
	/// <param name="contentType">The image's media type.</param>
	/// <param name="base64">The image bytes as base64.</param>
	/// <param name="name">A file name, when there is one.</param>
	[JSInvokable]
	public async Task OnImageAdded(string contentType, string base64, string? name)
	{
		if (!IsImageInputOffered)
		{
			return;
		}

		if (Images.Count >= MaxImages)
		{
			ImageNotice = $"A message can carry at most {MaxImages} images.";
			StateHasChanged();
			return;
		}

		ImageNotice = null;
		await SetImagesAsync([.. Images, new ChatImage { ContentType = contentType, Base64Data = base64, Name = name }]);
	}

	/// <summary>Called from JavaScript when an image could not be added.</summary>
	/// <param name="message">What went wrong, for the user.</param>
	[JSInvokable]
	public Task OnImageError(string message) => InvokeAsync(() =>
	{
		ImageNotice = message;
		StateHasChanged();
	});

	/// <summary>Removes every image. Called by the parent after a message is sent.</summary>
	public void ClearImages()
	{
		ImageNotice = null;
		Images = [];
		StateHasChanged();
	}

	private string? ImageNotice { get; set; }

	private ElementReference FilePickerRef { get; set; }

	private async Task SetImagesAsync(List<ChatImage> images)
	{
		Images = images;
		await ImagesChanged.InvokeAsync(images);
		StateHasChanged();
	}

	private Task RemoveImageAsync(int index)
	{
		ImageNotice = null;
		return SetImagesAsync([.. Images.Where((_, i) => i != index)]);
	}

	private async Task OnAttachClickedAsync()
	{
		if (_module is not null)
		{
			await _module.InvokeVoidAsync("openFilePicker", FilePickerRef);
		}
	}

	private async Task OnFilePickerChangedAsync()
	{
		if (_module is not null)
		{
			await _module.InvokeVoidAsync("readPickedFiles", FilePickerRef, _dotNetRef, MaxImages - Images.Count);
		}
	}

	private async Task OnScreenshotClickedAsync()
	{
		if (_module is not null)
		{
			await _module.InvokeVoidAsync("captureTab", _dotNetRef);
		}
	}

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