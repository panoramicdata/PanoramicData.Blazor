namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines the contract for a chat service consumed by the <see cref="PanoramicData.Blazor.PDChat"/> component.
/// </summary>
public interface IChatService
{
	/// <summary>
	/// Returns true if the chat service is active and ready to send/receive messages.
	/// If not, the UI should either not display the chat interface, grey out the chat bubble, or display a "Chat is unavailable" message.
	/// </summary>
	bool IsLive { get; }

	/// <summary>
	/// Gets the current dock mode preference for the chat interface.
	/// This allows the service to remember user's preferred chat positioning.
	/// </summary>
	PDChatDockMode DockMode { get; set; }

	/// <summary>
	/// Gets the current dock mode preference for the chat interface.
	/// This allows the service to remember user's preferred chat positioning.
	/// </summary>
	PDChatDockMode PreferredDockMode { get; set; }

	/// <summary>
	/// Gets or sets the dock mode to restore to when coming out of minimized state.
	/// When the chat is minimized and then restored, it will return to this mode.
	/// </summary>
	PDChatDockMode RestoreMode { get; set; }

	/// <summary>
	/// Gets or sets the position where the minimized chat button appears.
	/// This is independent of the RestoreMode and defaults to BottomRight.
	/// Use None when you want to provide your own chat trigger button.
	/// </summary>
	PDChatButtonPosition MinimizedButtonPosition { get; set; }

	/// <summary>
	/// Gets or sets whether sound notifications are muted.
	/// </summary>
	bool IsMuted { get; set; }

	/// <summary>
	/// Gets or sets the chat title displayed in the header.
	/// </summary>
	string Title { get; set; }

	/// <summary>
	/// Gets or sets whether the maximize/fullscreen button is available.
	/// </summary>
	bool IsMaximizePermitted { get; set; }

	/// <summary>
	/// Gets or sets whether the canvas/coding panel is available in fullscreen mode.
	/// </summary>
	bool IsCanvasUsePermitted { get; set; }

	/// <summary>
	/// Gets or sets whether the clear chat button is available.
	/// </summary>
	bool IsClearPermitted { get; set; }

	/// <summary>
	/// Gets or sets whether the user may type and send messages.
	/// When false, the message input (text area and send button) is not rendered and
	/// the chat acts as a notifications-only ("toast-only") surface. Defaults to true.
	/// </summary>
	bool IsInputPermitted
	{
		get => true;
		set { }
	}

	/// <summary>
	/// Gets or sets an optional message shown where the input would normally appear when
	/// <see cref="IsInputPermitted"/> is false, e.g. to explain why sending is unavailable.
	/// When null or empty (the default), nothing is rendered in place of the input, giving a
	/// pure notifications-only surface. The message is rendered as plain text.
	/// </summary>
	string? InputDisabledMessage
	{
		get => null;
		set { }
	}

	/// <summary>
	/// Gets or sets whether the chat should auto-restore when new messages arrive.
	/// </summary>
	bool AutoRestoreOnNewMessage { get; set; }

	/// <summary>
	/// Gets or sets whether messages use full width layout.
	/// </summary>
	bool UseFullWidthMessages { get; set; }

	/// <summary>
	/// Gets or sets the message metadata display mode.
	/// </summary>
	MessageMetadataDisplayMode MessageMetadataDisplayMode { get; set; }

	/// <summary>
	/// Gets or sets whether user icons are shown in messages.
	/// </summary>
	bool ShowMessageUserIcon { get; set; }

	/// <summary>
	/// Gets or sets whether user names are shown in messages.
	/// </summary>
	bool ShowMessageUserName { get; set; }

	/// <summary>
	/// Gets or sets whether timestamps are shown in messages.
	/// </summary>
	bool ShowMessageTimestamp { get; set; }

	/// <summary>
	/// Gets or sets the format string for message timestamps.
	/// </summary>
	string MessageTimestampFormat { get; set; }

	/// <summary>
	/// Gets or sets whether the last message should be shown as a preview when the chat is minimized and a new message arrives.
	/// </summary>
	[Obsolete("Superseded by the richer toast API. Use " + nameof(ToastEnabled) + " instead. This member maps onto " + nameof(ToastEnabled) + " for backward compatibility.")]
	bool ShowLastMessage { get; set; }

	/// <summary>
	/// Gets or sets the duration in seconds for which the last message preview is shown before automatically hiding.
	/// Default is 5 seconds.
	/// </summary>
	[Obsolete("Superseded by the richer toast API. Use " + nameof(ToastDisplayDurationSeconds) + " instead. This member maps onto " + nameof(ToastDisplayDurationSeconds) + " for backward compatibility.")]
	double ShowLastMessageDurationSeconds { get; set; }

