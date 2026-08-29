namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a chat interface with support for docking, muting, and message history.
/// </summary>
public partial class PDChat : JSModuleComponentBase
{
	/// <summary>
	/// Gets or sets the chat service used to send and receive messages.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public required IChatService ChatService { get; set; }

	/// <summary>
	/// Gets or sets the sender identity for the current user.
	/// </summary>
	[EditorRequired]
	[Parameter]
	public required ChatMessageSender User { get; set; }

	/// <summary>
	/// Gets or sets an optional conversation history: the list of previous conversations that can be searched,
	/// opened and archived (issue #108). <c>null</c> - the default - means the host has no such store.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Null means the capability is absent, not merely hidden.</b> Without one there is no sidebar, no
	/// conversation tabs and no toolbar, and <see cref="HasConversationHistory"/> is the single test that says
	/// so. A host that supplies nothing gets exactly the chat it has today.
	/// </para>
	/// <para>
	/// A parameter rather than an injected service, matching <see cref="ChatService"/> beside it. Acquiring
	/// the two by different routes would let a host pass a bespoke chat service and silently receive a
	/// conversation store from the container that knows nothing about it - two halves of one conversation,
	/// disagreeing. A host that does keep this in its container passes it through as
	/// <c>ConversationService="@ConversationService"</c>.
	/// </para>
	/// </remarks>
	[Parameter]
	public IChatConversationService? ConversationService { get; set; }

	/// <summary>
	/// Gets a value indicating whether a conversation history is available to show.
	/// </summary>
	/// <remarks>
	/// The single place the question is asked, so that the sidebar, the conversation tabs and the toolbar
	/// cannot end up disagreeing about whether the capability is present - which would show, for instance, a
	/// toolbar whose every control is dead.
	/// </remarks>
	private bool HasConversationHistory => ConversationService is not null;

	/// <summary>
	/// Handed to descendant messages so an inline form can report its outcome (issue #106).
	/// </summary>
	private ChatFormContext FormContext => _formContext ??= new ChatFormContext
	{
		OnSubmitted = OnFormSubmittedAsync,
		OnDismissed = OnFormDismissedAsync
	};

	private ChatFormContext? _formContext;

	/// <summary>
	/// Cascading parameter to get the parent chat container, if any.
	/// When present, dock mode changes will be automatically synchronized.
	/// </summary>
	[CascadingParameter(Name = "ChatContainer")]
	public PDChatContainer? Container { get; set; }

	/// <summary>
	/// Gets or sets the dock position of the chat window.
	/// </summary>
	[Parameter]
	public PDChatDockPosition ChatDockPosition { get; set; } = PDChatDockPosition.Right;

	/// <summary>
	/// Gets or sets the icon to display when the chat window is collapsed.
	/// </summary>
	[Parameter]
	public string CollapsedIcon { get; set; } = "💬";

	/// <summary>
	/// A function to select a user icon for a given message.
	/// </summary>
	[Parameter]
	public Func<ChatMessage, string?>? UserIconSelector { get; set; }

	/// <summary>
	/// A function to select a priority icon for a given message.
	/// </summary>
	[Parameter]
	public Func<ChatMessage, string?>? PriorityIconSelector { get; set; }

