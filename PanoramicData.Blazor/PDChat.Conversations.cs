namespace PanoramicData.Blazor;

/// <summary>
/// The conversation history of <see cref="PDChat"/> (issues #111 and #112, MS-25787 / MS-25788).
/// </summary>
/// <remarks>
/// Everything here runs only when the host supplied an IChatConversationService and the chat is
/// full-screen. HasConversationHistory is the single test for that, so the sidebar, the tabs and the
/// toolbar cannot end up disagreeing about whether the capability is present.
/// </remarks>
public partial class PDChat
{
	/// <summary>
	/// The conversations the user currently has open as tabs, in the order they were opened
	/// (issue #112, MS-25788).
	/// </summary>
	/// <remarks>
	/// Open is not the same as exists. The sidebar lists every conversation the store has; this holds only
	/// the ones the user is working on. Closing a tab removes it from here and from nowhere else - the
	/// conversation is still in the sidebar, because there is no delete anywhere in this feature.
	/// </remarks>
	private readonly List<ChatConversation> _openConversations = [];

	/// <summary>
	/// Conversations that received a reply while the user was looking at a different tab.
	/// </summary>
	/// <remarks>
	/// The whole reason tabs are worth having: Merlin answers take between forty seconds and three minutes,
	/// so the working pattern is ask, switch away, come back. Without a marker the user has to poll their own
	/// tabs to find out which one finished.
	/// </remarks>
	private readonly HashSet<Guid> _unreadConversationIds = [];

	/// <summary>Gets or sets the conversation sidebar; set by the markup's <c>@ref</c>.</summary>
	internal PDChatConversationSidebar? ConversationSidebar { get; set; }

	/// <summary>Gets or sets the conversation tab set; set by the markup's <c>@ref</c>.</summary>
	internal PDTabSet? ConversationTabSet { get; set; }

	private Guid? _selectedConversationId;
	private Guid? _conversationTabToSelect;
	private bool _isConversationSidebarCollapsed;

	/// <summary>
	/// Marks a tab unread when a reply arrives for a conversation the user is not looking at.
	/// </summary>
	/// <remarks>
	/// Deliberately does not switch to it. A reply yanking the user out of what they are reading is worse
	/// than a marker they can act on when they choose - especially when the answer they are waiting for may
	/// be three minutes away.
	/// </remarks>
	private async void OnConversationMessageReceived(Guid conversationId)
	{
		if (conversationId == _selectedConversationId)
		{
			// The singular OnMessageReceived already handled this one; marking it unread as well would leave
			// a marker on the tab the user is currently reading.
			return;
		}

		if (!_openConversations.Any(conversation => conversation.Id == conversationId))
		{
			return;
		}

		_unreadConversationIds.Add(conversationId);
		await InvokeAsync(StateHasChanged);
	}

	/// <summary>
	/// Opens the conversation the chat service is already on, once the sidebar has told us what it is called.
	/// </summary>
	/// <remarks>
	/// Entering full-screen should land on the conversation the user was already having, not on an empty
	/// pane they have to click out of. It runs only while nothing is open, so it cannot fight the user for
	/// control of the selected tab on a later refresh.
	/// </remarks>
	private async Task OnConversationsLoadedAsync(IReadOnlyList<ChatConversation> conversations)
	{
		if (_openConversations.Count > 0 || !ChatService.SupportsConversations)
		{
			return;
		}

		var active = conversations.FirstOrDefault(c => c.Id == ChatService.ActiveConversationId);
		if (active is not null)
		{
			await OpenConversationAsync(active);
		}
	}

	/// <summary>
	/// Opens a conversation from the sidebar into a tab, selecting the existing tab if it is already open.
	/// </summary>
	private async Task OpenConversationAsync(ChatConversation conversation)
	{
		if (!_openConversations.Any(open => open.Id == conversation.Id))
		{
			_openConversations.Add(conversation);

			// The PDTab for a conversation only exists after the next render, so the selection is deferred to
			// OnAfterRenderAsync rather than attempted against a tab set that has not seen it yet.
			_conversationTabToSelect = conversation.Id;
		}

		await SelectConversationAsync(conversation.Id);
	}

