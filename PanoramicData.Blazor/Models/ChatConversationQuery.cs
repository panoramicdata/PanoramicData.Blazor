namespace PanoramicData.Blazor.Models;

/// <summary>
/// What a caller is asking <c>IChatConversationService.ListAsync</c> for: which conversations, matched how,
/// and how many of them.
/// </summary>
/// <remarks>
/// <para>
/// A single parameter object rather than a long argument list, so that adding a way to narrow the list later
/// does not break every implementer. The properties are <c>init</c>-only because a query is passed to an
/// implementation that may hold it across an await; one that could be mutated afterwards would let a caller
/// change the meaning of a request already in flight.
/// </para>
/// <para>
/// <b>Scoping to the current user is not expressed here, and cannot be.</b> See
/// <c>IChatConversationService</c>: the implementer is responsible for it, and the component renders whatever
/// it is handed.
/// </para>
/// </remarks>
public class ChatConversationQuery
{
	/// <summary>
	/// The number of conversations a query asks for when its caller does not say.
	/// </summary>
	/// <remarks>
	/// Enough to fill a sidebar at any supported height without a second round trip, and few enough that a
	/// user with years of history does not pay for all of it to open a list.
	/// </remarks>
	public const int DefaultTake = 50;

	/// <summary>
	/// Gets the text to match, or <c>null</c> when the caller wants the unfiltered list.
	/// </summary>
	/// <remarks>
	/// Use <see cref="HasSearchText"/> rather than testing this for emptiness: a debounced search box hands
	/// over whatever is in it, which after a backspace is routinely a single space.
	/// </remarks>
	public string? SearchText { get; init; }

	/// <summary>
	/// Gets how <see cref="SearchText"/> is to be matched. Defaults to
	/// <see cref="ChatConversationSearchMode.Keyword"/>.
	/// </summary>
	/// <remarks>
	/// Keyword is the default because it is the mode every implementer can support. Defaulting to semantic
	/// would fail a host that has no embedding model, on a query that never asked to be semantic.
	/// </remarks>
	public ChatConversationSearchMode SearchMode { get; init; } = ChatConversationSearchMode.Keyword;

	/// <summary>
	/// Gets a value indicating whether archived conversations are included. Defaults to <c>false</c>.
	/// </summary>
	/// <remarks>
	/// The default is the load-bearing half of this property. Archived means hidden from the default list, so
	/// a caller who has not thought about archiving must get the list a user would expect. Defaulting the
	/// other way would make archiving do nothing until every call site opted out of it.
	/// </remarks>
	public bool IncludeArchived { get; init; }

	/// <summary>Gets the number of matching conversations to skip. Defaults to none.</summary>
	public int Skip { get; init; }

	/// <summary>
	/// Gets the maximum number of conversations to return. Defaults to <see cref="DefaultTake"/>.
	/// </summary>
	/// <remarks>
	/// Deliberately not defaulted to zero. A careful implementer would read zero as "take none" and render an
	/// empty sidebar; a careless one would read it as "take all" and fetch every transcript the user has ever
	/// had. Neither is what a caller who left it alone meant.
	/// </remarks>
	public int Take { get; init; } = DefaultTake;

	/// <summary>
	/// Gets a value indicating whether <see cref="SearchText"/> holds something worth searching for.
	/// </summary>
	/// <remarks>
	/// Whitespace is not a search. Treating it as one costs a semantic implementation an embedding call and
	/// returns nothing.
	/// </remarks>
	public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
}
