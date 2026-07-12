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
	Success = 5
}