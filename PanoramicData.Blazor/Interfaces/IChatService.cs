namespace PanoramicData.Blazor.Interfaces;

public interface IChatService
{
	/// <summary>
	/// Returns true if the chat service is active and ready to send/receive messages.
	/// If not, the UI should either not display the chat interface, grey out the chat bubble, or display a "Chat is unavailable" message.
	/// </summary>
	bool IsLive { get; }

	/// <summary>
	/// Event triggered when a new message is received.
	/// </summary>
	event Action<ChatMessage>? OnMessageReceived;

	/// <summary>
	/// Event triggered when going on/offline.
	/// </summary>
	event Action<bool>? OnLiveStatusChanged;

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
}
