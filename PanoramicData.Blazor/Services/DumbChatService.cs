namespace PanoramicData.Blazor.Services;

public class DumbChatService : IChatService, IDisposable
{
	private static readonly Random _random = new();
	private static readonly MessageType[] _messageTypes = [.. Enum.GetValues<MessageType>().Except([MessageType.Typing])];
	private bool _isInitialized;
	private bool _isOnline;
	private Timer? _timer;

	public event Action<ChatMessage>? OnMessageReceived;
	public event Action<bool>? OnLiveStatusChanged;

	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}

		// Start the timer to send a time check every minute
		_timer = new Timer(SendTimeCheck, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
		_isInitialized = true;
		_isOnline = true;
	}

	public bool IsLive => _isInitialized && _isOnline;

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

		// Add some basic behaviours if the user message contains certain keywords:
		// - "away" or "offline" or "bio-break", simulate going offline for 10 second.
		// - "help", provide a list of commands.
		// - otherwise echo what the user typed.

		if (userMessage.Message.Contains("away", StringComparison.OrdinalIgnoreCase) ||
			userMessage.Message.Contains("offline", StringComparison.OrdinalIgnoreCase) ||
			userMessage.Message.Contains("bio-break", StringComparison.OrdinalIgnoreCase))
		{
			// Simulate going offline for 10 seconds
			_isOnline = false;
			OnLiveStatusChanged?.Invoke(_isOnline);
			OnMessageReceived?.Invoke(new ChatMessage
			{
				Id = responseId,
				Sender = "DumbBot",
				Title = "Going Offline",
				Message = "I'm going offline for a short break. Please wait...",
				Type = MessageType.Warning
			});

			await Task.Delay(10000);

			// After the break, come back online
			_isOnline = true;
			OnLiveStatusChanged?.Invoke(_isOnline);
			OnMessageReceived?.Invoke(new ChatMessage
			{
				Id = Guid.NewGuid(),
				Sender = "DumbBot",
				Title = "Back Online",
				Message = "I'm back online! How can I assist you?",
				Type = MessageType.Normal
			});
			return;
		}

		if (userMessage.Message.Contains("help", StringComparison.OrdinalIgnoreCase))
		{
			// Provide a list of commands
			var helpMessage = new ChatMessage
			{
				Id = responseId,
				Sender = "DumbBot",
				Title = "Help",
				Message = "Available commands: 'help', 'go away'.",
				Type = MessageType.Normal
			};
			OnMessageReceived?.Invoke(helpMessage);
			return; // No further response needed
		}

		// Send the final response with same Id so UI can replace
		// Generate random type for the response

		OnMessageReceived?.Invoke(new ChatMessage
		{
			Id = responseId,
			Sender = "DumbBot",
			Message = $"You said: \"{userMessage.Message}\"",
			Type = _messageTypes[_random.Next(_messageTypes.Length)]
		});
	}
}
