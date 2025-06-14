namespace PanoramicData.Blazor.Services;

public class DumbChatService : IChatService, IDisposable
{
	private static readonly Random _random = new();
	private static readonly MessageType[] _messageTypes = [.. Enum.GetValues<MessageType>().Except([MessageType.Typing])];
	private bool _isInitialized;
	private Timer? _timer;

	public event Action<ChatMessage>? OnMessageReceived;

	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}

		// Start the timer to send a time check every minute
		_timer = new Timer(SendTimeCheck, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
		_isInitialized = true;
	}

	private void SendTimeCheck(object? state)
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = "TimeBot",
			Title = "Time Check",
			Message = $"The current time is {DateTime.Now:T}",
			Type = MessageType.Normal,
			Timestamp = DateTimeOffset.Now
		};

		OnMessageReceived?.Invoke(message);
	}

	public void Dispose()
	{
		_timer?.Dispose();
	}

	public void SendMessage(ChatMessage message)
	{
		// Invoke the user message immediately
		OnMessageReceived?.Invoke(message);

		// Kick off the async reply workflow
		_ = RespondAsync(message);
	}

	private async Task RespondAsync(ChatMessage userMessage)
	{
		// Create a shared GUID for both typing and final messages
		var responseId = Guid.NewGuid();

		// Send the "typing" message (empty content)
		var typingMessage = new ChatMessage
		{
			Id = responseId,
			Sender = "DumbBot",
			Title = "Typing...",
			Message = "...",
			Type = MessageType.Typing,
		};

		// Wait 500-1000ms to simulate "delayed response"
		await Task.Delay(_random.Next(500, 3000));

		OnMessageReceived?.Invoke(typingMessage);

		// Wait for 1-2 seconds to simulate "typing"
		await Task.Delay(_random.Next(2000, 8000));

		// Send the final response with same Id so UI can replace
		// Generate random type for the response
		var response = new ChatMessage
		{
			Id = responseId,
			Sender = "DumbBot",
			Title = "Auto-reply",
			Message = $"You said: \"{userMessage.Message}\"",
			Type = _messageTypes[_random.Next(_messageTypes.Length)]
		};

		OnMessageReceived?.Invoke(response);
	}
}
