using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor;

/// <summary>
/// The conversation list shown beside the transcript in full-screen chat: search, filter to include
/// archived conversations, and open one (issue #111, MS-25787).
/// </summary>
/// <remarks>
/// <para>
/// Rendered only by <see cref="PDChat"/>, only in full-screen, and only when the host supplied an
/// <see cref="IChatConversationService"/>. Restricting it to full-screen costs nothing and avoids the
/// alternative: a conversation list crammed into a 320px dock.
/// </para>
/// <para>
/// <b>Search is debounced and cancellable.</b> Semantic search is a network call plus an embedding, so firing
/// one per keystroke would queue work the user has already typed past. Each keystroke supersedes the one
/// before it, and the in-flight request is cancelled rather than left to land out of order - two searches
/// racing would otherwise let the earlier, longer one overwrite the later result, which presents as a list
/// that ignores the last thing you typed.
/// </para>
/// </remarks>
public partial class PDChatConversationSidebar : ComponentBase, IDisposable
{
	/// <summary>
	/// How long to wait after the last keystroke before searching.
	/// </summary>
	/// <remarks>
	/// Long enough to swallow a burst of typing, short enough not to feel like a pause. This is the value
	/// that decides whether a semantic search costs one embedding per query or one per character.
	/// </remarks>
	private const int SearchDebounceMilliseconds = 300;

	private readonly List<ChatConversation> _conversations = [];

	private CancellationTokenSource? _searchCancellation;
	private string _searchText = string.Empty;
	private ChatConversationSearchMode _searchMode = ChatConversationSearchMode.Keyword;
	private bool _includeArchived;
	private bool _isLoading;
	private bool _hasMore;
	private bool _hasSearchText;
	private string? _loadFailureMessage;
	private bool _isDisposed;

	/// <summary>Gets or sets the conversation store to list.</summary>
	[EditorRequired]
	[Parameter]
	public required IChatConversationService ConversationService { get; set; }

	/// <summary>Gets or sets the conversation currently being shown, so that its row reads as selected.</summary>
	[Parameter]
	public Guid? SelectedConversationId { get; set; }

	/// <summary>Raised when the user picks a conversation to open.</summary>
	[Parameter]
	public EventCallback<ChatConversation> OnConversationSelected { get; set; }

	/// <summary>Raised after each successful load, with the conversations now listed.</summary>
	/// <remarks>
	/// Exists so that <see cref="PDChat"/> can open the conversation the chat service is already on into a
	/// tab as soon as it knows what that conversation is called. Without it, entering full-screen showed a
	/// sidebar row highlighted as selected beside a pane saying no conversation was open - the component
	/// knew the id but had no title to put on a tab, so it put nothing.
	/// </remarks>
	[Parameter]
	public EventCallback<IReadOnlyList<ChatConversation>> OnConversationsLoaded { get; set; }

	/// <inheritdoc />
	protected override async Task OnInitializedAsync() => await ReloadAsync();

	/// <summary>
	/// Reloads the list from the first page, abandoning anything already in flight.
	/// </summary>
	/// <remarks>
	/// Public so that <see cref="PDChat"/> can refresh the list after archiving or creating a conversation,
	/// rather than the sidebar polling for changes it cannot see.
	/// </remarks>
	public async Task ReloadAsync()
	{
		_conversations.Clear();
		await SearchAsync();
	}

	private async Task OnSearchTextChangedAsync(ChangeEventArgs args)
	{
		_searchText = args.Value?.ToString() ?? string.Empty;
		await DebouncedSearchAsync();
	}

	private async Task SetSearchModeAsync(ChatConversationSearchMode mode)
	{
		if (_searchMode == mode)
		{
			return;
		}

		_searchMode = mode;
		_conversations.Clear();
		await SearchAsync();
	}

	private async Task OnIncludeArchivedChangedAsync(ChangeEventArgs args)
	{
		_includeArchived = args.Value is bool value && value;

		// Cleared immediately rather than left until the results arrive: unticking the box must remove
		// archived conversations from a list the user is already looking at, not leave them there until a
		// round trip completes.
		_conversations.Clear();
		await SearchAsync();
	}

	/// <summary>
	/// Waits for the user to stop typing, then searches - unless another keystroke supersedes this one first.
	/// </summary>
	private async Task DebouncedSearchAsync()
	{
		var cancellation = ReplaceCancellation();

		try
		{
			await Task.Delay(SearchDebounceMilliseconds, cancellation.Token);
		}
		catch (TaskCanceledException)
		{
			// Superseded by a later keystroke, which is the normal path while somebody is typing.
			return;
		}

		_conversations.Clear();
		await SearchAsync(cancellation.Token);
	}

	private Task SearchAsync() => SearchAsync(ReplaceCancellation().Token);

	private async Task SearchAsync(CancellationToken cancellationToken)
	{
		_isLoading = true;
		_loadFailureMessage = null;
		_hasSearchText = !string.IsNullOrWhiteSpace(_searchText);
		StateHasChanged();

		var query = new ChatConversationQuery
		{
			SearchText = _searchText,
			SearchMode = _searchMode,
			IncludeArchived = _includeArchived,
			Skip = _conversations.Count
		};

		try
		{
			var page = await ConversationService.ListAsync(query, cancellationToken);

			if (cancellationToken.IsCancellationRequested)
			{
				// A superseded search must not write its results over a newer one's.
				return;
			}

			_conversations.AddRange(page.Conversations);
			_hasMore = page.HasMore;

			if (OnConversationsLoaded.HasDelegate)
			{
				await OnConversationsLoaded.InvokeAsync(_conversations);
			}
		}
		catch (OperationCanceledException)
		{
			return;
		}
#pragma warning disable CA1031 // The component cannot know what a host's store throws, and must not take
		// the chat down with it - the transcript beside this stays usable whatever happens here.
		catch (Exception ex)
#pragma warning restore CA1031
		{
			_loadFailureMessage = $"Could not load conversations: {ex.Message}";
		}
		finally
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				_isLoading = false;
				if (!_isDisposed)
				{
					StateHasChanged();
				}
			}
		}
	}

	private async Task LoadMoreAsync() => await SearchAsync(ReplaceCancellation().Token);

	/// <summary>
	/// Cancels whatever is in flight and returns a fresh token source for the request replacing it.
	/// </summary>
	private CancellationTokenSource ReplaceCancellation()
	{
		_searchCancellation?.Cancel();
		_searchCancellation?.Dispose();
		_searchCancellation = new CancellationTokenSource();
		return _searchCancellation;
	}

	/// <summary>
	/// Renders a last-activity time the way somebody scanning a list reads it, rather than as a full date.
	/// </summary>
	private static string FormatLastActivity(DateTimeOffset lastMessageUtc)
	{
		var age = DateTimeOffset.UtcNow - lastMessageUtc;

		return age switch
		{
			{ TotalMinutes: < 1 } => "just now",
			{ TotalMinutes: < 60 } => $"{(int)age.TotalMinutes}m ago",
			{ TotalHours: < 24 } => $"{(int)age.TotalHours}h ago",
			{ TotalDays: < 7 } => $"{(int)age.TotalDays}d ago",
			_ => lastMessageUtc.ToLocalTime().ToString("d MMM yyyy", CultureInfo.CurrentCulture)
		};
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_isDisposed = true;
		_searchCancellation?.Cancel();
		_searchCancellation?.Dispose();
		_searchCancellation = null;
		GC.SuppressFinalize(this);
	}
}
