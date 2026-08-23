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

	/// <summary>
	/// Gets or sets what has happened so far while this message is being composed, oldest first.
	/// </summary>
	/// <remarks>
	/// Rendered only while <see cref="Type"/> is <see cref="MessageType.Typing"/>, and ignored
	/// otherwise - once the answer arrives, how it was reached belongs in the answer, not beside it.
	/// Intended for the concrete things a reader would recognise ("Querying getNetworkWirelessSsids
	/// on the live network"), not a percentage: a step list that names real work is legible even when
	/// it scrolls past quickly, and a progress bar over an unknown number of steps is a fiction.
	/// </remarks>
	public IReadOnlyList<string>? ProgressSteps { get; set; }

	/// <summary>
	/// Gets or sets model reasoning to show, collapsed, against this in-progress message.
	/// </summary>
	/// <remarks>
	/// Rendered only while <see cref="Type"/> is <see cref="MessageType.Typing"/>. See
	/// <see cref="ChatThought"/> for why these are collapsed rather than shown inline.
	/// </remarks>
	public IReadOnlyList<ChatThought>? Thoughts { get; set; }

	/// <summary>
	/// Gets or sets the answer so far, for a service that can stream it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Rendered only while <see cref="Type"/> is <see cref="MessageType.Typing"/>. Kept separate from
	/// <see cref="Message"/> rather than reusing it, so that a partial answer can never be mistaken
	/// for a finished one by anything that reads a message off the transcript - a half-written
	/// diagnosis quoted into a ticket would be worse than no diagnosis.
	/// </para>
	/// <para>
	/// A streamed value is routinely mid-structure - half a table row, an unclosed code fence - so it
	/// is passed through <see cref="PartialMarkdown.Trim"/> before rendering rather than shown raw.
	/// </para>
	/// </remarks>
	public string? PartialMessage { get; set; }

	/// <summary>
	/// Gets or sets optional per-message overrides for the toast shown when this message arrives while the
	/// chat is closed. When <c>null</c>, the service-level <c>Toast*</c> defaults on
	/// <see cref="Interfaces.IChatService"/> are used. Individual override properties that are left <c>null</c>
	/// also fall back to the service defaults.
	/// </summary>
	public ChatToastOptions? ToastOptions { get; set; }
}
