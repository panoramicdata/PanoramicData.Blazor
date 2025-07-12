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
	public PDChatDockMode DockMode { get; set; } = PDChatDockMode.BottomRight;

	[Parameter]
	public EventCallback<PDChatDockMode> DockModeChanged { get; set; }

	[Parameter]
	public PDChatDockPosition ChatDockPosition { get; set; } = PDChatDockPosition.Right;

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

	[Parameter]
	public bool IsClearPermitted { get; set; } = true;

	private IJSObjectReference? _module;
	private bool _isOpen;
	private bool _isMuted;
	private bool _unreadMessages;
	private string _currentInput = "";
	private PDChatDockMode _previousDockMode = PDChatDockMode.BottomRight; // Store previous dock mode

	private readonly List<ChatMessage> _messages = [];
	private PDTabSet? _tabSetRef;
	private PDMessages? _pdMessages;

	// Helper property to check if in fullscreen mode
	private bool IsFullScreen => DockMode == PDChatDockMode.FullScreen;

	protected override Task OnInitializedAsync()
	{
		// Initialize previous dock mode with current value
		_previousDockMode = DockMode;
		
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

	private async Task ToggleFullScreenAsync()
	{
		if (DockMode == PDChatDockMode.FullScreen)
		{
			// Restore to previous dock mode
			await SetDockModeAsync(_previousDockMode);
		}
		else
		{
			// Store current dock mode before going fullscreen
			_previousDockMode = DockMode;
			await SetDockModeAsync(PDChatDockMode.FullScreen);
		}
	}

	private void ClearChat()
	{
		_messages.Clear();
		_currentInput = string.Empty;
		StateHasChanged();
	}

	private async Task SetDockModeAsync(PDChatDockMode mode)
	{
		if (DockMode != mode)
		{
			DockMode = mode;
			await DockModeChanged.InvokeAsync(mode);
			StateHasChanged();
		}
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

	// Helper method to get CSS classes for dock mode positioning
	private string GetDockModeClasses()
	{
		return DockMode switch
		{
			PDChatDockMode.BottomRight => "dock-bottom-right",
			PDChatDockMode.TopRight => "dock-top-right",
			PDChatDockMode.BottomLeft => "dock-bottom-left",
			PDChatDockMode.TopLeft => "dock-top-left",
			PDChatDockMode.FullScreen => "dock-fullscreen",
			_ => "dock-bottom-right" // Default fallback
		};
	}

	// Helper method to get the appropriate split direction based on chat dock position
	private SplitDirection GetSplitDirection()
	{
		return ChatDockPosition switch
		{
			PDChatDockPosition.Left or PDChatDockPosition.Right => SplitDirection.Horizontal,
			PDChatDockPosition.Top or PDChatDockPosition.Bottom => SplitDirection.Vertical,
			_ => SplitDirection.Horizontal // Default to horizontal
		};
	}

	// Helper method to determine if chat should be the first panel
	private bool IsChatFirstPanel()
	{
		return ChatDockPosition switch
		{
			PDChatDockPosition.Left => true,
			PDChatDockPosition.Top => true,
			PDChatDockPosition.Right => false,
			PDChatDockPosition.Bottom => false,
			_ => false // Default chat on the right (second panel)
		};
	}
}
