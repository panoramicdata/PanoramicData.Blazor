using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Demo.Services;

/// <summary>
/// An in-memory conversation history for the demo, backed by the same <see cref="DumbChatService"/> that
/// holds the transcripts (issue #111, MS-25787).
/// </summary>
/// <remarks>
/// <para>
/// Deliberately <b>one store seen two ways</b> rather than two stores kept in step: the metadata here and the
/// transcripts in the chat service are the same conversations, so message counts and previews are derived
/// from the chat service on every read rather than cached. A second copy would drift the moment a reply
/// arrived, and the symptom - a list saying four messages beside a transcript showing five - would look like
/// a rendering bug.
/// </para>
/// <para>
/// A real host would scope every result to the signed-in user. This one has no users, so it does not; see
/// <see cref="IChatConversationService"/>, which says plainly that the component cannot do that scoping and
/// will render whatever it is handed.
/// </para>
/// <para>
/// Keyword search only. Reporting <see cref="SupportsSemanticSearch"/> as <c>false</c> is not a limitation
/// being worked around - it is what lets the demo show that the semantic option is hidden rather than greyed
/// when a store has no embedding model behind it.
/// </para>
/// </remarks>
public class DemoChatConversationService : IChatConversationService
{
	private readonly DumbChatService _chatService;
	private readonly Dictionary<Guid, ConversationRecord> _records = [];

	/// <summary>
	/// Creates the history and seeds it with a few conversations, so that the demo has a list worth looking
	/// at before anybody has typed anything.
	/// </summary>
	/// <param name="chatService">The chat service holding the transcripts.</param>
	public DemoChatConversationService(DumbChatService chatService)
	{
		_chatService = chatService;

		// Seeded with enough variety to exercise the list: a titled conversation, an untitled one that has to
		// fall back to its preview, and an archived one that is invisible until "Include archived" is ticked.
		Seed("Guest network SSID audit", TimeSpan.FromMinutes(4), isArchived: false,
			("Which SSIDs are broadcasting on the guest network?", "Three: Guest, Guest-5G and Events."),
			("Are any of them open?", "Events is open. The other two are WPA2."));

		Seed(title: null, TimeSpan.FromHours(3), isArchived: false,
			("Investigate INC0010004", "That incident is a failed nightly backup on pdl-app-04."));

		Seed("Switch firmware rollout", TimeSpan.FromDays(9), isArchived: true,
			("Did the firmware rollout finish?", "Yes - all twelve switches are on 7.14.3."));

		// The conversation the chat service starts life with, so that whatever the user types before touching
		// the sidebar is a conversation like any other rather than an orphan the list cannot show.
		_records[ChatConversation.ImplicitConversationId] = new ConversationRecord
		{
			Id = ChatConversation.ImplicitConversationId,
			Title = "Current conversation",
			CreatedUtc = DateTimeOffset.UtcNow,
			LastMessageUtc = DateTimeOffset.UtcNow
		};
	}

	/// <inheritdoc />
	/// <remarks>
	/// False, so the demo shows the semantic search option being hidden rather than offered against a store
	/// that could not answer it.
	/// </remarks>
	public bool SupportsSemanticSearch => false;

