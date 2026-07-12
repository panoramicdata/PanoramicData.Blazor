namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a single message in a chat conversation.
/// </summary>
public class ChatMessage()
{
	/// <summary>
	/// A unique id per message.  This allows existing messages to be updated.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>Gets the sender of this message.</summary>
	public required ChatMessageSender Sender { get; init; }

	/// <summary>Gets or sets an optional title displayed above the message body.</summary>
	public string? Title { get; set; }

	/// <summary>Gets or sets the message body text.</summary>
	public required string Message { get; set; }

	/// <summary>Gets or sets a value indicating whether <see cref="Title"/> is interpreted as HTML markup.</summary>
	public bool IsTitleHtml { get; set; }

	/// <summary>Gets or sets a value indicating whether <see cref="Message"/> is interpreted as HTML markup.</summary>
	public bool IsMessageHtml { get; set; }

	/// <summary>Gets or sets the semantic type of this message, which controls its visual presentation.</summary>
	public required MessageType Type { get; set; } = MessageType.Normal;

	/// <summary>Gets or sets the UTC timestamp when the message was created. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</summary>
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
