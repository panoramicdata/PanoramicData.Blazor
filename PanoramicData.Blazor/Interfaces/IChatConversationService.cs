using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// An optional contract a host application implements to give <see cref="PDChat"/> a conversation history:
/// a list of previous conversations that can be searched, opened, renamed and archived.
/// </summary>
/// <remarks>
/// <para>
/// <b>Optional means optional.</b> A host that does not supply one gets exactly today's <see cref="PDChat"/> -
/// no sidebar, no conversation tabs, no toolbar, and no code path that could show them. Nothing about the
/// docked chat changes.
/// </para>
/// <para>
/// <b>Why this is not more members on <see cref="IChatService"/>.</b> That interface is already over sixty
/// members. Adding list, search, archive, rename, import and export to it would oblige every implementer -
/// including the trivial test doubles that exist only to hand back a canned reply - to implement storage they
/// have not got. Keeping the two apart also keeps the storage concern out of a component library that has no
/// business owning it.
/// </para>
/// <para>
/// <b>There is no <c>DeleteAsync</c>, and there will not be one.</b> A transcript is a record of what an
/// assistant told somebody, so a conversation is archived - see <see cref="ArchiveAsync"/> - and never
/// removed. The guarantee is enforced by the absence of the method rather than by a UI that hides a button,
/// because a UI-only guarantee lasts exactly until the next person adds a convenience call. There is a test
/// that fails if any member here, or on any type in this feature, acquires a name suggesting removal.
/// </para>
/// <para>
/// <b>Scoping results to the current user is the implementer's responsibility.</b> The component cannot do it
/// and will render whatever it is handed: it has no notion of who is signed in, and no way to tell one user's
/// conversation from another's. An implementation that returns unscoped rows will show one user another
/// user's transcripts, and will do so without any error.
/// </para>
/// <para>
/// Every method takes a <see cref="CancellationToken"/> because the conversation sidebar's search is debounced
/// and superseded as the user keeps typing. A semantic search is a network call plus an embedding; issuing one
/// per keystroke and cancelling none of them queues work behind a result the user has already moved past.
/// </para>
/// </remarks>
public interface IChatConversationService
{
	/// <summary>
	/// Gets a value indicating whether this implementation can match on meaning as well as on wording.
	/// Defaults to <c>false</c>.
	/// </summary>
	/// <remarks>
	/// A consumer must check this before offering
	/// <see cref="ChatConversationSearchMode.Semantic"/>. The choice is then hidden rather than disabled,
	/// because a control that is permanently greyed teaches the reader nothing about why.
	/// </remarks>
	bool SupportsSemanticSearch => false;

	/// <summary>
	/// Lists the conversations matching a query.
	/// </summary>
	/// <param name="query">Which conversations, matched how, and how many.</param>
	/// <param name="cancellationToken">Cancels a search the user has already typed past.</param>
	/// <returns>
	/// The matching page, which is <see cref="ChatConversationPage.Empty"/> when nothing matched. A search
	/// that finds nothing is an ordinary outcome and must not be reported as a failure.
	/// </returns>
	/// <remarks>
	/// Results are expected to be scoped to the current user and ordered by
	/// <see cref="ChatConversation.LastMessageUtc"/>, newest first: the conversation a user wants next is
	/// almost always the one they last said something in, which is not necessarily the newest one.
	/// </remarks>
	Task<ChatConversationPage> ListAsync(ChatConversationQuery query, CancellationToken cancellationToken);

	/// <summary>
	/// Gets the full transcript of one conversation.
	/// </summary>
	/// <param name="id">The conversation to read.</param>
	/// <param name="cancellationToken">Cancels a read whose conversation is no longer being shown.</param>
	/// <returns>The messages, oldest first, or an empty list if the conversation has none.</returns>
	/// <remarks>
	/// Separate from <see cref="ListAsync"/> so that a list of two hundred conversations does not fetch two
	/// hundred transcripts. <see cref="ChatConversation.Preview"/> and
	/// <see cref="ChatConversation.MessageCount"/> carry enough for a list row without this call.
	/// </remarks>
	Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid id, CancellationToken cancellationToken);

	/// <summary>
	/// Creates a new, empty conversation.
	/// </summary>
	/// <param name="cancellationToken">Cancels the creation.</param>
	/// <returns>The conversation created, whose <see cref="ChatConversation.Id"/> the caller then addresses.</returns>
	/// <remarks>
	/// A consumer is expected to call this lazily - on the first message rather than when the user clicks
	/// <i>new</i> - so that somebody who opens a conversation and changes their mind does not litter a history
	/// that nothing can subsequently delete.
	/// </remarks>
	Task<ChatConversation> CreateAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Sets a conversation's title.
	/// </summary>
	/// <param name="id">The conversation to rename.</param>
	/// <param name="title">The new title.</param>
	/// <param name="cancellationToken">Cancels the rename.</param>
	/// <remarks>
	/// Renaming a conversation to a blank title titles it as blank; it does not return it to the untitled
	/// state. See <see cref="ChatConversation.Title"/> for why that distinction is load-bearing for a host
	/// that generates titles in the background.
	/// </remarks>
	Task RenameAsync(Guid id, string title, CancellationToken cancellationToken);

	/// <summary>
	/// Archives a conversation, hiding it from a list that has not asked for archived conversations.
	/// </summary>
	/// <param name="id">The conversation to archive.</param>
	/// <param name="cancellationToken">Cancels the archive.</param>
	/// <remarks>
	/// Archived is hidden, never removed and never unreadable. This is the nearest thing the contract has to
	/// a delete, and it is reversible by <see cref="UnarchiveAsync"/> - archiving without a way back would be
	/// deletion wearing a different name.
	/// </remarks>
	Task ArchiveAsync(Guid id, CancellationToken cancellationToken);

	/// <summary>
	/// Returns an archived conversation to the default list.
	/// </summary>
	/// <param name="id">The conversation to un-archive.</param>
	/// <param name="cancellationToken">Cancels the un-archive.</param>
	Task UnarchiveAsync(Guid id, CancellationToken cancellationToken);
}