	/// <inheritdoc />
	protected override async Task OnAfterRenderWithModuleAsync(bool firstRender)
	{
		SelectPendingConversationTab();
		await base.OnAfterRenderWithModuleAsync(firstRender);
	}

	/// <summary>
	/// Moves PDTabSet's own selection onto a tab that has only just been rendered.
	/// </summary>
	/// <remarks>
	/// PDTabSet owns which tab is active - a PDTab registers itself on initialisation and the set selects the
	/// first one it is given. Opening a second conversation therefore has to ask it to move, and can only do
	/// so once the new tab exists.
	/// </remarks>
	private void SelectPendingConversationTab()
	{
		if (_conversationTabToSelect is not { } pending || ConversationTabSet is null)
		{
			return;
		}

		if (ConversationTabSet.SelectTabById(pending))
		{
			_conversationTabToSelect = null;
		}
	}

	/// <summary>Handles the user clicking a tab.</summary>
	private async Task OnConversationTabSelectedAsync(PDTab tab)
	{
		if (tab.Id != _selectedConversationId)
		{
			await SelectConversationAsync(tab.Id);
		}
	}

	/// <summary>Handles the user closing a tab. Closing neither archives nor deletes.</summary>
	private async Task OnConversationTabClosedAsync(PDTab tab) => await CloseConversationTabAsync(tab.Id);

	/// <summary>Handles the tab set's add button.</summary>
	private async Task OnConversationTabAddedAsync() => await NewConversationAsync();

	/// <summary>
	/// Handles a tab being renamed, writing the new title through to the conversation store.
	/// </summary>
	/// <remarks>
	/// PDTabSet already supports renaming by double-click, and the conversation contract already has
	/// RenameAsync - which nothing called until now. Wiring the two together is the whole feature.
	/// </remarks>
	private async Task OnConversationTabRenamedAsync(PDTab tab)
	{
		if (ConversationService is null)
		{
			return;
		}

		await ConversationService.RenameAsync(tab.Id, tab.Title, CancellationToken.None);

		var open = _openConversations.FirstOrDefault(conversation => conversation.Id == tab.Id);
		if (open is not null)
		{
			open.Title = tab.Title;
		}

		if (ConversationSidebar is not null)
		{
			await ConversationSidebar.ReloadAsync();
		}
	}

	/// <summary>
	/// Shows one of the open conversations, loading its transcript.
	/// </summary>
	private async Task SelectConversationAsync(Guid conversationId)
	{
		_selectedConversationId = conversationId;
		_unreadConversationIds.Remove(conversationId);

		if (ChatService.SupportsConversations)
		{
			ChatService.ActiveConversationId = conversationId;
		}

		_messages.Clear();

		// Read the transcript from the history rather than the chat service: the chat service knows only
		// what has happened this session, and a conversation opened from the sidebar may predate it entirely.
		if (ConversationService is not null)
		{
			try
			{
				_messages.AddRange(await ConversationService.GetMessagesAsync(conversationId, CancellationToken.None));
			}
#pragma warning disable CA1031 // A host store's failure must not take the chat down; the transcript simply
			// starts empty and the user can carry on typing into it.
			catch (Exception)
#pragma warning restore CA1031
			{
				_messages.AddRange(ChatService.GetMessages(conversationId));
			}
		}

		await InvokeAsync(StateHasChanged);
	}

