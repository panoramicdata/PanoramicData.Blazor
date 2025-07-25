﻿namespace PanoramicData.Blazor;

public partial class PDChat : JSModuleComponentBase
{
	[EditorRequired]
	[Parameter]
	public required IChatService ChatService { get; set; }

	[EditorRequired]
	[Parameter]
	public required ChatMessageSender User { get; set; }

	/// <summary>
	/// Cascading parameter to get the parent chat container, if any.
	/// When present, dock mode changes will be automatically synchronized.
	/// </summary>
	[CascadingParameter(Name = "ChatContainer")]
	public PDChatContainer? Container { get; set; }

	[Parameter]
	public PDChatDockPosition ChatDockPosition { get; set; } = PDChatDockPosition.Right;

	[Parameter]
	public string CollapsedIcon { get; set; } = "💬";

	[Parameter]
	public Func<ChatMessage, string?>? UserIconSelector { get; set; }

	[Parameter]
	public Func<ChatMessage, string?>? PriorityIconSelector { get; set; }

	[Parameter]
	public Func<ChatMessage, string?>? SoundSelector { get; set; }

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

	private bool _isMuted;
	private bool _unreadMessages;
	private MessageType _highestPriorityUnreadMessage = MessageType.Normal;
	private DateTimeOffset _lastReadTimestamp = DateTimeOffset.UtcNow;
	private string _currentInput = "";

	private readonly List<ChatMessage> _messages = [];
	private PDTabSet? _tabSetRef;
	private PDChatDockMode? _restoreDockMode;

	protected override string ModulePath => "./_content/PanoramicData.Blazor/PDChat.razor.js";

	protected override Task OnInitializedAsync()
	{
		// Load existing messages from the service
		_messages.Clear();
		_messages.AddRange(ChatService.Messages);

		// Sync local mute state with service
		_isMuted = ChatService.IsMuted;

		ChatService.OnMessageReceived += OnMessageReceived;
		ChatService.OnLiveStatusChanged += OnLiveStatusChanged;
		ChatService.OnDockModeChanged += OnServiceDockModeChanged;
		ChatService.OnMuteStatusChanged += OnServiceMuteStatusChanged;
		ChatService.OnConfigurationChanged += OnServiceConfigurationChanged;
		ChatService.Initialize();
		return base.OnInitializedAsync();
	}

	/// <summary>
	/// Change the dock mode state with proper state tracking.
	/// </summary>
	private async Task ChangeDockModeAsync(PDChatDockMode newMode)
	{
		if (ChatService.DockMode is not PDChatDockMode.Minimized and not PDChatDockMode.FullScreen)
		{
			_restoreDockMode = ChatService.DockMode;
		}

		// Notify container if there is one, and let it handle the changes
		if (Container is not null)
		{
			await Container.OnInternalDockModeChanged(newMode);
			return;
		}

		ChatService.DockMode = newMode;

		StateHasChanged();
	}

	private void OnLiveStatusChanged(bool obj)
	{
		StateHasChanged();
	}

	private void OnServiceDockModeChanged(PDChatDockMode newDockMode)
		=> _ = ChangeDockModeAsync(newDockMode);

	private void OnServiceMuteStatusChanged(bool isMuted)
	{
		// Sync local mute state with service
		_isMuted = isMuted;
		StateHasChanged();
	}

