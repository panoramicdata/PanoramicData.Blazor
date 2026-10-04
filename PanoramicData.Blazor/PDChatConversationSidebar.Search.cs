namespace PanoramicData.Blazor;

/// <summary>
/// PDChatConversationSidebar: the debounced, cancellable search that fills the conversation list.
/// </summary>
public partial class PDChatConversationSidebar
{
	private CancellationTokenSource? _searchCancellation;
	private bool _isLoading;
	private bool _hasMore;
	private bool _hasSearchText;
	private string? _loadFailureMessage;

	/// <summary>
	/// Waits for the user to stop typing, then searches - unless another keystroke supersedes this one first.
	/// </summary>
	private async Task DebouncedSearchAsync()
	{
		var cancellation = ReplaceCancellation();

		try
		{
			await Task.Delay(TimeSpan.FromMilliseconds(SearchDebounceMilliseconds), Clock, cancellation.Token);
		}
		catch (TaskCanceledException)
		{
			// Superseded by a later keystroke, which is the normal path while somebody is typing.
			return;
		}

		// The delay can finish just before a newer keystroke cancels it, with this continuation still queued
		// behind that keystroke. It is superseded all the same, and its token source is already disposed, so
		// it must neither clear the list the newer search is filling nor read the token (issue #218).
		if (cancellation.IsCancellationRequested)
		{
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

			await OnConversationsLoaded.InvokeAsync(_conversations);
		}
		catch (OperationCanceledException)
		{
			// Superseded by a newer search, which owns the list and the loading state from here on.
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
		CancelSearch();
		_searchCancellation = new CancellationTokenSource();
		return _searchCancellation;
	}

	/// <summary>
	/// Cancels whatever is in flight and releases its token source.
	/// </summary>
	private void CancelSearch()
	{
		_searchCancellation?.Cancel();
		_searchCancellation?.Dispose();
		_searchCancellation = null;
	}
}
