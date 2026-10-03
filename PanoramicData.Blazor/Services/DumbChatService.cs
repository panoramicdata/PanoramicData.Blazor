using System;

namespace PanoramicData.Blazor.Services;

/// <summary>
/// Demo chat service that simulates responses and online/offline behavior.
/// </summary>
public partial class DumbChatService : IChatService, IDisposable
{
	private bool _isInitialized;
	private bool _isOnline;
	private Timer? _timer;
	private PDChatDockMode _preferredDockMode = PDChatDockMode.Right;

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
	public event Action<PDChatDockMode>? OnDockModeChanged;

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

	/// <summary>
	/// Adds a message to a conversation's transcript without answering it or announcing it.
	/// </summary>
	/// <param name="conversationId">The conversation to add to; created if it does not exist.</param>
	/// <param name="chatMessage">The message to record.</param>
	/// <remarks>
	/// For building a conversation that is supposed to look as though it already happened - a demo history,
	/// or a test's starting state. Deliberately not <see cref="SendMessage(Guid, ChatMessage)"/>: that starts
	/// the simulated reply workflow, so seeding a past exchange through it would have the bot answer every
	/// seeded question with "You said: ..." a second after the page loaded.
	///
	/// It also raises no events, because nothing was received - a subscriber told about a message from last
	/// Tuesday would reasonably toast it.
	/// </remarks>
	public void SeedMessage(Guid conversationId, ChatMessage chatMessage)
	{
		EnsureConversation(conversationId);
		_conversations[conversationId].Add(chatMessage);
	}

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
		_timer = new Timer(_ => SendTimeCheck(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
		_isInitialized = true;
		_isOnline = true;
	}

	/// <inheritdoc />
	public bool IsLive => _isInitialized && _isOnline;

	/// <inheritdoc />
	public PDChatDockMode DockMode { get; set; }

	/// <summary>
	/// Delivers a time check from <see cref="TimeBot"/>; called by the periodic timer started in <see cref="Initialize"/>.
	/// </summary>
	internal void SendTimeCheck()
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
		var messages = _conversations[conversationId];

		// A message id is an identity, not a sequence number: a "Typing..." placeholder and the answer that
		// replaces it deliberately share one, so that a consumer can swap the first for the second. The
		// service therefore has to replace in place too. Appending both left the placeholder in the stored
		// transcript for ever - invisible while PDChat kept its own de-duplicated copy, and plainly wrong the
		// moment a conversation tab read the transcript back from here.
		var existing = messages.FirstOrDefault(message => message.Id == chatMessage.Id);
		if (existing is null)
		{
			messages.Add(chatMessage);
		}
		else
		{
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
		if (!_conversations.ContainsKey(conversationId))
		{
			throw new InvalidOperationException(
				$"This chat service does not have conversation {conversationId}. " +
				$"Create it with {nameof(CreateConversation)} or {nameof(EnsureConversation)} first.");
		}

		// Record the user's message and tell subscribers immediately. Deliver handles the add-or-update, so
		// there is one implementation of what a repeated message id means rather than two that can disagree.
		Deliver(conversationId, chatMessage);

		// Kick off the async reply workflow, on the conversation the message was sent to. Threading the id
		// through is what makes a slow reply arrive back where it was asked for: without it, a reply landed
		// wherever the user happened to be looking by the time it finished, which is precisely the case
		// conversation tabs make visible.
		_ = RespondAsync(conversationId, userMessage: chatMessage);
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
}
