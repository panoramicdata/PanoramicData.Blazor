namespace PanoramicData.Blazor;

public partial class PDChat
{
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

	[Parameter]
	public bool IsMaximizePermitted { get; set; }

	[Parameter]
	public bool IsCanvasUsePermitted { get; set; }

	private IJSObjectReference? _module;
	private bool _isOpen;
	private bool _isMuted;
	private bool _isMaximized;
	private bool _unreadMessages;
	private string _currentInput = "";

	private readonly List<ChatMessage> _messages = [];
	private PDTabSet? _tabSetRef;
	private PDMessages? _pdMessages;

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

	private void OnMessageReceived(ChatMessage message)
	{
		var existing = _messages.FirstOrDefault(m => m.Id == message.Id);
		if (existing != null)
		{
			existing.Message = message.Message;
			existing.Type = message.Type;
			existing.Title = message.Title;
			existing.Timestamp = message.Timestamp;
			existing.IsTitleHtml = message.IsTitleHtml;
			existing.IsMessageHtml = message.IsMessageHtml;
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
		}
	}

	private void ToggleMute()
	{
		_isMuted = !_isMuted;
	}

	private void ToggleMaximize()
	{
		_isMaximized = !_isMaximized;
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
	}

	private bool CanSend => ChatService.IsLive && !string.IsNullOrWhiteSpace(_currentInput);

	private void OnTabAdded()
	{
		if (_tabSetRef is not null)
		{
			var newTab = new PDTab
			{
				Title = "New Tab",
				IsRenamingEnabled = true
			};
			_tabSetRef.AddTab(newTab);
			_tabSetRef.StartRenamingTab(newTab);
		}
	}
}
