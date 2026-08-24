namespace PanoramicData.Blazor.Services;

/// <summary>
/// Demo chat service that simulates responses and online/offline behavior.
/// </summary>
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
	private bool _isInputPermitted = true;
	private string? _inputDisabledMessage;
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
	private bool _toastEnabled = true;
	private double _toastDisplayDurationSeconds = 5.0;
	private PDChatToastAnimation _toastEntryAnimation = PDChatToastAnimation.Grow;
	private PDChatToastAnimation _toastExitAnimation = PDChatToastAnimation.Shrink;
	private double _toastAnimationDurationMs = 250d;
	private bool _toastAutoDismiss = true;
	private bool _toastShowTitle = true;
	private string _toastMinWidth = "200px";
	private string _toastMaxWidth = "300px";
	private string _toastMinHeight = string.Empty;
	private string _toastMaxHeight = string.Empty;
	private int _toastMaxVisible = 5;
	private PDChatButtonPosition _toastAnchor = PDChatButtonPosition.BottomRight;

	private readonly List<ChatMessage> _messages = [];

	/// <inheritdoc />
	public event Action<ChatMessage>? OnMessageReceived;
	/// <inheritdoc />
	public event Action<bool>? OnLiveStatusChanged;
	/// <inheritdoc />
	public event Action<PDChatDockMode>? OnDockModeChanged;
	/// <inheritdoc />
	public event Action<bool>? OnMuteStatusChanged;
	/// <inheritdoc />
	public event Action? OnConfigurationChanged;

	/// <inheritdoc />
	public IReadOnlyList<ChatMessage> Messages => _messages;

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
	public bool IsInputPermitted
	{
		get => _isInputPermitted;
		set
		{
			if (_isInputPermitted != value)
			{
				_isInputPermitted = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public string? InputDisabledMessage
	{
		get => _inputDisabledMessage;
		set
		{
			if (_inputDisabledMessage != value)
			{
				_inputDisabledMessage = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
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

	/// <inheritdoc />
	[Obsolete("Superseded by ToastEnabled.")]
	public bool ShowLastMessage
	{
		get => ToastEnabled;
		set => ToastEnabled = value;
	}

	/// <inheritdoc />
	[Obsolete("Superseded by ToastDisplayDurationSeconds.")]
	public double ShowLastMessageDurationSeconds
	{
		get => ToastDisplayDurationSeconds;
		set => ToastDisplayDurationSeconds = value;
	}

	/// <inheritdoc />
	public bool ToastEnabled
	{
		get => _toastEnabled;
		set
		{
			if (_toastEnabled != value)
			{
				_toastEnabled = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public double ToastDisplayDurationSeconds
	{
		get => _toastDisplayDurationSeconds;
		set
		{
			if (Math.Abs(_toastDisplayDurationSeconds - value) > 0.001)
			{
				_toastDisplayDurationSeconds = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public PDChatToastAnimation ToastEntryAnimation
	{
		get => _toastEntryAnimation;
		set
		{
			if (_toastEntryAnimation != value)
			{
				_toastEntryAnimation = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public PDChatToastAnimation ToastExitAnimation
	{
		get => _toastExitAnimation;
		set
		{
			if (_toastExitAnimation != value)
			{
				_toastExitAnimation = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public double ToastAnimationDurationMs
	{
		get => _toastAnimationDurationMs;
		set
		{
			if (Math.Abs(_toastAnimationDurationMs - value) > 0.001)
			{
				_toastAnimationDurationMs = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public bool ToastAutoDismiss
	{
		get => _toastAutoDismiss;
		set
		{
			if (_toastAutoDismiss != value)
			{
				_toastAutoDismiss = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public bool ToastShowTitle
	{
		get => _toastShowTitle;
		set
		{
			if (_toastShowTitle != value)
			{
				_toastShowTitle = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public string ToastMinWidth
	{
		get => _toastMinWidth;
		set
		{
			if (_toastMinWidth != value)
			{
				_toastMinWidth = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public string ToastMaxWidth
	{
		get => _toastMaxWidth;
		set
		{
			if (_toastMaxWidth != value)
			{
				_toastMaxWidth = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public string ToastMinHeight
	{
		get => _toastMinHeight;
		set
		{
			if (_toastMinHeight != value)
			{
				_toastMinHeight = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public string ToastMaxHeight
	{
		get => _toastMaxHeight;
		set
		{
			if (_toastMaxHeight != value)
			{
				_toastMaxHeight = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public int ToastMaxVisible
	{
		get => _toastMaxVisible;
		set
		{
			if (_toastMaxVisible != value)
			{
				_toastMaxVisible = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <inheritdoc />
	public PDChatButtonPosition ToastAnchor
	{
		get => _toastAnchor;
		set
		{
			if (_toastAnchor != value)
			{
				_toastAnchor = value;
				OnConfigurationChanged?.Invoke();
			}
		}
	}

	/// <summary>
	/// Gets the predefined sender used for periodic time-check messages.
	/// </summary>
	public static ChatMessageSender TimeBot { get; } = new()
	{
		Name = "TimeBot",
		IsUser = false,
		IsHuman = false,
		IsSupport = false
	};

	/// <summary>
	/// Gets the predefined sender used for automated demo replies.
	/// </summary>
	public static ChatMessageSender DumbBot { get; } = new()
	{
		Name = "DumbBot",
		IsUser = false,
		IsHuman = false,
		IsSupport = false
	};

	/// <inheritdoc />
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

	/// <inheritdoc />
	public bool IsLive => _isInitialized && _isOnline;

	/// <inheritdoc />
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

	/// <inheritdoc />
	public void Dispose()
	{
		_timer?.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	public void ClearMessages()
	{
		_messages.Clear();
	}

	/// <inheritdoc />
	public void SendMessage(ChatMessage chatMessage)
	{
		// Add message to service's message collection
		var existing = _messages.FirstOrDefault(m => m.Id == chatMessage.Id);
		if (existing != null)
		{
			// Update existing message
			existing.Message = chatMessage.Message;
			existing.Type = chatMessage.Type;
			existing.Title = chatMessage.Title;
			existing.Timestamp = chatMessage.Timestamp;
			existing.IsTitleHtml = chatMessage.IsTitleHtml;
			existing.IsMessageHtml = chatMessage.IsMessageHtml;
			// Issue #106: copied by hand like the rest, so a new payload must be added here too.
			existing.Form = chatMessage.Form;
			existing.FormSubmission = chatMessage.FormSubmission;
		}
		else
		{
			// Add new message
			_messages.Add(chatMessage);
		}

		// Invoke the user message immediately
		OnMessageReceived?.Invoke(chatMessage);

		// Kick off the async reply workflow
		_ = RespondAsync(chatMessage);
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

		// Issue #106: "form" always produces a question form, so the inline form can be exercised
		// without an AI behind the chat.
		if (userMessage.Message.Contains("form", StringComparison.OrdinalIgnoreCase)
			|| userMessage.Message.Contains("question", StringComparison.OrdinalIgnoreCase))
		{
			var formMessage = new ChatMessage
			{
				Id = Guid.NewGuid(),
				Sender = DumbBot,
				Message = "A few quick questions - answer what you like and skip the rest.",
				Type = MessageType.Form,
				Timestamp = DateTime.UtcNow,
				Form = BuildDemonstrationForm()
			};

			_messages.Add(formMessage);
			OnMessageReceived?.Invoke(formMessage);

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

	/// <summary>
	/// Updates the dock mode and raises the dock-mode-changed event when needed.
	/// </summary>
	/// <param name="newMode">The requested dock mode.</param>
	/// <returns>A completed task.</returns>
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

	/// <summary>
	/// A form exercising every answer kind, for the demo (issue #106).
	/// </summary>
	/// <remarks>
	/// Deliberately one of each: single choice with descriptions, multiple choice with "Other", two
	/// differently-shaped scales, and both text sizes including a pre-filled draft. If a change to
	/// the form renderer breaks any kind, typing "form" into the demo shows it immediately.
	/// </remarks>
	public static ChatForm BuildDemonstrationForm() => new()
	{
		Id = Guid.NewGuid(),
		Title = "Tell us about ice cream",
		Questions =
		[
			new ChatFormQuestion
			{
				Id = "flavours",
				Header = "Flavours",
				Question = "Which flavours of ice cream do you like?",
				Kind = ChatFormAnswerKind.MultipleChoice,
				AllowOther = true,
				Options =
				[
					new ChatFormOption { Label = "Vanilla", Description = "The one everything else is measured against" },
					new ChatFormOption { Label = "Pistachio", Description = "Green, expensive, worth it" },
					new ChatFormOption { Label = "Rum and raisin", Description = "Divisive" }
				]
			},
			new ChatFormQuestion
			{
				Id = "favourite",
				Header = "Favourite",
				Question = "Which is your favourite?",
				Kind = ChatFormAnswerKind.SingleChoice,
				AllowOther = true,
				Options =
				[
					new ChatFormOption { Label = "Vanilla", Description = "Reliable" },
					new ChatFormOption { Label = "Pistachio", Description = "Green, expensive, worth it" },
					new ChatFormOption { Label = "Rum and raisin", Description = "Divisive" }
				]
			},
			new ChatFormQuestion
			{
				Id = "agreement",
				Header = "Agreement",
				Question = "Ice cream is better than cake.",
				Kind = ChatFormAnswerKind.Scale,
				Scale = new ChatFormScale
				{
					Minimum = 1,
					Maximum = 4,
					MinimumLabel = "Strongly disagree",
					MaximumLabel = "Strongly agree"
				}
			},
			new ChatFormQuestion
			{
				Id = "again",
				Header = "Yes/No",
				Question = "Would you eat ice cream again today?",
				Kind = ChatFormAnswerKind.Scale,
				Scale = new ChatFormScale
				{
					Minimum = 0,
					Maximum = 1,
					MinimumLabel = "No",
					MaximumLabel = "Yes"
				}
			},
			new ChatFormQuestion
			{
				Id = "shop",
				Header = "Shop",
				Question = "Which shop do you buy it from?",
				Kind = ChatFormAnswerKind.Text
			},
			new ChatFormQuestion
			{
				Id = "summary",
				Header = "Summary",
				Question = "Here is a suggested summary - please edit it.",
				Kind = ChatFormAnswerKind.Text,
				IsMultiline = true,
				SuggestedValue = "I like several flavours, pistachio most of all, and I would happily "
					+ "eat more today."
			}
		]
	};
}
