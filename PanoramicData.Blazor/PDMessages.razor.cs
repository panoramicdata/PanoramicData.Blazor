namespace PanoramicData.Blazor;
public partial class PDMessages
{
	[Parameter] public List<ChatMessage>? Messages { get; set; }
	[Parameter] public string CurrentInput { get; set; } = string.Empty;
	[Parameter] public EventCallback<string> CurrentInputChanged { get; set; }
	[Parameter] public bool IsLive { get; set; }
	[Parameter] public bool CanSend { get; set; }
	[Parameter] public EventCallback OnSendClicked { get; set; }
	[Parameter] public Func<ChatMessage, string?>? UserIconSelector { get; set; }
	[Parameter] public ElementReference MessagesContainer { get; set; }

	private async Task OnInputChanged(ChangeEventArgs e)
	{
		var value = e.Value?.ToString() ?? string.Empty;
		CurrentInput = value;
		await CurrentInputChanged.InvokeAsync(value);
	}

	private async Task OnSendClickedInternal()
	{
		if (CanSend && OnSendClicked.HasDelegate)
		{
			await OnSendClicked.InvokeAsync();
			CurrentInput = string.Empty;
			StateHasChanged();
			await CurrentInputChanged.InvokeAsync("");
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