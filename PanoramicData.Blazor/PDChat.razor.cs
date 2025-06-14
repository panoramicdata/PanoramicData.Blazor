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
	public Func<ChatMessage, string?>? UserIconSelector { get; set; }

	[Parameter]
	public Func<ChatMessage, string?>? PriorityIconSelector { get; set; }

	[Parameter]
	public Func<ChatMessage, string?>? SoundSelector { get; set; }

	private IJSObjectReference? _module;
	private bool _isOpen;
	private bool _isMuted = false;
	private string _currentInput = "";
	private readonly List<ChatMessage> _messages = [];

	protected override Task OnInitializedAsync()
	{
		Service.OnMessageReceived += OnMessageReceived;
		Service.Initialize();
		return base.OnInitializedAsync();
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

		InvokeAsync(StateHasChanged);

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
			Sender = "User",
			Title = "User Input",
			Type = MessageType.Normal,
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
		return chatMessage.Type switch
		{
			MessageType.Normal => "ℹ️",
			MessageType.Warning => "⚠️",
			MessageType.Error => "❗",
			MessageType.Critical => "🚨",
			_ => "❓"
		};
	}

	private ElementReference messagesContainer;
	private bool _unreadMessages;

	private async Task ScrollToBottomAsync()
	{
		if (_module is not null)
		{
			await _module.InvokeVoidAsync("scrollToBottom", messagesContainer);
			StateHasChanged();
		}
	}
}