	private void OnServiceConfigurationChanged()
	{
		// Configuration changed, trigger UI update and ensure parameters are synchronized
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

		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			_unreadMessages = true;

			// Update the highest priority unread message type
			if (isNewMessage && message.Type != MessageType.Typing)
			{
				UpdateHighestPriorityUnreadMessage();
			}

			// Optionally auto-restore the chat when new messages arrive
			if (ChatService.AutoRestoreOnNewMessage && isNewMessage && message.Type != MessageType.Typing)
			{
				await ChangeDockModeAsync(ChatService.RestoreMode);
				_unreadMessages = false;
				_highestPriorityUnreadMessage = MessageType.Normal;
				_lastReadTimestamp = DateTimeOffset.UtcNow;

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

	private async Task ToggleChatAsync()
	{
		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			// Restore to last normal state
			await ChangeDockModeAsync(ChatService.RestoreMode);
			_unreadMessages = false;
			_highestPriorityUnreadMessage = MessageType.Normal;
			_lastReadTimestamp = DateTimeOffset.UtcNow;

			if (OnChatRestored.HasDelegate)
			{
				await OnChatRestored.InvokeAsync();
			}
		}
		else
		{
			// Minimize
			await ChangeDockModeAsync(PDChatDockMode.Minimized);

			if (OnChatMinimized.HasDelegate)
			{
				await OnChatMinimized.InvokeAsync();
			}
		}
	}

	private async Task ToggleMuteAsync()
	{
		// Update both local and service mute state
		_isMuted = !_isMuted;
		ChatService.IsMuted = _isMuted;

		// Emit mute toggle event
		if (OnMuteToggled.HasDelegate)
		{
			await OnMuteToggled.InvokeAsync();
		}
	}

	private async Task ToggleFullScreenAsync()
	{
		if (ChatService.DockMode == PDChatDockMode.FullScreen)
		{
			// Restore to last normal state
			await ChangeDockModeAsync(ChatService.RestoreMode);

			if (OnChatRestored.HasDelegate)
			{
				await OnChatRestored.InvokeAsync();
			}
		}
		else
		{
			// Maximize to fullscreen
			await ChangeDockModeAsync(PDChatDockMode.FullScreen);

			if (OnChatMaximized.HasDelegate)
			{
				await OnChatMaximized.InvokeAsync();
			}
		}
	}

	private async Task ClearChatAsync()
	{
		// Clear messages from both local collection and service
		_messages.Clear();
		ChatService.ClearMessages();
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

	private async Task DockToSideAsync()
	{
		// Determine which side to dock to based on current position
		await ChangeDockModeAsync(ChatService.DockMode switch
		{
			PDChatDockMode.TopRight or PDChatDockMode.BottomRight => PDChatDockMode.Right,
			PDChatDockMode.TopLeft or PDChatDockMode.BottomLeft => PDChatDockMode.Left,
			_ => PDChatDockMode.Right // Default to right for other cases
		});
	}

	private async Task UnpinFromSideAsync()
	{
		// Restore to last normal state (should be a corner position)
		await ChangeDockModeAsync(_restoreDockMode ?? ChatService.RestoreMode);

		if (OnChatRestored.HasDelegate)
		{
			await OnChatRestored.InvokeAsync();
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

			newTab.ChildContent = builder =>
			{
				builder.OpenComponent<PDMonacoEditor>(0);
				builder.AddAttribute(1, "Language", "csharp");
				builder.AddAttribute(2, "Theme", "vs-dark");
				builder.AddAttribute(3, "InitializeOptions", new Func<BlazorMonaco.Editor.StandaloneEditorConstructionOptions>(() =>
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
		// If minimized, always use minimized logic regardless of container state
		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			// When minimized, use the service's MinimizedButtonPosition to position the button
			var buttonPositionClass = ChatService.MinimizedButtonPosition switch
			{
				PDChatButtonPosition.BottomRight => "dock-bottom-right",
				PDChatButtonPosition.TopRight => "dock-top-right",
				PDChatButtonPosition.BottomLeft => "dock-bottom-left",
				PDChatButtonPosition.TopLeft => "dock-top-left",
				PDChatButtonPosition.None => "dock-none", // Hide the button completely
				_ => "dock-bottom-right" // Default fallback
			};
			return $"{buttonPositionClass} dock-minimized";
		}

		// Check if we're in a container that's handling split mode
		if (Container?.IsSplitMode == true && (ChatService.DockMode == PDChatDockMode.Left || ChatService.DockMode == PDChatDockMode.Right))
		{
			return "dock-split-panel";
		}

		return ChatService.DockMode switch
		{
			PDChatDockMode.BottomRight => "dock-bottom-right",
			PDChatDockMode.TopRight => "dock-top-right",
			PDChatDockMode.BottomLeft => "dock-bottom-left",
			PDChatDockMode.TopLeft => "dock-top-left",
			PDChatDockMode.FullScreen => "dock-fullscreen",
			PDChatDockMode.Left => "dock-left",
			PDChatDockMode.Right => "dock-right",
			_ => "dock-bottom-right" // Default fallback
		};
	}

	public override async ValueTask DisposeAsync()
	{
		// Clean up event handlers
		ChatService.OnMessageReceived -= OnMessageReceived;
		ChatService.OnLiveStatusChanged -= OnLiveStatusChanged;
		ChatService.OnDockModeChanged -= OnServiceDockModeChanged;
		ChatService.OnMuteStatusChanged -= OnServiceMuteStatusChanged;
		ChatService.OnConfigurationChanged -= OnServiceConfigurationChanged;

		await base.DisposeAsync();
	}
}