	/// <summary>
	/// A function to select a sound to play for a given message.
	/// </summary>
	[Parameter]
	public Func<ChatMessage, string?>? SoundSelector { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat window is minimized.
	/// </summary>
	[Parameter]
	public EventCallback OnChatMinimized { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat window is restored.
	/// </summary>
	[Parameter]
	public EventCallback OnChatRestored { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat window is maximized.
	/// </summary>
	[Parameter]
	public EventCallback OnChatMaximized { get; set; }

	/// <summary>
	/// An event callback that is invoked when the mute setting is toggled.
	/// </summary>
	[Parameter]
	public EventCallback OnMuteToggled { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat is cleared.
	/// </summary>
	[Parameter]
	public EventCallback OnChatCleared { get; set; }

	/// <summary>
	/// An event callback that is invoked when a message is sent.
	/// </summary>
	[Parameter]
	public EventCallback<ChatMessage> OnMessageSent { get; set; }

	/// <summary>
	/// An event callback that is invoked when a message is received.
	/// </summary>
	[Parameter]
	public EventCallback<ChatMessage> OnMessageReceivedEvent { get; set; }

	/// <summary>
	/// An event callback that is invoked when the chat window is automatically restored.
	/// </summary>
	[Parameter]
	public EventCallback OnAutoRestored { get; set; }

	private bool _isMuted;
	private bool _unreadMessages;
	private MessageType _highestPriorityUnreadMessage = MessageType.Normal;
	private DateTimeOffset _lastReadTimestamp = DateTimeOffset.UtcNow;
	private string _currentInput = "";

	// Fixed duration, in milliseconds, of the "de-stack" animation used when a toast leaves a stack
	// that still contains other toasts. Deliberately uniform and independent of any per-message
	// animation configuration (see the toast stacking rules).
	private const double _deStackDurationMs = 250d;

	private readonly List<ToastItem> _toasts = [];

	private readonly List<ChatMessage> _messages = [];
	private PDTabSet? _tabSetRef;
	private PDChatDockMode? _restoreDockMode;
	private PDMessages? _messagesComponent;

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

	private PDChatConversationSidebar? _conversationSidebar;
	private PDTabSet? _conversationTabSet;
	private Guid? _selectedConversationId;
	private Guid? _conversationTabToSelect;
	private bool _isConversationSidebarCollapsed;

	/// <summary>Gets the JavaScript module path for this component.</summary>
	protected override string ModulePath => "./_content/PanoramicData.Blazor/PDChat.razor.js";

	/// <inheritdoc />
	protected override Task OnInitializedAsync()
	{
		// Load existing messages from the service
		_messages.Clear();
		_messages.AddRange(ChatService.Messages);

		// Sync local mute state with service
		_isMuted = ChatService.IsMuted;

		ChatService.OnMessageReceived += OnMessageReceived;

		// Issue #112: a reply arriving for a tab the user is not looking at has to mark that tab rather than
		// append to the one they are. Only the conversation-addressed event says which conversation a message
		// belongs to; the singular one above cannot, which is why this is conditional rather than a
		// replacement for it.
		if (ChatService.SupportsConversations)
		{
			ChatService.OnConversationMessageReceived += OnConversationMessageReceived;
			_selectedConversationId = ChatService.ActiveConversationId;
		}

		ChatService.OnLiveStatusChanged += OnLiveStatusChanged;
		ChatService.OnDockModeChanged += OnServiceDockModeChanged;
		ChatService.OnMuteStatusChanged += OnServiceMuteStatusChanged;
		ChatService.OnConfigurationChanged += OnServiceConfigurationChanged;
		ChatService.Initialize();
		return base.OnInitializedAsync();
	}

	/// <summary>
	/// Change the dock mode state with proper state tracking.
	/// </summary>
	private async Task ChangeDockModeAsync(PDChatDockMode newMode)
	{
		// Only remember genuine corner positions here - _restoreDockMode exists purely so
		// UnpinFromSideAsync can return to "wherever the panel was before it got pinned to a
		// side". Capturing Left/Right too (as this used to) meant a minimize/reopen or
		// fullscreen/restore round-trip while docked would leave it holding the *current*
		// split mode, silently turning the next "Unpin from Side" click back into the exact
		// no-op MS-24840 fixed - reproducibly, not just intermittently.
		if (ChatService.DockMode is PDChatDockMode.TopLeft or PDChatDockMode.TopRight
			or PDChatDockMode.BottomLeft or PDChatDockMode.BottomRight)
		{
			_restoreDockMode = ChatService.DockMode;
		}

		// Notify container if there is one, and let it handle the changes
		if (Container is not null)
		{
			await Container.OnInternalDockModeChanged(newMode);
			return;
		}

		ChatService.DockMode = newMode;

		await InvokeAsync(StateHasChanged);
	}

	private void OnLiveStatusChanged(bool obj)
	{
		_ = InvokeAsync(StateHasChanged);
	}

	private void OnServiceDockModeChanged(PDChatDockMode newDockMode)
		=> _ = ChangeDockModeAsync(newDockMode);

	private void OnServiceMuteStatusChanged(bool isMuted)
	{
		// Sync local mute state with service
		_isMuted = isMuted;
		_ = InvokeAsync(StateHasChanged);
	}

	private void OnServiceConfigurationChanged()
	{
		// Configuration changed, trigger UI update and ensure parameters are synchronized
		_ = InvokeAsync(StateHasChanged);
	}

	// This is an async void method because it is a synchronous event handler for
	// ChatService.OnMessageReceived. Exceptions thrown here cannot be observed by the caller and
	// would otherwise be posted to the renderer's synchronization context, surfacing as Blazor's
	// "An unhandled error has occurred". During circuit teardown (page navigation or a dropped
	// WebSocket) the JS interop calls below throw TaskCanceledException / JSDisconnectedException,
	// so those teardown exceptions are swallowed here rather than crashing the circuit. See MS-24383.
	private async void OnMessageReceived(ChatMessage message)
	{
		try
		{
			await OnMessageReceivedAsync(message);
		}
		catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
		{
			// Expected when the Blazor circuit / JS runtime is being torn down; nothing to do.
		}
	}

	private async Task OnMessageReceivedAsync(ChatMessage message)
	{
		var existing = _messages.FirstOrDefault(m => m.Id == message.Id);
		var isNewMessage = existing == null;

		if (existing != null)
		{
			existing.Message = message.Message;
			existing.Type = message.Type;
			existing.Title = message.Title;
			existing.Timestamp = message.Timestamp;
			existing.IsTitleHtml = message.IsTitleHtml;
			existing.IsMessageHtml = message.IsMessageHtml;

			// Issue #98: the in-progress fields have to be copied too. This list is hand-maintained,
			// which is a trap - a field added to ChatMessage and not added here is silently dropped on
			// every update after the first, and the symptom is baffling: the title changes, so the
			// update is clearly arriving, while the content it was carrying never appears. That is
			// exactly how these three were first missed.
			existing.ProgressSteps = message.ProgressSteps;
			existing.Thoughts = message.Thoughts;
			existing.PartialMessage = message.PartialMessage;
			existing.ToastOptions = message.ToastOptions;
			// Issue #106. Every field here is copied by hand, so a new payload on ChatMessage that is
			// not added to this list is silently dropped - which has already caught out ProgressSteps
			// and Thoughts once.
			existing.Form = message.Form;
			existing.FormSubmission = message.FormSubmission;
		}
		else
		{
			_messages.Add(message);
		}

		// Emit OnMessageReceived event for new messages
		if (isNewMessage && OnMessageReceivedEvent.HasDelegate)
		{
			await OnMessageReceivedEvent.InvokeAsync(message);
		}

		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			_unreadMessages = true;

			// Update the highest priority unread message type
			if (isNewMessage && message.Type != MessageType.Typing)
			{
				UpdateHighestPriorityUnreadMessage();
			}

			var isToastable = isNewMessage && message.Type != MessageType.Typing;

			// Auto-restore takes priority over toasts: if the chat is going to open anyway there is no
			// point showing a toast for the same message.
			if (ChatService.AutoRestoreOnNewMessage && isToastable)
			{
				ClearToasts();
				await ChangeDockModeAsync(ChatService.RestoreMode);
				_unreadMessages = false;
				_highestPriorityUnreadMessage = MessageType.Normal;
				_lastReadTimestamp = DateTimeOffset.UtcNow;

				if (OnAutoRestored.HasDelegate)
				{
					await OnAutoRestored.InvokeAsync();
				}
			}
			else if (ChatService.ToastEnabled && isToastable)
			{
				// Show the message as an animated toast. This works whether or not the minimized
				// button is visible (MinimizedButtonPosition.None => headless toast).
				await ShowToastAsync(message);
			}
		}

		// Get the sound to play, if any
		var soundUrlString = SoundSelector?.Invoke(message);
		if (!string.IsNullOrWhiteSpace(soundUrlString) && !_isMuted && Module is not null)
		{
			try
			{
				// Play the sound
				await Module.InvokeVoidAsync("playSound", soundUrlString).ConfigureAwait(true);
			}
			catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException or ObjectDisposedException)
			{
				// The circuit / JS runtime is gone (e.g. the tab was closed). Ignore and continue
				// so the remaining state update is still attempted. See MS-24383.
			}
		}

		await InvokeAsync(StateHasChanged);
	}

	/// <summary>
	/// Adds a new toast for the supplied message, resolving per-message overrides against the
	/// service-level defaults, enforcing the visible-toast cap, and starting its auto-dismiss timer.
	/// </summary>
	private async Task ShowToastAsync(ChatMessage message)
	{
		var o = message.ToastOptions;

		var item = new ToastItem
		{
			Message = message,
			EntryAnimation = o?.EntryAnimation ?? ChatService.ToastEntryAnimation,
			ExitAnimation = o?.ExitAnimation ?? ChatService.ToastExitAnimation,
			AnimationDurationMs = o?.AnimationDurationMs ?? ChatService.ToastAnimationDurationMs,
			AutoDismiss = o?.AutoDismiss ?? ChatService.ToastAutoDismiss,
			DisplayDurationSeconds = o?.DisplayDurationSeconds ?? ChatService.ToastDisplayDurationSeconds,
			ShowTitle = o?.ShowTitle ?? ChatService.ToastShowTitle,
			MinWidth = o?.MinWidth ?? ChatService.ToastMinWidth,
			MaxWidth = o?.MaxWidth ?? ChatService.ToastMaxWidth,
			MinHeight = o?.MinHeight ?? ChatService.ToastMinHeight,
			MaxHeight = o?.MaxHeight ?? ChatService.ToastMaxHeight,
		};

		// Enforce the visible-toast cap by dismissing the oldest still-present toasts.
		var maxVisible = Math.Max(1, ChatService.ToastMaxVisible);
		var overflow = _toasts.Count(t => !t.IsExiting) - (maxVisible - 1);
		for (var i = 0; overflow > 0 && i < _toasts.Count; i++)
		{
			if (!_toasts[i].IsExiting)
			{
				BeginDismiss(_toasts[i]);
				overflow--;
			}
		}

		// Newest is added at the bottom of the stack (oldest remains at the top).
		_toasts.Add(item);
		await InvokeAsync(StateHasChanged);

		if (item.AutoDismiss && item.DisplayDurationSeconds > 0)
		{
			StartDismissTimer(item, item.DisplayDurationSeconds * 1000d);
		}
	}

	// (Re)starts the auto-dismiss timer for a toast, tracking the start time so hover-pause can
	// compute the remaining time accurately.
	private void StartDismissTimer(ToastItem item, double remainingMs)
	{
		item.DismissTimer?.Dispose();
		item.RemainingMs = remainingMs;
		item.StartedTicks = Environment.TickCount64;
		item.Paused = false;
		item.DismissTimer = new Timer(
			_ => _ = InvokeAsync(() => BeginDismiss(item)),
			null,
			TimeSpan.FromMilliseconds(remainingMs),
			Timeout.InfiniteTimeSpan);
	}

	// Pauses a toast's auto-dismiss countdown (e.g. while the pointer hovers over it).
	private static void PauseToast(ToastItem item)
	{
		if (item.IsExiting || item.Paused || item.DismissTimer is null)
		{
			return;
		}

		var elapsed = Environment.TickCount64 - item.StartedTicks;
		item.RemainingMs = Math.Max(0, item.RemainingMs - elapsed);
		item.DismissTimer.Dispose();
		item.DismissTimer = null;
		item.Paused = true;
	}

	// Resumes a paused toast's auto-dismiss countdown from where it left off.
	private void ResumeToast(ToastItem item)
	{
		if (item.IsExiting || !item.Paused)
		{
			return;
		}

		if (item.RemainingMs <= 0)
		{
			BeginDismiss(item);
			return;
		}

		StartDismissTimer(item, item.RemainingMs);
	}

	// Begins the exit of a toast. If other toasts remain in the stack it leaves via the fixed
	// de-stack animation; if it is the last one it uses its own configured exit animation.
	private void BeginDismiss(ToastItem item)
	{
		if (item.IsExiting)
		{
			return;
		}

		item.DismissTimer?.Dispose();
		item.DismissTimer = null;

		var othersRemain = _toasts.Any(t => t != item && !t.IsExiting);
		item.IsExiting = true;
		item.IsDeStacking = othersRemain;

		var exitMs = othersRemain ? _deStackDurationMs : item.AnimationDurationMs;

		item.RemovalTimer?.Dispose();
		item.RemovalTimer = new Timer(
			_ => _ = InvokeAsync(() =>
			{
				item.RemovalTimer?.Dispose();
				item.RemovalTimer = null;
				_toasts.Remove(item);
				StateHasChanged();
			}),
			null,
			TimeSpan.FromMilliseconds(Math.Max(1, exitMs)),
			Timeout.InfiniteTimeSpan);

		_ = InvokeAsync(StateHasChanged);
	}

	// Disposes every toast timer and clears the stack immediately (no exit animation).
	private void ClearToasts()
	{
		foreach (var t in _toasts)
		{
			t.DismissTimer?.Dispose();
			t.RemovalTimer?.Dispose();
		}

		_toasts.Clear();
	}

	// Invoked when a toast is clicked: opens the chat and clears the stack.
	private async Task OnToastClickedAsync()
	{
		ClearToasts();
		await ToggleChatAsync();
	}

	private async Task ToggleChatAsync()
	{
		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			// Dismiss any toasts when opening chat
			ClearToasts();

			// Restore to last normal state
			await ChangeDockModeAsync(ChatService.RestoreMode);
			_unreadMessages = false;
			_highestPriorityUnreadMessage = MessageType.Normal;
			_lastReadTimestamp = DateTimeOffset.UtcNow;

			if (OnChatRestored.HasDelegate)
			{
				await OnChatRestored.InvokeAsync();
			}
		}
		else
		{
			// Minimize
			await ChangeDockModeAsync(PDChatDockMode.Minimized);

			if (OnChatMinimized.HasDelegate)
			{
				await OnChatMinimized.InvokeAsync();
			}
		}
	}

	private async Task ToggleMuteAsync()
	{
		// Update both local and service mute state
		_isMuted = !_isMuted;
		ChatService.IsMuted = _isMuted;

		// Emit mute toggle event
		if (OnMuteToggled.HasDelegate)
		{
			await OnMuteToggled.InvokeAsync();
		}
	}

	private async Task ToggleFullScreenAsync()
	{
		if (ChatService.DockMode == PDChatDockMode.FullScreen)
		{
			// Restore to last normal state
			await ChangeDockModeAsync(ChatService.RestoreMode);

			if (OnChatRestored.HasDelegate)
			{
				await OnChatRestored.InvokeAsync();
			}
		}
		else
		{
			// Maximize to fullscreen
			await ChangeDockModeAsync(PDChatDockMode.FullScreen);

			if (OnChatMaximized.HasDelegate)
			{
				await OnChatMaximized.InvokeAsync();
			}
		}
	}

	private async Task ClearChatAsync()
	{
		// Clear messages from both local collection and service
		_messages.Clear();
		ChatService.ClearMessages();
		_currentInput = string.Empty;
		_unreadMessages = false;
		_highestPriorityUnreadMessage = MessageType.Normal;
		_lastReadTimestamp = DateTimeOffset.UtcNow;
		await InvokeAsync(StateHasChanged);

		// Emit chat cleared event
		if (OnChatCleared.HasDelegate)
		{
			await OnChatCleared.InvokeAsync();
		}
	}

	private async Task DockToSideAsync()
	{
		// Determine which side to dock to based on current position
		await ChangeDockModeAsync(ChatService.DockMode switch
		{
			PDChatDockMode.TopRight or PDChatDockMode.BottomRight => PDChatDockMode.Right,
			PDChatDockMode.TopLeft or PDChatDockMode.BottomLeft => PDChatDockMode.Left,
			_ => PDChatDockMode.Right // Default to right for other cases
		});
	}

	private async Task UnpinFromSideAsync()
	{
		// Restore to wherever the panel was docked before it was pinned to a side. If that was
		// never recorded (e.g. the panel opened directly into a split RestoreMode from Minimized,
		// so there is no prior non-split mode to remember), ChatService.RestoreMode is not a safe
		// fallback here - it commonly *is* the current split mode, which would make this a no-op.
		// Fall back to the corner the minimized button already lives in instead, which is always
		// a corner position and never equal to the current (split) mode.
		await ChangeDockModeAsync(_restoreDockMode ?? GetFallbackCornerDockMode());

		if (OnChatRestored.HasDelegate)
		{
			await OnChatRestored.InvokeAsync();
		}
	}

	private PDChatDockMode GetFallbackCornerDockMode()
		=> ChatService.MinimizedButtonPosition switch
		{
			PDChatButtonPosition.TopLeft => PDChatDockMode.TopLeft,
			PDChatButtonPosition.TopRight => PDChatDockMode.TopRight,
			PDChatButtonPosition.BottomLeft => PDChatDockMode.BottomLeft,
			_ => PDChatDockMode.BottomRight
		};

	private async Task SendCurrentMessageAsync()
	{
		if (string.IsNullOrWhiteSpace(_currentInput))
		{
			return;
		}

		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = _currentInput,
			Sender = User,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow
		};

		// Fire it off
		ChatService.SendMessage(message);

		// Emit message sent event
		if (OnMessageSent.HasDelegate)
		{
			await OnMessageSent.InvokeAsync(message);
		}

		// Clear input locally
		_currentInput = string.Empty;

		// Clear the PDMessages component's textarea
		_messagesComponent?.ClearInput();
	}

