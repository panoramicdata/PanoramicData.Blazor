namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies the semantic type of a <see cref="ChatMessage"/>, which controls its visual styling in the chat UI.
/// </summary>
public enum MessageType
{
	/// <summary>A placeholder message indicating the sender is composing a reply.</summary>
	Typing = 0,
	/// <summary>A standard informational message.</summary>
	Normal = 1,
	/// <summary>A warning message, typically rendered with an amber highlight.</summary>
	Warning = 2,
	/// <summary>An error message, typically rendered with a red highlight.</summary>
	Error = 3,
	/// <summary>A critical-severity message, typically rendered with a strong red highlight.</summary>
	Critical = 4,
	/// <summary>A success message, typically rendered with a green highlight.</summary>
	Success = 5,

	/// <summary>
	/// Carries an inline question form (issue #106).
	/// </summary>
	/// <remarks>
	/// Rendering is actually driven by <c>ChatMessage.Form</c> being non-null, so a consumer that
	/// sets the payload but leaves the type as Normal still gets a form. This value exists so a
	/// consumer can style or filter form messages without inspecting the payload.
	/// </remarks>
	Form = 6,

	/// <summary>
	/// A message that is genuinely informational, rather than ordinary conversation.
	/// </summary>
	/// <remarks>
	/// Tinted, where Normal is not. Before this existed, Normal itself carried the tint - which meant
	/// every message in a conversation was coloured, so the colour said nothing and a real FYI had no
	/// way to stand out.
	/// </remarks>
	Information = 7
}