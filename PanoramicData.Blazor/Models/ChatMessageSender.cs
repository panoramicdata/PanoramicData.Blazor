namespace PanoramicData.Blazor.Models;

/// <summary>
/// Describes the sender of a <see cref="ChatMessage"/>.
/// </summary>
public class ChatMessageSender
{
	/// <summary>Gets the display name of the sender.</summary>
	public required string Name { get; init; }

	/// <summary>Gets a value indicating whether this sender is the currently authenticated user.</summary>
	public bool IsUser { get; init; }

	/// <summary>Gets a value indicating whether this sender is a human (as opposed to a bot or automated system).</summary>
	public bool IsHuman { get; init; }

	/// <summary>Gets a value indicating whether this sender represents a support agent.</summary>
	public bool IsSupport { get; init; }

	/// <summary>Gets an optional image URL, such as the person's avatar, shown with each of this sender's messages.</summary>
	/// <remarks>Used when a message has no <see cref="ChatMessage.AuthorImageUrl"/> or <see cref="ChatMessage.AuthorImageSvg"/> of its own.</remarks>
	public string? ImageUrl { get; init; }
}