namespace PanoramicData.Blazor.Services;

public class DumbChatService : IChatService, IDisposable
{
	private static readonly Random _random = new();
	private static readonly MessageType[] _messageTypes = [.. Enum.GetValues<MessageType>().Except([MessageType.Typing])];
	private bool _isInitialized;
	private bool _isOnline;
	private Timer? _timer;
	private PDChatDockMode _preferredDockMode = PDChatDockMode.Right;
	private bool _isMuted;
	private bool _isMaximizePermitted = true;
	private bool _isCanvasUsePermitted = true;
	private bool _isClearPermitted = true;
	private bool _autoRestoreOnNewMessage;
	private bool _useFullWidthMessages = true;
	private MessageMetadataDisplayMode _messageMetadataDisplayMode = MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;
	private bool _showMessageUserIcon = true;
	private bool _showMessageUserName = true;
	private bool _showMessageTimestamp = true;
	private string _messageTimestampFormat = "HH:mm:ss";
	private string _title = "Demo Chat";
	private PDChatDockMode _restoreMode = PDChatDockMode.BottomRight;
	private PDChatButtonPosition _minimizedButtonPosition = PDChatButtonPosition.BottomRight;
	private bool _showLastMessage = true;
	private double _showLastMessageDurationSeconds = 5.0;

	private readonly List<ChatMessage> _messages = [];

	public event Action<ChatMessage>? OnMessageReceived;
	public event Action<bool>? OnLiveStatusChanged;
	public event Action<PDChatDockMode>? OnDockModeChanged;
	public event Action<bool>? OnMuteStatusChanged;
	public event Action? OnConfigurationChanged;

	public IReadOnlyList<ChatMessage> Messages => _messages;

	public PDChatDockMode PreferredDockMode
	{
		get => _preferredDockMode;
		set
		{
			if (_preferredDockMode != value)
			{
				_preferredDockMode = value;
				OnDockModeChanged?.Invoke(value);
			}
		}
	}