	private bool CanSend => ChatService.IsInputPermitted && ChatService.IsLive && !string.IsNullOrWhiteSpace(_currentInput);

	// ==========================================================================================
	// Conversation history (issues #111 and #112, MS-25787 / MS-25788)
	//
	// Everything below runs only when the host supplied an IChatConversationService and the chat is
	// full-screen. HasConversationHistory is the single test for that, so the sidebar, the tabs and the
	// toolbar cannot end up disagreeing about whether the capability is present.
	// ==========================================================================================

	/// <summary>
	/// Marks a tab unread when a reply arrives for a conversation the user is not looking at.
	/// </summary>
	/// <remarks>
	/// Deliberately does not switch to it. A reply yanking the user out of what they are reading is worse
	/// than a marker they can act on when they choose - especially when the answer they are waiting for may
	/// be three minutes away.
	/// </remarks>
	private async void OnConversationMessageReceived(Guid conversationId, ChatMessage message)
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
		if (_conversationTabToSelect is not { } pending || _conversationTabSet is null)
		{
			return;
		}

		if (_conversationTabSet.SelectTabById(pending))
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
	private async Task OnConversationTabAddedAsync(CreateTabPosition position) => await NewConversationAsync();

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

		if (_conversationSidebar is not null)
		{
			await _conversationSidebar.ReloadAsync();
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

		if (_conversationSidebar is not null)
		{
			await _conversationSidebar.ReloadAsync();
		}
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

		if (_conversationSidebar is not null)
		{
			await _conversationSidebar.ReloadAsync();
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

	private void OnTabAdded()
	{
		if (_tabSetRef is not null)
		{
#pragma warning disable BL0005 // Setting component parameters directly when building tabs programmatically
			var newTab = new PDTab
			{
				Title = "New Tab",
				IsRenamingEnabled = true,
				ChildContent = builder =>
					{
						builder.OpenComponent<PDMonacoEditor>(0);
						builder.AddAttribute(1, "Language", "csharp");
						builder.AddAttribute(2, "Theme", "vs-dark");
						builder.AddAttribute(3, "InitializeOptions", new Func<BlazorMonaco.Editor.StandaloneEditorConstructionOptions>(() =>
							new BlazorMonaco.Editor.StandaloneEditorConstructionOptions
							{
								AutomaticLayout = true,
								Language = "csharp",
								Theme = "vs-dark",
								Value = "// Welcome to the Monaco Editor!\n// Start coding here...\n",
								Minimap = new BlazorMonaco.Editor.EditorMinimapOptions { Enabled = false }
							}));
						builder.CloseComponent();
					}
			};
#pragma warning restore BL0005

			_tabSetRef.AddTab(newTab);
			_tabSetRef.StartRenamingTab(newTab);
		}
	}

	// Helper method to update the highest priority unread message
	private void UpdateHighestPriorityUnreadMessage()
	{
		if (!_unreadMessages || _messages.Count == 0)
		{
			_highestPriorityUnreadMessage = MessageType.Normal;
			return;
		}

		// Get the highest priority message that arrived after the last read timestamp
		// and exclude typing messages
		var unreadNonTypingMessages = _messages
			.Where(m => m.Type != MessageType.Typing && m.Timestamp > _lastReadTimestamp)
			.ToList();

		if (unreadNonTypingMessages.Count == 0)
		{
			_highestPriorityUnreadMessage = MessageType.Normal;
			return;
		}

		// Rank by severity explicitly rather than relying on the raw MessageType enum order.
		// The enum's numeric order is not a severity order: Success = 5 is the highest value,
		// so a plain .Max() would let a success message mask an unread warning/error/critical.
		_highestPriorityUnreadMessage = unreadNonTypingMessages
			.Select(m => m.Type)
			.OrderByDescending(GetSeverityRank)
			.First();
	}

	// Maps a message type to a severity rank (higher = more severe) so the minimized badge
	// reflects the worst unread message. Deliberately independent of the MessageType enum's
	// numeric values, which are not ordered by severity (Success is the highest enum value).
	private static int GetSeverityRank(MessageType type) => type switch
	{
		MessageType.Critical => 5,
		MessageType.Error => 4,
		MessageType.Warning => 3,
		MessageType.Normal => 2,
		MessageType.Success => 1,
		MessageType.Typing => 0,
		_ => 0,
	};

	// Helper method to get the bootstrap color class based on message priority
	private string GetBootstrapColorClass()
	{
		if (!ChatService.IsLive)
		{
			return "pdchat-not-live";
		}

		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Critical => "pdchat-critical",
			MessageType.Error => "pdchat-error",
			MessageType.Warning => "pdchat-warning",
			MessageType.Normal => "pdchat-info",
			MessageType.Success => "pdchat-success",
			MessageType.Typing => string.Empty,
			_ => string.Empty
		};
	}

	// Helper method to get the animation class based on message priority
	private string GetAnimationClass()
	{
		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Critical => "pulsate-critical",
			MessageType.Error => "pulsate-error",
			MessageType.Warning => "pulsate-warning",
			MessageType.Normal => "pulsate",
			MessageType.Typing => string.Empty,
			_ => string.Empty
		};
	}

