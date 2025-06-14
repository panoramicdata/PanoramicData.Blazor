namespace PanoramicData.Blazor.Interfaces;

public interface IChatService
{
	/// <summary>
	/// Event triggered when a new message is received.
	/// </summary>
	event Action<ChatMessage>? OnMessageReceived;

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