	public PDChatDockMode RestoreMode
	{
		get => _restoreMode;
		set
		{
			if (_restoreMode != value)
			{
				_restoreMode = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public PDChatButtonPosition MinimizedButtonPosition
	{
		get => _minimizedButtonPosition;
		set
		{
			if (_minimizedButtonPosition != value)
			{
				_minimizedButtonPosition = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool IsMuted
	{
		get => _isMuted;
		set
		{
			if (_isMuted != value)
			{
				_isMuted = value;
				OnMuteStatusChanged?.Invoke(value);
			}
		}
	}

	public string Title
	{
		get => _title;
		set
		{
			if (_title != value)
			{
				_title = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool IsMaximizePermitted
	{
		get => _isMaximizePermitted;
		set
		{
			if (_isMaximizePermitted != value)
			{
				_isMaximizePermitted = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool IsCanvasUsePermitted
	{
		get => _isCanvasUsePermitted;
		set
		{
			if (_isCanvasUsePermitted != value)
			{
				_isCanvasUsePermitted = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool IsClearPermitted
	{
		get => _isClearPermitted;
		set
		{
			if (_isClearPermitted != value)
			{
				_isClearPermitted = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool AutoRestoreOnNewMessage
	{
		get => _autoRestoreOnNewMessage;
		set
		{
			if (_autoRestoreOnNewMessage != value)
			{
				_autoRestoreOnNewMessage = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool UseFullWidthMessages
	{
		get => _useFullWidthMessages;
		set
		{
			if (_useFullWidthMessages != value)
			{
				_useFullWidthMessages = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public MessageMetadataDisplayMode MessageMetadataDisplayMode
	{
		get => _messageMetadataDisplayMode;
		set
		{
			if (_messageMetadataDisplayMode != value)
			{
				_messageMetadataDisplayMode = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool ShowMessageUserIcon
	{
		get => _showMessageUserIcon;
		set
		{
			if (_showMessageUserIcon != value)
			{
				_showMessageUserIcon = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool ShowMessageUserName
	{
		get => _showMessageUserName;
		set
		{
			if (_showMessageUserName != value)
			{
				_showMessageUserName = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool ShowMessageTimestamp
	{
		get => _showMessageTimestamp;
		set
		{
			if (_showMessageTimestamp != value)
			{
				_showMessageTimestamp = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public string MessageTimestampFormat
	{
		get => _messageTimestampFormat;
		set
		{
			if (_messageTimestampFormat != value)
			{
				_messageTimestampFormat = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public bool ShowLastMessage
	{
		get => _showLastMessage;
		set
		{
			if (_showLastMessage != value)
			{
				_showLastMessage = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public double ShowLastMessageDurationSeconds
	{
		get => _showLastMessageDurationSeconds;
		set
		{
			if (Math.Abs(_showLastMessageDurationSeconds - value) > 0.001)
			{
				_showLastMessageDurationSeconds = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	public static ChatMessageSender TimeBot { get; } = new()
	{
		Name = "TimeBot",
		IsUser = false,
		IsHuman = false,
		IsSupport = false
	};

	public static ChatMessageSender DumbBot { get; } = new()
	{
		Name = "DumbBot",
		IsUser = false,
		IsHuman = false,
		IsSupport = false
	};

	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}

		// Start the timer to send a time check every minute
		_timer = new Timer(SendTimeCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
		_isInitialized = true;
		_isOnline = true;
	}

	public bool IsLive => _isInitialized && _isOnline;

	public PDChatDockMode DockMode { get; set; }

	private void SendTimeCheck(object? state)
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = TimeBot,
			Title = "Time Check",
			Message = $"The current time is {DateTime.Now:T}",
			Type = MessageType.Normal,
			Timestamp = DateTimeOffset.Now
		};

		// Add to service's message collection
		_messages.Add(message);

		OnMessageReceived?.Invoke(message);
	}

	public void Dispose()
	{
		_timer?.Dispose();
		GC.SuppressFinalize(this);
	}

	public void ClearMessages()
	{
		_messages.Clear();
	}

	public void SendMessage(ChatMessage message)
	{
		// Add message to service's message collection
		var existing = _messages.FirstOrDefault(m => m.Id == message.Id);
		if (existing != null)
		{
			// Update existing message
			existing.Message = message.Message;
			existing.Type = message.Type;
			existing.Title = message.Title;
			existing.Timestamp = message.Timestamp;
			existing.IsTitleHtml = message.IsTitleHtml;
			existing.IsMessageHtml = message.IsMessageHtml;
		}
		else
		{
			// Add new message
			_messages.Add(message);
		}

		// Invoke the user message immediately
		OnMessageReceived?.Invoke(message);

		// Kick off the async reply workflow
		_ = RespondAsync(message);
	}

	private async Task RespondAsync(ChatMessage userMessage)
	{
		// Ignore messages not from the user
		if (!userMessage.Sender.IsUser)
		{
			return;
		}

		// Create a shared GUID for both typing and final messages
		var responseId = Guid.NewGuid();

		// Send the "typing" message (empty content)
		var typingMessage = new ChatMessage
		{
			Id = responseId,
			Sender = DumbBot,
			Title = "Typing...",
			Message = "...",
			Type = MessageType.Typing,
		};

		// Wait 500-1000ms to simulate "delayed response"
		await Task.Delay(_random.Next(500, 3000));

		// Add typing message to collection temporarily
		_messages.Add(typingMessage);
		OnMessageReceived?.Invoke(typingMessage);

		// Wait for 1-2 seconds to simulate "typing"
		await Task.Delay(_random.Next(1000, 2000));

		// Add some basic behaviours if the user message contains certain keywords:
		// - "away" or "offline" or "bio-break", simulate going offline for 10 second.
		// - "help", provide a list of commands.
		// - otherwise echo what the user typed.

		if (userMessage.Message.Contains("away", StringComparison.OrdinalIgnoreCase) ||
			userMessage.Message.Contains("offline", StringComparison.OrdinalIgnoreCase) ||
			userMessage.Message.Contains("bio-break", StringComparison.OrdinalIgnoreCase))
		{
			// Simulate going offline for 10 seconds
			_isOnline = false;
			OnLiveStatusChanged?.Invoke(_isOnline);
			var offlineMessage = new ChatMessage
			{
				Id = responseId,
				Sender = DumbBot,
				Title = "Going Offline",
				Message = "I'm going offline for a short break. Please wait...",
				Type = MessageType.Warning
			};
			_messages.Add(offlineMessage);
			OnMessageReceived?.Invoke(offlineMessage);

			await Task.Delay(10000);

			// After the break, come back online
			_isOnline = true;
			OnLiveStatusChanged?.Invoke(_isOnline);
			var backOnlineMessage = new ChatMessage
			{
				Id = Guid.NewGuid(),
				Sender = DumbBot,
				Title = "Back Online",
				Message = "I'm back online! How can I assist you?",
				Type = MessageType.Normal
			};
			_messages.Add(backOnlineMessage);
			OnMessageReceived?.Invoke(backOnlineMessage);
			return;
		}

		if (userMessage.Message.Contains("help", StringComparison.OrdinalIgnoreCase))
		{
			// Provide a list of commands
			var helpMessage = new ChatMessage
			{
				Id = responseId,
				Sender = DumbBot,
				Title = "<b>Help</b>",
				IsTitleHtml = true,
				Message = "Available commands: <ul><li>help</li><li>go away</li></ul>",
				IsMessageHtml = true,
				Type = MessageType.Normal
			};
			_messages.Add(helpMessage);
			OnMessageReceived?.Invoke(helpMessage);
			return; // No further response needed
		}

		// Send the final response with same Id so UI can replace
		// Generate random type for the response
		var finalResponse = new ChatMessage
		{
			Id = responseId,
			Sender = DumbBot,
			Message = $"You said: \"{userMessage.Message}\"",
			Type = _messageTypes[_random.Next(_messageTypes.Length)]
		};
		_messages.Add(finalResponse);
		OnMessageReceived?.Invoke(finalResponse);
	}

	public Task SetDockModeAsync(PDChatDockMode newMode)
	{
		if (DockMode == newMode)
		{
			return Task.CompletedTask;
		}

		DockMode = newMode;
		OnDockModeChanged?.Invoke(DockMode);
		return Task.CompletedTask;
	}
}