	// ==========================================================================================
	// Toast notification API
	//
	// PDChat can double as a toast surface: when a message arrives while the chat is "closed"
	// (minimized to its button, or fully hidden when MinimizedButtonPosition is None) the chat
	// animates the message into view, optionally auto-dismissing after a configurable time.
	// Toasts stack (oldest at the top) and each runs its own independent dismiss timer.
	//
	// All members below carry default interface implementations so existing IChatService
	// implementations continue to compile without change. ToastEnabled / ToastDisplayDurationSeconds
	// default to the legacy ShowLastMessage / ShowLastMessageDurationSeconds members so that the
	// previous behaviour is preserved for consumers that have not yet migrated.
	// ==========================================================================================

	/// <summary>
	/// Gets or sets whether PDChat shows arriving messages as animated toasts while it is closed.
	/// Supersedes <see cref="ShowLastMessage"/>; defaults to that member's value for backward compatibility.
	/// </summary>
	bool ToastEnabled
	{
#pragma warning disable CS0618 // Legacy member intentionally used as the default backing value.
		get => ShowLastMessage;
		set => ShowLastMessage = value;
#pragma warning restore CS0618
	}

	/// <summary>Gets or sets the default animation used when a toast appears. Defaults to <see cref="PDChatToastAnimation.Grow"/>.</summary>
	PDChatToastAnimation ToastEntryAnimation
	{
		get => PDChatToastAnimation.Grow;
		set { }
	}

	/// <summary>Gets or sets the default animation used when a toast is dismissed. Defaults to <see cref="PDChatToastAnimation.Shrink"/>.</summary>
	PDChatToastAnimation ToastExitAnimation
	{
		get => PDChatToastAnimation.Shrink;
		set { }
	}

	/// <summary>Gets or sets the default duration, in milliseconds, of the toast entry / exit transitions. Defaults to 250ms.</summary>
	double ToastAnimationDurationMs
	{
		get => 250d;
		set { }
	}

	/// <summary>Gets or sets whether toasts auto-dismiss after <see cref="ToastDisplayDurationSeconds"/>. Defaults to true.</summary>
	bool ToastAutoDismiss
	{
		get => true;
		set { }
	}

	/// <summary>
	/// Gets or sets how long, in seconds, a toast stays on screen before auto-dismissing.
	/// Supersedes <see cref="ShowLastMessageDurationSeconds"/>; defaults to that member's value for backward compatibility.
	/// </summary>
	double ToastDisplayDurationSeconds
	{
#pragma warning disable CS0618 // Legacy member intentionally used as the default backing value.
		get => ShowLastMessageDurationSeconds;
		set => ShowLastMessageDurationSeconds = value;
#pragma warning restore CS0618
	}

	/// <summary>Gets or sets whether the message title is shown in toasts by default. Defaults to true.</summary>
	bool ToastShowTitle
	{
		get => true;
		set { }
	}

	/// <summary>Gets or sets the default toast minimum width (any valid CSS length). Defaults to "200px".</summary>
	string ToastMinWidth
	{
		get => "200px";
		set { }
	}

	/// <summary>Gets or sets the default toast maximum width (any valid CSS length). Defaults to "300px".</summary>
	string ToastMaxWidth
	{
		get => "300px";
		set { }
	}

	/// <summary>Gets or sets the default toast minimum height (any valid CSS length). Empty means unconstrained.</summary>
	string ToastMinHeight
	{
		get => string.Empty;
		set { }
	}

	/// <summary>Gets or sets the default toast maximum height (any valid CSS length). Empty means unconstrained.</summary>
	string ToastMaxHeight
	{
		get => string.Empty;
		set { }
	}

	/// <summary>
	/// Gets or sets the maximum number of toasts visible at once. When a new toast would exceed this,
	/// the oldest is dismissed to make room. Defaults to 5.
	/// </summary>
	int ToastMaxVisible
	{
		get => 5;
		set { }
	}

	/// <summary>
	/// Gets or sets the corner the toast stack anchors to when the chat is fully hidden
	/// (<see cref="MinimizedButtonPosition"/> is <see cref="PDChatButtonPosition.None"/>).
	/// When the minimized button is visible, the stack follows the button's position instead.
	/// Defaults to <see cref="PDChatButtonPosition.BottomRight"/>.
	/// </summary>
	PDChatButtonPosition ToastAnchor
	{
		get => PDChatButtonPosition.BottomRight;
		set { }
	}

	/// <summary>
	/// Gets the current list of chat messages.
	/// </summary>
	IReadOnlyList<ChatMessage> Messages { get; }

