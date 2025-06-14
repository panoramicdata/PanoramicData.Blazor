namespace PanoramicData.Blazor.Models;

public class ChatMessage()
{
	public required Guid Id { get; init; }

	public required string Sender { get; init; }

	public required string Title { get; init; }

	public required string Message { get; set; }

	public required MessagePriority Priority { get; init; } = MessagePriority.Normal;

	public bool IsThinking { get; set; }

	public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
