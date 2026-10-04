namespace PanoramicData.Blazor.Services;

/// <summary>
/// The conversations of <see cref="DumbChatService"/>: one transcript per conversation, and which one is active.
/// </summary>
public partial class DumbChatService
{
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
}
