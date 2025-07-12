namespace PanoramicData.Blazor;

public partial class PDChat : JSModuleComponentBase
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

	[Parameter]
	public bool AutoRestoreOnNewMessage { get; set; } = false;

	[Parameter]
	public EventCallback OnChatMinimized { get; set; }

	[Parameter]
	public EventCallback OnChatRestored { get; set; }

	[Parameter]
	public EventCallback OnChatMaximized { get; set; }

	[Parameter]
	public EventCallback OnMuteToggled { get; set; }

	[Parameter]
	public EventCallback OnChatCleared { get; set; }

	[Parameter]
	public EventCallback<ChatMessage> OnMessageSent { get; set; }

	[Parameter]
	public EventCallback<ChatMessage> OnMessageReceivedEvent { get; set; }

	[Parameter]
	public EventCallback OnAutoRestored { get; set; }

	[Parameter]
	public bool UseFullWidthMessages { get; set; } = true;

	[Parameter]
	public MessageMetadataDisplayMode MessageMetadataDisplayMode { get; set; } = MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;

	[Parameter]
	public bool ShowMessageUserIcon { get; set; } = true;

	[Parameter]
	public bool ShowMessageUserName { get; set; } = true;

	[Parameter]
	public bool ShowMessageTimestamp { get; set; } = true;

	[Parameter]
	public string MessageTimestampFormat { get; set; } = "HH:mm:ss";

	private bool _isMuted;
	private bool _unreadMessages;
	private MessageType _highestPriorityUnreadMessage = MessageType.Normal;
	private DateTimeOffset _lastReadTimestamp = DateTimeOffset.UtcNow;
	private string _currentInput = "";
	private PDChatDockMode _previousDockMode = PDChatDockMode.BottomRight; // Store previous dock mode for minimize/restore
	private PDChatDockMode _lastNonMaximizedMode = PDChatDockMode.BottomRight; // Store last non-fullscreen dock mode for maximize/restore

	private readonly List<ChatMessage> _messages = [];
	private PDTabSet? _tabSetRef;
	private PDMessages? _pdMessages;

	protected override string ModulePath => "./_content/PanoramicData.Blazor/PDChat.razor.js";

	protected override Task OnInitializedAsync()
	{
		// Initialize previous dock mode and last non-maximized mode with current value
		_previousDockMode = DockMode;
		_lastNonMaximizedMode = DockMode;

		ChatService.OnMessageReceived += OnMessageReceived;
		ChatService.OnLiveStatusChanged += OnLiveStatusChanged;
		ChatService.Initialize();
		return base.OnInitializedAsync();
	}

	private void OnLiveStatusChanged(bool obj)
	{
		StateHasChanged();
	}

	private async void OnMessageReceived(ChatMessage message)
	{
		var existing = _messages.FirstOrDefault(m => m.Id == message.Id);
		var isNewMessage = existing == null;

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

		// Emit OnMessageReceived event for new messages
		if (isNewMessage && OnMessageReceivedEvent.HasDelegate)
		{
			await OnMessageReceivedEvent.InvokeAsync(message);
		}

		if (DockMode == PDChatDockMode.Minimized)
		{
			_unreadMessages = true;

			// Update the highest priority unread message type
			if (isNewMessage && message.Type != MessageType.Typing)
			{
				UpdateHighestPriorityUnreadMessage();
			}

			// Optionally auto-restore the chat when new messages arrive
			// Only auto-restore for new messages (not updates) and if it's not a typing indicator
			if (AutoRestoreOnNewMessage && isNewMessage && message.Type != MessageType.Typing)
			{
				await SetDockModeAsync(_previousDockMode);
				_unreadMessages = false; // Clear unread flag since we're opening the chat
				_highestPriorityUnreadMessage = MessageType.Normal; // Reset priority
				_lastReadTimestamp = DateTimeOffset.UtcNow; // Update last read time

				// Emit auto-restore event
				if (OnAutoRestored.HasDelegate)
				{
					await OnAutoRestored.InvokeAsync();
				}
			}
		}

		// Get the sound to play, if any
		var soundUrlString = SoundSelector?.Invoke(message);
		if (!string.IsNullOrWhiteSpace(soundUrlString) && !_isMuted)
		{
			var soundUrl = new Uri(soundUrlString, UriKind.RelativeOrAbsolute);
			// Play the sound
			Module?.InvokeVoidAsync("playSound", soundUrlString);
		}

		await InvokeAsync(StateHasChanged);
	}

	public void ShowToast(ChatMessage message)
	{
		_messages.Add(message);
		InvokeAsync(StateHasChanged);
	}

	private async Task ToggleChatAsync()
	{
		if (DockMode == PDChatDockMode.Minimized)
		{
			// Restore from minimized state to previous dock mode
			await SetDockModeAsync(_previousDockMode);
			_unreadMessages = false; // Clear unread messages when chat is opened
			_highestPriorityUnreadMessage = MessageType.Normal; // Reset priority
			_lastReadTimestamp = DateTimeOffset.UtcNow; // Update last read time

			// Emit restore event
			if (OnChatRestored.HasDelegate)
			{
				await OnChatRestored.InvokeAsync();
			}
		}
		else
		{
			// Save current dock mode before minimizing (for minimize/restore cycle)
			_previousDockMode = DockMode;
			await SetDockModeAsync(PDChatDockMode.Minimized);

			// Emit minimize event
			if (OnChatMinimized.HasDelegate)
			{
				await OnChatMinimized.InvokeAsync();
			}
		}
	}

	private async Task ToggleMuteAsync()
	{
		_isMuted = !_isMuted;

		// Emit mute toggle event
		if (OnMuteToggled.HasDelegate)
		{
			await OnMuteToggled.InvokeAsync();
		}
	}

	private async Task ToggleFullScreenAsync()
	{
		if (DockMode == PDChatDockMode.FullScreen)
		{
			// Restore to last non-maximized dock mode, fallback to BottomRight if not set
			var targetMode = IsNormalDockMode(_lastNonMaximizedMode) ? _lastNonMaximizedMode : PDChatDockMode.BottomRight;
			await SetDockModeAsync(targetMode);

			// Emit restore event
			if (OnChatRestored.HasDelegate)
			{
				await OnChatRestored.InvokeAsync();
			}
		}
		else
		{
			// Store current dock mode as last non-maximized before going full screen
			// Only store if it's a normal dock mode (not Minimized)
			if (IsNormalDockMode(DockMode))
			{
				_lastNonMaximizedMode = DockMode;
			}
			await SetDockModeAsync(PDChatDockMode.FullScreen);

			// Emit maximize event
			if (OnChatMaximized.HasDelegate)
			{
				await OnChatMaximized.InvokeAsync();
			}
		}
	}

	private async Task ClearChatAsync()
	{
		_messages.Clear();
		_currentInput = string.Empty;
		_unreadMessages = false;
		_highestPriorityUnreadMessage = MessageType.Normal;
		_lastReadTimestamp = DateTimeOffset.UtcNow;
		StateHasChanged();

		// Emit chat cleared event
		if (OnChatCleared.HasDelegate)
		{
			await OnChatCleared.InvokeAsync();
		}
	}

	private async Task SetDockModeAsync(PDChatDockMode mode)
	{
		if (DockMode != mode)
		{
			// Track the last non-maximized mode when changing away from normal dock modes
			if (IsNormalDockMode(DockMode))
			{
				_lastNonMaximizedMode = DockMode;
			}

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

		// Emit message sent event
		if (OnMessageSent.HasDelegate)
		{
			await OnMessageSent.InvokeAsync(message);
		}

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

			// Add PDMonaco editor as child content when in fullscreen mode
			if (DockMode == PDChatDockMode.FullScreen)
			{
				newTab.ChildContent = builder =>
				{
					builder.OpenComponent<PDMonacoEditor>(0);
					builder.AddAttribute(1, "Language", "csharp");
					builder.AddAttribute(2, "Theme", "vs-dark");
					builder.AddAttribute(3, "CssClass", "pdchat-canvas-tab-content");
					builder.AddAttribute(4, "InitializeOptions", new Func<BlazorMonaco.Editor.StandaloneEditorConstructionOptions>(() =>
						new BlazorMonaco.Editor.StandaloneEditorConstructionOptions
						{
							AutomaticLayout = true,
							Language = "csharp",
							Theme = "vs-dark",
							Value = "// Welcome to the Monaco Editor!\n// Start coding here...\n",
							Minimap = new BlazorMonaco.Editor.EditorMinimapOptions { Enabled = false }
						}));
					builder.CloseComponent();
				};
			}

			_tabSetRef.AddTab(newTab);
			_tabSetRef.StartRenamingTab(newTab);
		}
	}

	// Helper method to update the highest priority unread message
	private void UpdateHighestPriorityUnreadMessage()
	{
		if (!_unreadMessages || !_messages.Any())
		{
			_highestPriorityUnreadMessage = MessageType.Normal;
			return;
		}

		// Get the highest priority message that arrived after the last read timestamp
		// and exclude typing messages
		var unreadNonTypingMessages = _messages
			.Where(m => m.Type != MessageType.Typing && m.Timestamp > _lastReadTimestamp)
			.ToList();

		if (!unreadNonTypingMessages.Any())
		{
			_highestPriorityUnreadMessage = MessageType.Normal;
			return;
		}

		_highestPriorityUnreadMessage = unreadNonTypingMessages
			.Select(m => m.Type)
			.Max(); // MessageType enum values are ordered by priority
	}

	// Helper method to get the bootstrap color class based on message priority
	private string GetBootstrapColorClass()
	{
		if (!ChatService.IsLive)
		{
			return "pdchat-not-live";
		}

		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Critical => "pdchat-critical",
			MessageType.Error => "pdchat-error",
			MessageType.Warning => "pdchat-warning",
			MessageType.Normal => "pdchat-info",
			MessageType.Typing => string.Empty,
			_ => string.Empty
		};
	}

	// Helper method to get the animation class based on message priority
	private string GetAnimationClass()
	{
		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Critical => "pulsate-critical",
			MessageType.Error => "pulsate-error",
			MessageType.Warning => "pulsate-warning",
			MessageType.Normal => "pulsate",
			MessageType.Typing => string.Empty,
			_ => string.Empty
		};
	}

	// Helper method to get the priority indicator icon
	private string GetPriorityIndicator()
	{
		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Warning => "⚠",
			MessageType.Error => "!",
			MessageType.Critical => "!!",
			_ => string.Empty
		};
	}

	// Helper method to get CSS classes for dock mode positioning
	private string GetDockModeClasses()
	{
		if (DockMode == PDChatDockMode.Minimized)
		{
			// When minimized, use the previous dock mode's positioning class plus minimized
			var previousClass = _previousDockMode switch
			{
				PDChatDockMode.BottomRight => "dock-bottom-right",
				PDChatDockMode.TopRight => "dock-top-right",
				PDChatDockMode.BottomLeft => "dock-bottom-left",
				PDChatDockMode.TopLeft => "dock-top-left",
				PDChatDockMode.FullScreen => "dock-bottom-right", // Default fallback for fullscreen
				PDChatDockMode.Left => "dock-left",
				PDChatDockMode.Right => "dock-right",
				PDChatDockMode.Top => "dock-top",
				PDChatDockMode.Bottom => "dock-bottom",
				_ => "dock-bottom-right" // Default fallback
			};
			return $"{previousClass} dock-minimized";
		}

		return DockMode switch
		{
			PDChatDockMode.BottomRight => "dock-bottom-right",
			PDChatDockMode.TopRight => "dock-top-right",
			PDChatDockMode.BottomLeft => "dock-bottom-left",
			PDChatDockMode.TopLeft => "dock-top-left",
			PDChatDockMode.FullScreen => "dock-fullscreen",
			PDChatDockMode.Left => "dock-left",
			PDChatDockMode.Right => "dock-right",
			PDChatDockMode.Top => "dock-top",
			PDChatDockMode.Bottom => "dock-bottom",
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

	// Helper method to check if a dock mode is a "normal" docked position (not minimized or fullscreen)
	private static bool IsNormalDockMode(PDChatDockMode mode)
	{
		return mode != PDChatDockMode.Minimized && mode != PDChatDockMode.FullScreen;
	}
}
