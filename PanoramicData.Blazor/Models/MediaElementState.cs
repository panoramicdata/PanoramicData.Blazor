namespace PanoramicData.Blazor.Models;

/// <summary>
/// A snapshot of a native media element, sent by the <see cref="PDMediaPlayer"/> script with every event it reports.
/// </summary>
public sealed class MediaElementState
{
	/// <summary>Gets or sets the current playback position, in seconds.</summary>
	public double CurrentTime { get; set; }

	/// <summary>
	/// Gets or sets the duration in seconds, or <c>null</c> while it is unknown or when it is infinite (see
	/// <see cref="IsInfinite"/>).
	/// </summary>
	public double? Duration { get; set; }

	/// <summary>Gets or sets whether the duration is infinite, as it is for a live stream.</summary>
	public bool IsInfinite { get; set; }

	/// <summary>Gets or sets whether the element is paused.</summary>
	public bool Paused { get; set; }

	/// <summary>Gets or sets whether playback has reached the end.</summary>
	public bool Ended { get; set; }

	/// <summary>Gets or sets the element's playback rate.</summary>
	public double PlaybackRate { get; set; } = 1;

	/// <summary>
	/// Gets or sets the buffered ranges as a flat list of start and end pairs, in seconds:
	/// <c>[start0, end0, start1, end1, ...]</c>.
	/// </summary>
	public IReadOnlyList<double>? Buffered { get; set; }

	/// <summary>Gets or sets the browser's <c>MediaError.code</c>, or <c>null</c> when there is no error.</summary>
	public int? ErrorCode { get; set; }

	/// <summary>Gets or sets the browser's <c>MediaError.message</c>, if any.</summary>
	public string? ErrorMessage { get; set; }
}