	// Helper method to get the priority indicator icon
	private string GetPriorityIndicator()
	{
		if (!_unreadMessages)
		{
			return string.Empty;
		}

		return _highestPriorityUnreadMessage switch
		{
			MessageType.Warning => "⚠",
			MessageType.Error => "!",
			MessageType.Critical => "!!",
			_ => string.Empty
		};
	}

	// Helper method to get CSS classes for dock mode positioning
	private string GetDockModeClasses()
	{
		// If minimized, always use minimized logic regardless of container state
		if (ChatService.DockMode == PDChatDockMode.Minimized)
		{
			// When minimized, use the service's MinimizedButtonPosition to position the button
			var buttonPositionClass = ChatService.MinimizedButtonPosition switch
			{
				PDChatButtonPosition.BottomRight => "dock-bottom-right",
				PDChatButtonPosition.TopRight => "dock-top-right",
				PDChatButtonPosition.BottomLeft => "dock-bottom-left",
				PDChatButtonPosition.TopLeft => "dock-top-left",
				PDChatButtonPosition.None => "dock-none", // Hide the button completely
				_ => "dock-bottom-right" // Default fallback
			};
			return $"{buttonPositionClass} dock-minimized";
		}

		// Check if we're in a container that's handling split mode
		if (Container?.IsSplitMode == true && (ChatService.DockMode == PDChatDockMode.Left || ChatService.DockMode == PDChatDockMode.Right))
		{
			return "dock-split-panel";
		}

		return ChatService.DockMode switch
		{
			PDChatDockMode.BottomRight => "dock-bottom-right",
			PDChatDockMode.TopRight => "dock-top-right",
			PDChatDockMode.BottomLeft => "dock-bottom-left",
			PDChatDockMode.TopLeft => "dock-top-left",
			PDChatDockMode.FullScreen => "dock-fullscreen",
			PDChatDockMode.Left => "dock-left",
			PDChatDockMode.Right => "dock-right",
			_ => "dock-bottom-right" // Default fallback
		};
	}

