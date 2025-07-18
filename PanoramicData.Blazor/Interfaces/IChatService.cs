
namespace PanoramicData.Blazor.Interfaces;

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
