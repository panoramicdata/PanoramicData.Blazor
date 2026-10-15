namespace PanoramicData.Blazor.Models;

/// <summary>
/// A timed line of text in a <see cref="PDMediaSegment"/>, such as a line of a transcript.
/// </summary>
public sealed class PDMediaTextLine
{
	/// <summary>
	/// Gets the offset from the start of the media at which the line begins.
	/// </summary>
	public TimeSpan Start { get; init; }

	/// <summary>
	/// Gets the offset at which the line ends, or <c>null</c> when it lasts until the next line starts.
	/// </summary>
	public TimeSpan? End { get; init; }

	/// <summary>
	/// Gets the text of the line.
	/// </summary>
	public string Text { get; init; } = string.Empty;

	/// <summary>
	/// Gets an optional name of who is speaking, shown before the text.
	/// </summary>
	public string? Speaker { get; init; }
}