	/// <inheritdoc />
	public override async ValueTask DisposeAsync()
	{
		// Clean up event handlers
		ChatService.OnMessageReceived -= OnMessageReceived;

		if (ChatService.SupportsConversations)
		{
			ChatService.OnConversationMessageReceived -= OnConversationMessageReceived;
		}
		ChatService.OnLiveStatusChanged -= OnLiveStatusChanged;
		ChatService.OnDockModeChanged -= OnServiceDockModeChanged;
		ChatService.OnMuteStatusChanged -= OnServiceMuteStatusChanged;
		ChatService.OnConfigurationChanged -= OnServiceConfigurationChanged;

		// Clean up toast timers
		ClearToasts();

		await base.DisposeAsync();

		GC.SuppressFinalize(this);
	}

	// Maps a message type to its toast colour-scheme class (shared with the legacy preview styles).
	private static string GetToastTypeClass(MessageType type) => type switch
	{
		MessageType.Warning => "preview-warning",
		MessageType.Error => "preview-error",
		MessageType.Critical => "preview-critical",
		MessageType.Success => "preview-success",
		_ => "preview-normal"
	};

	// Builds the full class list for a toast card: colour scheme, anchor side, and the current
	// entry / exit / de-stack animation state.
	private static string GetToastClasses(ToastItem item)
	{
		var animationClass = item.IsDeStacking
			? "toast-destack"
			: item.IsExiting
				? $"toast-exit-{GetAnimationName(item.ExitAnimation)}"
				: $"toast-enter-{GetAnimationName(item.EntryAnimation)}";

		return $"{GetToastTypeClass(item.Message.Type)} {animationClass}";
	}