	// ==========================================================================================
	// Conversation-addressed API
	//
	// Everything above is singular: one Messages list, one SendMessage, one OnMessageReceived. A host that
	// wants several conversations open at once - tabs, say - has no way to say which conversation a message
	// belongs to, so a reply arriving from any of them appends to whichever transcript happens to be selected.
	// That failure is intermittent, depends on timing, and presents as a rendering bug.
	//
	// The members below address a conversation explicitly. All carry default interface implementations that map
	// onto the singular members using a single implicit conversation, so every existing implementation compiles
	// and behaves identically - the same technique the toast API above already uses.
	//
	// The defaults deliberately REFUSE an unrecognised conversation id rather than falling back to the single
	// conversation. Serving the one transcript a service has, for an id it has never heard of, would be the
	// misdelivery bug moved out of the UI and into the interface, where it is harder to see.
	//
	// TRAP WHEN OPTING IN FROM A DERIVED CLASS. If a base class already implements IChatService, adding these
	// members to a class derived from it is NOT enough - the interface mapping was fixed at the base, so the new
	// members are ordinary class members and a caller holding an IChatService still gets the defaults. Nothing
	// warns about it: the type compiles, the members exist, and SupportsConversations reads true off the concrete
	// type and false off the interface. Re-state the interface in the derived type's base list
	// (class MyService : MyServiceBase, IChatService) so that the members bind.
	// ==========================================================================================

	/// <summary>
	/// Gets a value indicating whether this service can hold more than one conversation at a time.
	/// Defaults to <c>false</c>.
	/// </summary>
	/// <remarks>
	/// A consumer must check this before relying on <see cref="OnConversationMessageReceived"/>, whose default
	/// implementation accepts subscriptions and never fires. Without the flag a consumer would subscribe, see
	/// nothing, and have no way to tell that from a service that simply had no traffic.
	/// </remarks>
	bool SupportsConversations => false;

	/// <summary>
	/// Gets or sets the conversation currently being shown. Defaults to
	/// <see cref="ChatConversation.ImplicitConversationId"/>, and the setter is ignored, for a service that does
	/// not support conversations.
	/// </summary>
	Guid ActiveConversationId
	{
		get => ChatConversation.ImplicitConversationId;
		set { }
	}

	/// <summary>
	/// Gets the messages belonging to one conversation, or an empty list if this service does not have it.
	/// </summary>
	/// <remarks>
	/// An unrecognised id yields nothing rather than <see cref="Messages"/>. Returning the wrong transcript
	/// would be indistinguishable, to a caller, from the conversation genuinely containing those messages.
	/// </remarks>
	IReadOnlyList<ChatMessage> GetMessages(Guid conversationId)
		=> conversationId == ActiveConversationId ? Messages : [];

	/// <summary>
	/// Sends a message to a specific conversation.
	/// </summary>
	/// <param name="conversationId">The conversation to send to.</param>
	/// <param name="chatMessage">The message to send.</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when this service does not have the requested conversation.
	/// </exception>
	/// <remarks>
	/// Throwing is deliberate. A caller addressing a conversation the service does not have has a bug, and it
	/// should surface where it happened rather than as a message appearing in an unrelated conversation several
	/// seconds later.
	/// </remarks>
	void SendMessage(Guid conversationId, ChatMessage chatMessage)
	{
		if (conversationId != ActiveConversationId)
		{
			throw new InvalidOperationException(
				$"This chat service does not have conversation {conversationId}. " +
				$"It supports a single conversation ({ActiveConversationId}); check {nameof(SupportsConversations)} " +
				"before addressing conversations individually.");
		}

		SendMessage(chatMessage);
	}

	/// <summary>
	/// Raised when a message arrives, reporting the conversation it belongs to so that a consumer can route it
	/// without guessing at "the current one".
	/// </summary>
	/// <remarks>
	/// The default implementation accepts subscriptions and never raises, because a service that does not
	/// support conversations has nothing to disambiguate. Check <see cref="SupportsConversations"/> and fall
	/// back to <see cref="OnMessageReceived"/> when it is <c>false</c>.
	/// </remarks>
	event Action<Guid, ChatMessage>? OnConversationMessageReceived
	{
		add { }
		remove { }
	}

	/// <summary>
	/// Event triggered when a new message is received.
	/// </summary>
	event Action<ChatMessage>? OnMessageReceived;

	/// <summary>
	/// Event triggered when going on/offline.
	/// </summary>
	event Action<bool>? OnLiveStatusChanged;

	/// <summary>
	/// Event triggered when the preferred dock mode changes.
	/// </summary>
	event Action<PDChatDockMode>? OnDockModeChanged;

	/// <summary>
	/// Event triggered when mute status changes.
	/// </summary>
	event Action<bool>? OnMuteStatusChanged;

	/// <summary>
	/// Event triggered when any chat configuration property changes.
	/// </summary>
	event Action? OnConfigurationChanged;

	/// <summary>
	/// Called by UI when user submits a message
	/// </summary>
	void SendMessage(ChatMessage chatMessage);

	/// <summary>
	/// Optionally called by UI to initialize the service.
	/// </summary>
	void Initialize();

	/// <summary>
	/// Optionally called by UI to dispose of the service.
	/// </summary>
	void Dispose();

	/// <summary>
	/// Clears all chat messages.
	/// </summary>
	void ClearMessages();
}
