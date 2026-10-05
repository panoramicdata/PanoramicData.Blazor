namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// The text box a chat message is composed in, whatever wrote the text: the keyboard, dictation, or both.
/// </summary>
/// <remarks>
/// <see cref="PanoramicData.Blazor.PDMessages"/> implements this, and <see cref="PanoramicData.Blazor.PDChat"/>'s Voice
/// control writes through it, so dictated words land where typed ones do and can be edited before sending.
/// </remarks>
public interface IChatInput
{
	/// <summary>Gets the text currently in the box.</summary>
	string Text { get; }

	/// <summary>Gets a value indicating whether the box has focus, meaning the user may be editing it.</summary>
	bool IsFocused { get; }

	/// <summary>Appends text to the end of the box, separated from what is already there by a space.</summary>
	/// <param name="text">The text to add.</param>
	Task AppendAsync(string text);

	/// <summary>Sends the box's text exactly as pressing Send would; does nothing when there is nothing to send.</summary>
	Task SendAsync();
}
