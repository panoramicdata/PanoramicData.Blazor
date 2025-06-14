namespace PanoramicData.Blazor.Services;

public class DumbChatService : IChatService, IDisposable
{
	private static readonly Random _random = new();

	public event Action<ChatMessage>? OnMessageReceived;

	public void Dispose()
	{
	}

	public void Initialize()
	{
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
		// Create a shared GUID for both thinking and final messages
		var responseId = Guid.NewGuid();

		// Send the "thinking" message (empty content)
		var thinkingMessage = new ChatMessage
		{
			Id = responseId,
			Sender = "DumbBot",
			Title = "Thinking...",
			Message = "...",
			Priority = MessagePriority.Normal
		};

		// Wait to simulate "typing"
		await Task.Delay(500);

		OnMessageReceived?.Invoke(thinkingMessage);

		// Wait to simulate "typing"
		await Task.Delay(1000);

		// Generate random priority for the final message
		var priorities = (MessagePriority[])Enum.GetValues(typeof(MessagePriority));
		var randomPriority = priorities[_random.Next(priorities.Length)];

		// Send the final response with same Id so UI can replace
		var response = new ChatMessage
		{
			Id = responseId,
			Sender = "DumbBot",
			Title = "Auto-reply",
			Message = $"You said: \"{userMessage.Message}\"",
			Priority = randomPriority
		};

		OnMessageReceived?.Invoke(response);
	}

}