	// Inline style carrying the resolved dimensions and the per-toast animation duration.
	private static string GetToastStyle(ToastItem item)
	{
		var durationMs = item.IsDeStacking ? _deStackDurationMs : item.AnimationDurationMs;
		var sb = new System.Text.StringBuilder();
		sb.Append("--pdchat-toast-anim-ms:").Append(durationMs.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("ms;");
		AppendStyle(sb, "min-width", item.MinWidth);
		AppendStyle(sb, "max-width", item.MaxWidth);
		AppendStyle(sb, "min-height", item.MinHeight);
		AppendStyle(sb, "max-height", item.MaxHeight);
		return sb.ToString();
	}

	private static void AppendStyle(System.Text.StringBuilder sb, string name, string? value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			sb.Append(name).Append(':').Append(value).Append(';');
		}
	}

	private static string GetAnimationName(PDChatToastAnimation animation) => animation switch
	{
		PDChatToastAnimation.None => "none",
		PDChatToastAnimation.Fade => "fade",
		PDChatToastAnimation.Grow => "grow",
		PDChatToastAnimation.Shrink => "shrink",
		PDChatToastAnimation.Slide => "slide",
		_ => "grow"
	};

	// Resolves the corner the toast stack anchors to: follow the minimized button when it is shown,
	// otherwise fall back to the configured headless anchor.
	private string GetToastAnchorClass()
	{
		var anchor = ChatService.MinimizedButtonPosition == PDChatButtonPosition.None
			? ChatService.ToastAnchor
			: ChatService.MinimizedButtonPosition;

		return anchor switch
		{
			PDChatButtonPosition.TopLeft => "toast-anchor-top-left",
			PDChatButtonPosition.TopRight => "toast-anchor-top-right",
			PDChatButtonPosition.BottomLeft => "toast-anchor-bottom-left",
			PDChatButtonPosition.BottomRight => "toast-anchor-bottom-right",
			PDChatButtonPosition.None => "toast-anchor-bottom-right",
			_ => "toast-anchor-bottom-right"
		};
	}

