namespace PanoramicData.Blazor.Models;

/// <summary>
/// An event along the timeline of a <see cref="PDMediaPlayer"/>: a point (such as a click in the audio) or, when
/// <see cref="Length"/> is set, a span (such as a stretch of silence).
/// </summary>
public sealed class PDMediaMarker
{
	/// <summary>
	/// Gets the identifier of the marker. The player does not interpret it; use it to find the host's own record.
	/// </summary>
	public string Id { get; init; } = string.Empty;

	/// <summary>
	/// Gets the offset of the marker from the start of the media. A negative value is treated as zero, and a value
	/// past the end of the media is drawn at the end.
	/// </summary>
	public TimeSpan Position { get; init; }

	/// <summary>
	/// Gets the length of the marker, or <c>null</c> (or zero) for a point marker.
	/// </summary>
	public TimeSpan? Length { get; init; }

	/// <summary>
	/// Gets the kind of the marker, such as <c>dead-air</c> or <c>clipping</c>. Kinds are compared without regard
	/// to case, drive the colour of the marker and can be shown or hidden as a group.
	/// </summary>
	public string Kind { get; init; } = string.Empty;

	/// <summary>
	/// Gets the short text shown for the marker in the list and its tooltip.
	/// </summary>
	public string? Label { get; init; }

	/// <summary>
	/// Gets optional further text shown beneath the label in the marker list.
	/// </summary>
	public string? Detail { get; init; }

	/// <summary>
	/// Gets an optional CSS class added to the marker's tick on the seek bar and to its list item.
	/// </summary>
	public string? CssClass { get; init; }

	/// <summary>
	/// Gets an optional CSS colour for this marker, overriding the colour of its kind.
	/// </summary>
	public string? Color { get; init; }

	/// <summary>
	/// Gets whether the marker covers a span of time rather than a single point.
	/// </summary>
	public bool IsSpan => Length is { } length && length > TimeSpan.Zero;

	/// <summary>
	/// Gets the end of the marker: <see cref="Position"/> plus <see cref="Length"/> for a span, otherwise
	/// <see cref="Position"/>.
	/// </summary>
	public TimeSpan End => IsSpan ? Position + Length!.Value : Position;
}
