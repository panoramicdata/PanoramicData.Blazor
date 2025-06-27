namespace PanoramicData.Blazor.Models;

public class ChatMessage()
{
	/// <summary>
	/// A unique id per message.  This allows existing messages to be updated.
	/// </summary>
	public required Guid Id { get; init; }

	public required ChatMessageSender Sender { get; init; }

	public string? Title { get; set; }

	public required string Message { get; set; }

	public required MessageType Type { get; set; } = MessageType.Normal;

	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
