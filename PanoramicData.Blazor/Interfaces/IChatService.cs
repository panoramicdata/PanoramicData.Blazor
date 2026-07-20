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