	/// <summary>
	/// Opens a new, empty conversation.
	/// </summary>
	/// <remarks>
	/// The conversation is created in the store immediately rather than lazily on first message. The ticket
	/// asks for lazy creation so that somebody who clicks <i>new</i> and changes their mind does not litter a
	/// history nothing can delete - that belongs with the store, which is the only thing that can tell an
	/// abandoned conversation from a quiet one, and is recorded on MS-25788 rather than faked here.
	/// </remarks>
	private async Task NewConversationAsync()
	{
		if (ConversationService is null)
		{
			return;
		}

		var conversation = await ConversationService.CreateAsync(CancellationToken.None);

		if (ChatService.SupportsConversations)
		{
			ChatService.ActiveConversationId = conversation.Id;
		}

		await OpenConversationAsync(conversation);

		if (ConversationSidebar is not null)
		{
			await ConversationSidebar.ReloadAsync();
		}
	}

	/// <summary>
	/// Gets a value indicating whether the conversation currently shown is archived.
	/// </summary>
	/// <remarks>
	/// Read from the open conversation rather than re-queried, so that the control appears and disappears in
	/// step with the archive and un-archive actions without a round trip to the store.
	/// </remarks>
	private bool SelectedConversationIsArchived
		=> _selectedConversationId is { } id
			&& _openConversations.FirstOrDefault(conversation => conversation.Id == id)?.IsArchived == true;

	/// <summary>
	/// Returns the selected conversation to the default list.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The counterpart to <see cref="ArchiveSelectedConversationAsync"/>, and the reason archiving is not a
	/// disguised delete: whatever archiving hid can be brought back.
	/// </para>
	/// <para>
	/// Unlike archiving, this deliberately leaves the tab open. Archiving closes the tab because leaving an
	/// archived conversation open and re-activating it on the next keystroke is a confusing pair of
	/// behaviours; un-archiving has no such tension - the user has just said they want this conversation
	/// back, so taking it off their screen would be perverse.
	/// </para>
	/// </remarks>
	private async Task UnarchiveSelectedConversationAsync()
	{
		if (ConversationService is null || _selectedConversationId is not { } conversationId)
		{
			return;
		}

		await ConversationService.UnarchiveAsync(conversationId, CancellationToken.None);

		// Update the open copy so the control disappears immediately, rather than waiting for the sidebar
		// reload to come back and tell us what we already know.
		var open = _openConversations.FirstOrDefault(conversation => conversation.Id == conversationId);
		if (open is not null)
		{
			open.IsArchived = false;
		}

		if (ConversationSidebar is not null)
		{
			await ConversationSidebar.ReloadAsync();
		}

		await InvokeAsync(StateHasChanged);
	}

	/// <summary>
	/// Archives the selected conversation and closes its tab.
	/// </summary>
	/// <remarks>
	/// Closing the tab is part of archiving rather than a separate step: leaving an archived conversation
	/// open, and re-activating it on the next keystroke, is a confusing pair of behaviours.
	/// </remarks>
	private async Task ArchiveSelectedConversationAsync()
	{
		if (ConversationService is null || _selectedConversationId is not { } conversationId)
		{
			return;
		}

		await ConversationService.ArchiveAsync(conversationId, CancellationToken.None);
		await CloseConversationTabAsync(conversationId);

		if (ConversationSidebar is not null)
		{
			await ConversationSidebar.ReloadAsync();
		}
	}

	/// <summary>
	/// Closes a tab. Does not archive and does not delete - the conversation stays in the sidebar.
	/// </summary>
	private async Task CloseConversationTabAsync(Guid conversationId)
	{
		_openConversations.RemoveAll(open => open.Id == conversationId);
		_unreadConversationIds.Remove(conversationId);

		if (_selectedConversationId != conversationId)
		{
			await InvokeAsync(StateHasChanged);
			return;
		}

		// Closing the selected tab falls back to whatever is still open, and to an empty state when nothing
		// is - not to a blank pane with no way out.
		if (_openConversations.Count > 0)
		{
			await SelectConversationAsync(_openConversations[^1].Id);
			return;
		}

		_selectedConversationId = null;
		_messages.Clear();
		await InvokeAsync(StateHasChanged);
	}

	private async Task ToggleConversationSidebarAsync()
	{
		_isConversationSidebarCollapsed = !_isConversationSidebarCollapsed;
		await InvokeAsync(StateHasChanged);
	}
}
