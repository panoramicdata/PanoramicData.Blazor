namespace PanoramicData.Blazor.Models;

/// <summary>
/// One page of the conversations matching a <see cref="ChatConversationQuery"/>.
/// </summary>
/// <remarks>
/// A page rather than a bare list, because a sidebar has to know whether scrolling further will find anything
/// - and because a caller cannot infer that from a short result set: a store is entitled to return fewer
/// conversations than were asked for.
/// </remarks>
public class ChatConversationPage
{
	/// <summary>
	/// A page holding nothing, counted: the correct result for a search that matched no conversations.
	/// </summary>
	/// <remarks>
	/// Exposed so that a search returning nothing is an ordinary page rather than a null, an exception, or a
	/// fresh empty list at every call site. A zero-result search is a normal outcome and must not read as a
	/// failure anywhere along the path.
	/// </remarks>
	public static readonly ChatConversationPage Empty = new() { TotalCount = 0 };

	/// <summary>
	/// Gets the conversations in this page, ordered as the implementer intends them to be shown -
	/// conventionally by <see cref="ChatConversation.LastMessageUtc"/>, newest first.
	/// </summary>
	/// <remarks>
	/// Ordering belongs to the implementer rather than to the component because only the store can sort
	/// across pages. A component that re-sorted the page it was handed would produce a list ordered correctly
	/// within each page and wrongly across them, which looks like a rendering glitch and is not one.
	/// </remarks>
	public IReadOnlyList<ChatConversation> Conversations { get; init; } = [];

	/// <summary>
	/// Gets a value indicating whether more conversations match the query beyond this page.
	/// </summary>
	/// <remarks>
	/// Stated by the implementer rather than derived from the page size. A store is free to return fewer
	/// conversations than were asked for, so "fewer than <c>Take</c>" does not mean "no more" - inferring it
	/// would silently truncate a user's history at the first short page.
	/// </remarks>
	public bool HasMore { get; init; }

	/// <summary>
	/// Gets the total number of conversations matching the query, or <c>null</c> when the implementer did not
	/// count them.
	/// </summary>
	/// <remarks>
	/// <c>null</c> means <i>not counted</i> and is deliberately distinct from <c>0</c>, which means
	/// <i>counted, and there are none</i>. Collapsing the two would let a store that skipped an expensive
	/// count render as an empty history - which is exactly the reassurance that a user of a feature promising
	/// never to delete anything must not be given falsely. A consumer should show a count only when it has
	/// one.
	/// </remarks>
	public int? TotalCount { get; init; }
}