	// aria-live politeness: escalate to assertive for the most severe message types.
	private static string GetToastAriaLive(MessageType type)
		=> type is MessageType.Error or MessageType.Critical ? "assertive" : "polite";

	/// <summary>
	/// Represents a single live toast instance in the stack, together with its resolved presentation
	/// options and auto-dismiss timers.
	/// </summary>
	private sealed class ToastItem
	{
		public Guid Key { get; } = Guid.NewGuid();
		public required ChatMessage Message { get; init; }
		public PDChatToastAnimation EntryAnimation { get; init; }
		public PDChatToastAnimation ExitAnimation { get; init; }
		public double AnimationDurationMs { get; init; }
		public bool AutoDismiss { get; init; }
		public double DisplayDurationSeconds { get; init; }
		public bool ShowTitle { get; init; }
		public string? MinWidth { get; init; }
		public string? MaxWidth { get; init; }
		public string? MinHeight { get; init; }
		public string? MaxHeight { get; init; }

		public bool IsExiting { get; set; }
		public bool IsDeStacking { get; set; }
		public Timer? DismissTimer { get; set; }
		public Timer? RemovalTimer { get; set; }
		public bool Paused { get; set; }
		public double RemainingMs { get; set; }
		public long StartedTicks { get; set; }
	}

