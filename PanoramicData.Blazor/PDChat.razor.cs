namespace PanoramicData.Blazor;

public partial class PDChat
{
	[Inject] IJSRuntime JSRuntime { get; set; }

	[EditorRequired]
	[Parameter]
	public required IChatService Service { get; set; }

	[Parameter]
	public string Title { get; set; } = "Chat";

	[Parameter]
	public string CollapsedIcon { get; set; } = "💬";

	[Parameter]
	public Func<ChatMessage, string>? UserIconSelector { get; set; }

	[Parameter]
	public Func<ChatMessage, string>? PriorityIconSelector { get; set; }

	private IJSObjectReference? _module;
	private bool _isOpen;
	private string _currentInput = "";
	private readonly List<ChatMessage> _messages = [];

	protected override Task OnInitializedAsync()
	{
		Service.OnMessageReceived += OnMessageReceived;
		return base.OnInitializedAsync();
	}

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDChat.razor.js").ConfigureAwait(true);
		}

		await ScrollToBottomAsync();
	}

	private void OnMessageReceived(ChatMessage message)
	{
		var existing = _messages.FirstOrDefault(m => m.Id == message.Id);
		if (existing != null)
		{
			existing.Message = message.Message;
			existing.IsThinking = message.IsThinking;
		}
		else
		{
			_messages.Add(message);
		}

		InvokeAsync(StateHasChanged);
	}

	public void ShowToast(ChatMessage message)
	{
		_messages.Add(message);
		InvokeAsync(StateHasChanged);
	}

	private void ToggleChat()
	{
		_isOpen = !_isOpen;
	}

	private async Task OnInputKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter" && !e.ShiftKey)
		{
			await SendCurrentMessageAsync();
		}
		// else let Shift+Enter insert newline naturally
	}

	private async Task SendCurrentMessageAsync()
	{
		if (string.IsNullOrWhiteSpace(_currentInput))
		{
			return;
		}

		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = _currentInput,
			Sender = "User",
			Title = "User Input",
			Priority = MessagePriority.Normal,
			Timestamp = DateTime.UtcNow
		};

		// Fire it off
		Service.SendMessage(message);

		// Clear input
		_currentInput = string.Empty;
	}

	private string GetDefaultUserIcon(ChatMessage chatMessage)
	{
		return chatMessage.Sender switch
		{
			"User" => "🙋",
			string x when x.EndsWith("Bot") => "🤖",
			_ => "👤"
		};
	}

	private string GetDefaultPriorityIcon(ChatMessage chatMessage)
	{
		return chatMessage.Priority switch
		{
			MessagePriority.Normal => "ℹ️",
			MessagePriority.Warning => "⚠️",
			MessagePriority.High => "❗",
			MessagePriority.Critical => "🚨",
			_ => "❓"
		};
	}

	private ElementReference messagesContainer;

	private async Task ScrollToBottomAsync()
	{
		if (_module is not null)
		{
			await _module.InvokeVoidAsync("scrollToBottom", messagesContainer);
			StateHasChanged();
		}
	}
}