	/// <inheritdoc />
	public Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken)
	{
		var matching = _records.Values
			.Where(record => query.IncludeArchived || !record.IsArchived)
			.Select(ToConversation)
			.Where(conversation => Matches(conversation, query))
			.OrderByDescending(conversation => conversation.LastMessageUtc)
			.ToList();

		var page = matching.Skip(query.Skip).Take(query.Take).ToList();

		return Task.FromResult(new ChatConversationPage
		{
			Conversations = page,
			HasMore = query.Skip + page.Count < matching.Count,
			TotalCount = matching.Count
		});
	}

	/// <inheritdoc />
	public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken)
		=> Task.FromResult(_chatService.GetMessages(id));

	/// <inheritdoc />
	public Task<ChatConversation> CreateAsync(CancellationToken cancellationToken)
	{
		var id = _chatService.CreateConversation();

		var record = new ConversationRecord
		{
			Id = id,
			CreatedUtc = DateTimeOffset.UtcNow,
			LastMessageUtc = DateTimeOffset.UtcNow
		};

		_records[id] = record;

		return Task.FromResult(ToConversation(record));
	}

	/// <inheritdoc />
	public Task RenameAsync(Guid id, string title, CancellationToken cancellationToken)
	{
		if (_records.TryGetValue(id, out var record))
		{
			record.Title = title;
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task ArchiveAsync(Guid id, CancellationToken cancellationToken) => SetArchived(id, isArchived: true);

	/// <inheritdoc />
	public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken) => SetArchived(id, isArchived: false);

	private Task SetArchived(Guid id, bool isArchived)
	{
		if (_records.TryGetValue(id, out var record))
		{
			record.IsArchived = isArchived;
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Builds the summary the sidebar renders, reading counts and preview from the chat service so the two
	/// views of a conversation cannot disagree.
	/// </summary>
	private ChatConversation ToConversation(ConversationRecord record)
	{
		var messages = _chatService.GetMessages(record.Id);

		return new ChatConversation
		{
			Id = record.Id,
			Title = record.Title,
			CreatedUtc = record.CreatedUtc,
			LastMessageUtc = messages.Count > 0 ? messages[^1].Timestamp : record.LastMessageUtc,
			IsArchived = record.IsArchived,
			MessageCount = messages.Count,
			Preview = messages.Count > 0 ? messages[0].Message : null
		};
	}

	/// <summary>
	/// Keyword matching over the title, the preview and the transcript.
	/// </summary>
	/// <remarks>
	/// Searching the transcript rather than only the title is what makes the demo honest: the interesting
	/// case for a conversation list is finding the conversation in which somebody mentioned a hostname, and a
	/// title-only search would appear to work while never finding one.
	/// </remarks>
	private bool Matches(ChatConversation conversation, ChatConversationQuery query)
	{
		if (!query.HasSearchText)
		{
			return true;
		}

		var term = query.SearchText!.Trim();

		return conversation.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
			|| _chatService.GetMessages(conversation.Id)
				.Any(message => message.Message.Contains(term, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Creates a conversation with a ready-made exchange in it, so the seeded list has transcripts to open
	/// and text to search rather than empty rows.
	/// </summary>
	private void Seed(string? title, TimeSpan age, bool isArchived, params (string User, string Bot)[] exchanges)
	{
		var id = _chatService.CreateConversation();
		var timestamp = DateTimeOffset.UtcNow - age;

		foreach (var (user, bot) in exchanges)
		{
			// Sent straight into the conversation's transcript rather than through SendMessage, which would
			// start the simulated reply workflow and answer these with "You said: ..." a second later.
			_chatService.SeedMessage(id, new ChatMessage
			{
				Id = Guid.NewGuid(),
				Sender = DemoUser,
				Message = user,
				Type = MessageType.Normal,
				Timestamp = timestamp
			});

			_chatService.SeedMessage(id, new ChatMessage
			{
				Id = Guid.NewGuid(),
				Sender = DumbChatService.DumbBot,
				Message = bot,
				Type = MessageType.Normal,
				Timestamp = timestamp.AddSeconds(20)
			});

			timestamp = timestamp.AddMinutes(1);
		}

		_records[id] = new ConversationRecord
		{
			Id = id,
			Title = title,
			CreatedUtc = DateTimeOffset.UtcNow - age,
			LastMessageUtc = timestamp,
			IsArchived = isArchived
		};
	}

	private static ChatMessageSender DemoUser { get; } = new()
	{
		// Matches the sender MainLayout hands PDChat, so a seeded exchange and a live one do not show two
		// different names for the same person.
		Name = "User",
		IsUser = true,
		IsHuman = true
	};

	/// <summary>
	/// What this store owns about a conversation: the things the chat service cannot tell it.
	/// </summary>
	/// <remarks>
	/// Message count, preview and last-activity are deliberately absent - they are derived from the
	/// transcript on read. Only the title and the archived flag actually live here.
	/// </remarks>
	private sealed class ConversationRecord
	{
		public required Guid Id { get; init; }

		public string? Title { get; set; }

		public DateTimeOffset CreatedUtc { get; init; }

		public DateTimeOffset LastMessageUtc { get; init; }

		public bool IsArchived { get; set; }
	}
}
