namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents one conversation - a named, orderable collection of <see cref="ChatMessage"/> - so that a host
/// application can offer <see cref="PanoramicData.Blazor.PDChat"/> a history of previous and current chats
/// rather than a single flat message list.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately a plain model with no service dependency. A host backs it with whatever store it already has,
/// and the component renders what it is handed; see <c>IChatConversationService</c> for the contract between
/// the two.
/// </para>
/// <para>
/// <b>There is no delete.</b> Neither here nor on the service that returns these. A transcript is a record of
/// what an assistant told somebody, and a conversation is archived rather than removed - see
/// <see cref="IsArchived"/>.
/// </para>
/// </remarks>
public class ChatConversation
{
	/// <summary>
	/// The name shown for a conversation that has neither a title nor anything said in it yet.
	/// </summary>
	/// <remarks>
	/// Exposed rather than inlined so that a caller wanting to sort or filter untitled conversations, or a test
	/// asserting on one, does not have to hard-code the same string and drift from it later.
	/// </remarks>
	public const string UntitledDisplayName = "New conversation";

	/// <summary>
	/// The identifier standing for the single conversation held by a chat service that does not support
	/// conversations - see <c>IChatService.SupportsConversations</c>.
	/// </summary>
	/// <remarks>
	/// A fixed, well-known value so that a consumer written against the conversation-addressed API has one id it
	/// can always address, whether or not the service behind it knows what a conversation is. It is
	/// deliberately not <see cref="Guid.Empty"/>: empty is what an uninitialised field holds, so a bug that
	/// forgot to set an id would silently address the implicit conversation and appear to work.
	/// </remarks>
	public static readonly Guid ImplicitConversationId = new("9f2a4c3e-7b1d-4a6f-8c05-1e3d5a7b9c11");

	/// <summary>
	/// Gets the conversation's unique identifier.
	/// </summary>
	/// <remarks>
	/// A <see cref="Guid"/> rather than an integer because this is the value that ends up in front of a browser.
	/// A sequential integer would let anyone read a colleague's conversation by subtracting one.
	/// </remarks>
	public required Guid Id { get; init; }

	/// <summary>
	/// Gets or sets the conversation's title, or <c>null</c> when it has not been titled.
	/// </summary>
	/// <remarks>
	/// <c>null</c> means <i>not yet titled</i> and is deliberately distinct from an empty string, which means
	/// <i>titled, as blank</i>. A host that generates titles in the background finds its work by looking for the
	/// former; if the two were the same value it would either re-title every conversation on every pass or never
	/// re-title any of them. Use <see cref="IsTitled"/> rather than testing for emptiness.
	/// </remarks>
	public string? Title { get; set; }

	/// <summary>Gets or sets the UTC time at which the conversation was started.</summary>
	public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets or sets the UTC time of the most recent message in the conversation.
	/// </summary>
	/// <remarks>
	/// A conversation list sorts on this rather than on <see cref="CreatedUtc"/>: the conversation a user wants
	/// next is almost always the one they last said something in, which is not necessarily the newest one.
	/// </remarks>
	public DateTimeOffset LastMessageUtc { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Gets or sets a value indicating whether the conversation has been archived.
	/// </summary>
	/// <remarks>
	/// Archived means hidden from the default list, never removed and never unreadable. Interacting with an
	/// archived conversation is expected to un-archive it, so that picking a conversation back up does not need
	/// a second deliberate action.
	/// </remarks>
	public bool IsArchived { get; set; }

	/// <summary>Gets or sets the number of messages in the conversation.</summary>
	/// <remarks>Carried on the summary so that a list can render without fetching every transcript.</remarks>
	public int MessageCount { get; set; }

	/// <summary>
	/// Gets or sets a short extract of the conversation - conventionally its opening message - or <c>null</c>
	/// when there is nothing to show.
	/// </summary>
	public string? Preview { get; set; }

	/// <summary>
	/// Gets a value indicating whether a title has been set, whatever its content.
	/// </summary>
	/// <remarks>
	/// Distinguishes "not yet titled" from "titled as blank". See <see cref="Title"/> for why that distinction
	/// is load-bearing rather than pedantic.
	/// </remarks>
	public bool IsTitled => Title is not null;

	/// <summary>
	/// Gets the name to show for this conversation: its title, falling back to its preview, falling back to
	/// <see cref="UntitledDisplayName"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The rule lives here, on the model, so that a conversation list and a conversation tab cannot disagree
	/// about what the same conversation is called. Two independently-written fallbacks would drift, and the
	/// symptom would be a user renaming something in one place and still seeing the old name in the other.
	/// </para>
	/// <para>
	/// A whitespace-only title falls back as though it were absent. It stays distinguishable to
	/// <see cref="IsTitled"/>, because the titling job needs to know it was set, but it is not something a
	/// reader can pick out of a list.
	/// </para>
	/// </remarks>
	public string DisplayName
		=> string.IsNullOrWhiteSpace(Title)
			? string.IsNullOrWhiteSpace(Preview) ? UntitledDisplayName : Preview
			: Title;
}
