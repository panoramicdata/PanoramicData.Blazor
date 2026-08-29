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

	/// <summary>
	/// One transcript per conversation, keyed by conversation id (issue #110).
	/// </summary>
	/// <remarks>
	/// Replaces the single message list this service used to hold. Keying by conversation is what lets the
	/// full-screen chat show several conversations at once: with one list, a reply arriving for any of them
	/// appended to whichever transcript happened to be selected, which is a misdelivery that depends on timing
	/// and presents as a rendering bug.
	/// </remarks>
	private readonly Dictionary<Guid, List<ChatMessage>> _conversations = new()
	{
		[ChatConversation.ImplicitConversationId] = []
	};

	private Guid _activeConversationId = ChatConversation.ImplicitConversationId;

	/// <inheritdoc />
	public event Action<ChatMessage>? OnMessageReceived;

	/// <inheritdoc />
	public event Action<Guid, ChatMessage>? OnConversationMessageReceived;
	/// <inheritdoc />
	public event Action<bool>? OnLiveStatusChanged;
	/// <inheritdoc />
	public event Action<PDChatDockMode>? OnDockModeChanged;
	/// <inheritdoc />
	public event Action<bool>? OnMuteStatusChanged;
	/// <inheritdoc />
	public event Action? OnConfigurationChanged;

	/// <inheritdoc />
	public IReadOnlyList<ChatMessage> Messages => GetMessages(_activeConversationId);

	/// <inheritdoc />
	/// <remarks>
	/// True, which is what makes this the worked example for the conversation-addressed API rather than a
	/// demonstration of the singular pattern that API exists to replace.
	/// </remarks>
	public bool SupportsConversations => true;

	/// <inheritdoc />
	/// <remarks>
	/// Setting this to a conversation the service does not have creates it, so that a consumer can select a
	/// conversation it has just learned about without a separate registration step. The alternative - throwing
	/// - would make the obvious call order wrong for no benefit, since a demo service has nothing to protect.
	/// </remarks>
	public Guid ActiveConversationId
	{
		get => _activeConversationId;
		set
		{
			EnsureConversation(value);
			_activeConversationId = value;
		}
	}

	/// <summary>
	/// Gets the ids of every conversation this service currently holds, oldest first.
	/// </summary>
	/// <remarks>
	/// Exposed so that a conversation history built over this service can list what exists without keeping a
	/// second, drifting copy of the same set.
	/// </remarks>
	public IReadOnlyCollection<Guid> ConversationIds => [.. _conversations.Keys];

	/// <summary>
	/// Creates a new, empty conversation and returns its id. Does not select it.
	/// </summary>
	/// <remarks>
	/// Selection is left to the caller because creating a conversation and switching to it are separate
	/// decisions: a conversation opened in a background tab is created but not selected.
	/// </remarks>
	public Guid CreateConversation()
	{
		var id = Guid.NewGuid();
		_conversations[id] = [];
		return id;
	}

	/// <summary>
	/// Ensures a conversation exists, creating an empty one if it does not.
	/// </summary>
	/// <param name="conversationId">The conversation to ensure.</param>
	/// <returns><c>true</c> if a conversation was created; <c>false</c> if it already existed.</returns>
	public bool EnsureConversation(Guid conversationId)
	{
		if (_conversations.ContainsKey(conversationId))
		{
			return false;
		}

		_conversations[conversationId] = [];
		return true;
	}

	/// <inheritdoc />
	/// <remarks>
	/// An unrecognised id yields nothing rather than the active transcript. Returning the wrong conversation
	/// would be indistinguishable, to a caller, from that conversation genuinely containing those messages.
	/// </remarks>
	public IReadOnlyList<ChatMessage> GetMessages(Guid conversationId)
		=> _conversations.TryGetValue(conversationId, out var messages) ? messages : [];

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

		// The periodic time check is ambient rather than a reply, so it lands in whatever conversation the
		// user is currently looking at.
		Deliver(_activeConversationId, message);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_timer?.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	/// <remarks>
	/// Clears the active conversation only. Clearing every conversation would make the button in one tab's
	/// header silently empty the others.
	/// </remarks>
	public void ClearMessages()
	{
		if (_conversations.TryGetValue(_activeConversationId, out var messages))
		{
			messages.Clear();
		}
	}

	/// <summary>
	/// Records a message against one conversation and tells subscribers about it.
	/// </summary>
	/// <param name="conversationId">The conversation the message belongs to.</param>
	/// <param name="chatMessage">The message.</param>
	/// <remarks>
	/// <para>
	/// The single place a message is delivered, so that the conversation it lands in and the conversation it
	/// is announced against cannot disagree. Every path that used to do
	/// <c>_messages.Add(x); OnMessageReceived?.Invoke(x);</c> goes through here instead - and there were nine
	/// of them, which is exactly how a reply ends up in the wrong transcript when one is edited and the others
	/// are not.
	/// </para>
	/// <para>
	/// <see cref="OnConversationMessageReceived"/> is raised for every message, whichever conversation it
	/// belongs to. <see cref="OnMessageReceived"/> is raised only for the active one, because a consumer using
	/// the singular API has no way to tell which conversation it was handed and would append a background
	/// conversation's reply to whatever it is showing.
	/// </para>
	/// </remarks>
	private void Deliver(Guid conversationId, ChatMessage chatMessage)
	{
		EnsureConversation(conversationId);
		_conversations[conversationId].Add(chatMessage);

		OnConversationMessageReceived?.Invoke(conversationId, chatMessage);

		if (conversationId == _activeConversationId)
		{
			OnMessageReceived?.Invoke(chatMessage);
		}
	}

	/// <inheritdoc />
	public void SendMessage(ChatMessage chatMessage) => SendMessage(_activeConversationId, chatMessage);

	/// <inheritdoc />
	/// <remarks>
	/// Throws for a conversation this service does not have, rather than falling back to the active one. A
	/// caller addressing a conversation that does not exist has a bug, and it should surface where it happened
	/// rather than as a message appearing in an unrelated conversation several seconds later.
	/// </remarks>
	public void SendMessage(Guid conversationId, ChatMessage chatMessage)
	{
		if (!_conversations.TryGetValue(conversationId, out var messages))
		{
			throw new InvalidOperationException(
				$"This chat service does not have conversation {conversationId}. " +
				$"Create it with {nameof(CreateConversation)} or {nameof(EnsureConversation)} first.");
		}

		// Add message to the conversation's message collection
		var existing = messages.FirstOrDefault(m => m.Id == chatMessage.Id);
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

			// An update is not a new message, so it is announced without being appended again.
			OnConversationMessageReceived?.Invoke(conversationId, chatMessage);

			if (conversationId == _activeConversationId)
			{
				OnMessageReceived?.Invoke(chatMessage);
			}
		}
		else
		{
			// Invoke the user message immediately
			Deliver(conversationId, chatMessage);
		}

		// Kick off the async reply workflow, on the conversation the message was sent to. Threading the id
		// through is what makes a slow reply arrive back where it was asked for: without it, a reply landed
		// wherever the user happened to be looking by the time it finished, which is precisely the case
		// conversation tabs make visible.
		_ = RespondAsync(conversationId, userMessage: chatMessage);
	}

	private async Task RespondAsync(Guid conversationId, ChatMessage userMessage)
	{
		// Ignore messages not from the user
		// Issue #106: answers to a form are not a fresh request, so they are acknowledged rather than
		// run through the keyword dispatch below. Echoing them back is merely noisy, but the dispatch
		// is worse: a question whose text happened to contain "form" or "question" would spawn another
		// form on submission, and then another. A submission must never be re-read as a new request.
		if (userMessage.FormSubmission is not null)
		{
			var answered = userMessage.FormSubmission.Answers.Count(answer => !answer.WasSkipped);

			var acknowledgement = new ChatMessage
			{
				Id = Guid.NewGuid(),
				Sender = DumbBot,
				Message = $"Thanks - I got {answered} of {userMessage.FormSubmission.Answers.Count} answers.",
				Type = MessageType.Success,
				Timestamp = DateTime.UtcNow
			};

			Deliver(conversationId, acknowledgement);

			return;
		}

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
		Deliver(conversationId, typingMessage);

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
			Deliver(conversationId, offlineMessage);

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
			Deliver(conversationId, backOnlineMessage);
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

			Deliver(conversationId, formMessage);

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
			Deliver(conversationId, helpMessage);
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
		Deliver(conversationId, finalResponse);
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
					MaximumLabel = "Strongly agree",

					// Named point by point, so the answer records "Agree" rather than "2".
					PointLabels = ["Strongly disagree", "Disagree", "Agree", "Strongly agree"]
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
					MaximumLabel = "Yes",
					PointLabels = ["No", "Yes"]
				}
			},
			new ChatFormQuestion
			{
				Id = "when",
				Header = "When",
				Question = "When did you last have some?",
				Kind = ChatFormAnswerKind.DateTime
			},
			new ChatFormQuestion
			{
				Id = "scoops",
				Header = "How many",
				Question = "How many scoops is the right number?",
				Kind = ChatFormAnswerKind.Number,
				Number = new ChatFormNumber { Minimum = 1, Maximum = 10, Unit = "scoops" }
			},
			new ChatFormQuestion
			{
				Id = "order",
				Header = "Order",
				Question = "Put these in order, best first.",
				Kind = ChatFormAnswerKind.Ranking,
				Options =
				[
					new ChatFormOption { Label = "Vanilla" },
					new ChatFormOption { Label = "Pistachio" },
					new ChatFormOption { Label = "Rum and raisin" }
				]
			},
			new ChatFormQuestion
			{
				Id = "ack",
				Header = "Agreed",
				Question = "One last thing.",
				Kind = ChatFormAnswerKind.Acknowledgement,
				Options = [new ChatFormOption { Label = "I accept that ice cream is not a breakfast food." }]
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
