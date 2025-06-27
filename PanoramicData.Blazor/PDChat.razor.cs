namespace PanoramicData.Blazor;

public partial class PDChat
{
	[Inject] IJSRuntime JSRuntime { get; set; }

	[EditorRequired]
	[Parameter]
	public required IChatService ChatService { get; set; }

	[EditorRequired]
	[Parameter]
	public required ChatMessageSender User { get; set; }

	[Parameter]
	public string Title { get; set; } = "Chat";

	[Parameter]
	public string CollapsedIcon { get; set; } = "💬";

	[Parameter]
	public Func<ChatMessage, string?>? UserIconSelector { get; set; }

	[Parameter]
	public Func<ChatMessage, string?>? PriorityIconSelector { get; set; }

	[Parameter]
	public Func<ChatMessage, string?>? SoundSelector { get; set; }

	private IJSObjectReference? _module;
	private bool _isOpen;
	private bool _isMuted;
	private ElementReference _messagesContainer;
	private bool _unreadMessages;
	private string _currentInput = "";
	private readonly List<ChatMessage> _messages = [];

	protected override Task OnInitializedAsync()
	{
		ChatService.OnMessageReceived += OnMessageReceived;
		ChatService.OnLiveStatusChanged += OnLiveStatusChanged;
		ChatService.Initialize();
		return base.OnInitializedAsync();
	}

	private void OnLiveStatusChanged(bool obj)
	{
		StateHasChanged();
	}

	protected async override Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PanoramicData.Blazor/PDChat.razor.js").ConfigureAwait(true);
		}
	}

	private void OnMessageReceived(ChatMessage message)
	{
		var existing = _messages.FirstOrDefault(m => m.Id == message.Id);
		if (existing != null)
		{
			existing.Message = message.Message;
			existing.Type = message.Type;
			existing.Title = message.Title;
			existing.Timestamp = message.Timestamp;
		}
		else
		{
			_messages.Add(message);
		}

		if (!_isOpen)
		{
			_unreadMessages = true;
		}

		// Get the sound to play, if any
		var soundUrlString = SoundSelector?.Invoke(message);
		if (!string.IsNullOrWhiteSpace(soundUrlString) && !_isMuted)
		{
			var soundUrl = new Uri(soundUrlString, UriKind.RelativeOrAbsolute);
			// Play the sound
			_module?.InvokeVoidAsync("playSound", soundUrlString);
		}

		InvokeAsync(ScrollToBottomAsync);
	}

	public void ShowToast(ChatMessage message)
	{
		_messages.Add(message);
		InvokeAsync(StateHasChanged);
	}

	private void ToggleChat()
	{
		_isOpen = !_isOpen;

		if (_isOpen)
		{
			_unreadMessages = false;
			InvokeAsync(ScrollToBottomAsync);
		}
	}

	private void ToggleMute()
	{
		_isMuted = !_isMuted;
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
			Sender = User,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};

		// Fire it off
		ChatService.SendMessage(message);

		// Clear input
		_currentInput = string.Empty;

		await ScrollToBottomAsync();
	}

	private static string GetDefaultUserIcon(ChatMessage chatMessage)
		=> chatMessage.Sender.IsUser ? "🙋"
			: !chatMessage.Sender.IsHuman ? "🤖"
			: "👤";

	private static string GetDefaultPriorityIcon(ChatMessage chatMessage)
	=> chatMessage.Type switch
	{
		MessageType.Normal => "ℹ️",
		MessageType.Warning => "⚠️",
		MessageType.Error => "❗",
		MessageType.Critical => "🚨",
		_ => "❓"
	};

	private async Task ScrollToBottomAsync()
	{
		if (_module is null)
		{
			return;
		}

		StateHasChanged();

		await _module.InvokeVoidAsync("scrollToBottom", _messagesContainer);
	}

	private bool CanSend => ChatService.IsLive && !string.IsNullOrWhiteSpace(_currentInput);
}
