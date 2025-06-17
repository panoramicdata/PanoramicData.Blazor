namespace PanoramicData.Blazor.Models;

public class ChatMessage()
{
	public required Guid Id { get; init; }

	public required string Sender { get; init; }

	public required string Title { get; set; }

	public required string Message { get; set; }

	public required MessageType Type { get; set; } = MessageType.Normal;

	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