	/// <summary>
	/// Turns a completed form into an ordinary outbound message (issue #106).
	/// </summary>
	/// <remarks>
	/// Deliberately the same path a typed message takes - ChatService.SendMessage plus
	/// OnMessageSent - so a consumer needs no new wiring to receive answers, and the transcript
	/// keeps them in order alongside everything else.
	/// </remarks>
	private async Task OnFormSubmittedAsync(ChatFormSubmission submission)
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Message = DescribeSubmission(submission),
			Sender = User,
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow,
			FormSubmission = submission
		};

		ChatService.SendMessage(message);

		if (OnMessageSent.HasDelegate)
		{
			await OnMessageSent.InvokeAsync(message);
		}

		StateHasChanged();
	}

	/// <summary>
	/// Records that a form was dismissed, without sending anything.
	/// </summary>
	/// <remarks>
	/// Nothing goes to the chat service on purpose. Declining to answer is not a contribution to the
	/// conversation, and a message saying "the user ignored your questions" would invite the asker to
	/// press the point - which is exactly what making the form optional was meant to avoid.
	/// </remarks>
	private Task OnFormDismissedAsync(Guid formId)
	{
		_ = formId;

		StateHasChanged();

		return Task.CompletedTask;
	}

	/// <summary>
	/// Renders a submission as the plain text a human reads in the transcript.
	/// </summary>
	internal static string DescribeSubmission(ChatFormSubmission submission)
	{
		ArgumentNullException.ThrowIfNull(submission);

		var lines = new List<string>();

		foreach (var answer in submission.Answers)
		{
			// Skipped questions are listed rather than omitted, so the reader can see what was asked
			// and declined - the absence of a line would look like the question was never put.
			lines.Add(answer.WasSkipped
				? $"{answer.Question} - skipped"
				: $"{answer.Question} - {answer.Value}");
		}

		return lines.Count == 0
			? "(no answers)"
			: string.Join(Environment.NewLine, lines);
	}
}
