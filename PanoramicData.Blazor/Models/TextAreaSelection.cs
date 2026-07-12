namespace PanoramicData.Blazor.Models;

/// <summary>
/// Holds the text selection state of a textarea element, as returned from JavaScript interop.
/// </summary>
public class TextAreaSelection
{
	/// <summary>Gets or sets the character position of the end of the selection.</summary>
	public int End { get; set; }

	/// <summary>Gets or sets the character position of the start of the selection.</summary>
	public int Start { get; set; }

	/// <summary>Gets or sets the current text value of the textarea.</summary>
	public string Value { get; set; } = string.Empty;
}
