namespace PanoramicData.Blazor;
public partial class PDMessages
{
	[Inject] public required IJSRuntime JSRuntime { get; set; }

	[Parameter] public List<ChatMessage>? Messages { get; set; }

	[Parameter] public string CurrentInput { get; set; } = string.Empty;

	[Parameter] public EventCallback<string> CurrentInputChanged { get; set; }

	[Parameter] public bool IsLive { get; set; }

	[Parameter] public bool CanSend { get; set; }

	[Parameter] public EventCallback OnSendClicked { get; set; }

	[Parameter] public Func<ChatMessage, string?>? UserIconSelector { get; set; }

	[Parameter] public bool UseFullWidthMessages { get; set; } = true;

	[Parameter] public UserInfoMode UserInfoMode { get; set; } = UserInfoMode.UserOnlyOnRightOthersOnLeft;

	private ElementReference _messagesContainer { get; set; }

	private IJSObjectReference? _module;
	private ElementReference _inputRef;

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
		if (CanSend && OnSendClicked.HasDelegate)
		{
			CurrentInput = string.Empty;
			await OnSendClicked.InvokeAsync();
		}
	}

	private async Task OnInputKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter" && !e.ShiftKey && !e.AltKey && !e.CtrlKey)
		{
			await OnSendClickedInternal();
		}
	}
}