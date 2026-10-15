using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Models;

/// <summary>
/// A stretch of the timeline of a <see cref="PDMediaPlayer"/> with a start and an end, such as a song, a talking
/// section or an advertisement. Segments may overlap and may leave gaps between them. A segment whose end is not
/// after its start is ignored.
/// </summary>
public sealed class PDMediaSegment
{
	/// <summary>
	/// Gets the identifier of the segment. The player does not interpret it; use it to find the host's own record.
	/// </summary>
	public string Id { get; init; } = string.Empty;

	/// <summary>
	/// Gets the offset of the start of the segment from the start of the media.
	/// </summary>
	public TimeSpan Start { get; init; }

	/// <summary>
	/// Gets the offset of the end of the segment from the start of the media. A segment that runs past the end of the
	/// media is drawn up to the end.
	/// </summary>
	public TimeSpan End { get; init; }

	/// <summary>
	/// Gets the kind of the segment, such as <c>song</c>, <c>talk</c>, <c>ad</c> or <c>jingle</c>. Kinds are compared
	/// without regard to case, drive the colour of the segment and can be shown or hidden as a group.
	/// </summary>
	public string Kind { get; init; } = string.Empty;

	/// <summary>
	/// Gets the short text shown on the segment's bar, in the list and in the now playing strip, such as
	/// <c>Artist: Title</c> or <c>Presenter talking</c>.
	/// </summary>
	public string? Label { get; init; }

	/// <summary>
	/// Gets optional further short text shown beneath the label in the list and the now playing strip.
	/// </summary>
	public string? Detail { get; init; }

	/// <summary>
	/// Gets optional long plain text for the segment, such as a transcript, shown in the transcript pane while the
	/// segment plays. When <see cref="Lines"/> is set, the lines are shown instead.
	/// </summary>
	public string? Text { get; init; }

	/// <summary>
	/// Gets optional timed lines of text, such as a transcript, shown in the transcript pane while the segment plays,
	/// with the line being spoken highlighted. Selecting a line seeks to it.
	/// </summary>
	public IReadOnlyList<PDMediaTextLine>? Lines { get; init; }

	/// <summary>
	/// Gets the lane to draw the segment in, counting from zero, or <c>null</c> (the default) to let the player pack
	/// overlapping segments into lanes. Lanes beyond <see cref="PDMediaPlayer.MaxSegmentLanes"/> are drawn in the last
	/// lane.
	/// </summary>
	public int? Lane { get; init; }

	/// <summary>
	/// Gets an optional CSS class added to the segment's bar and list item.
	/// </summary>
	public string? CssClass { get; init; }

	/// <summary>
	/// Gets an optional CSS colour for this segment, overriding the colour of its kind.
	/// </summary>
	public string? Color { get; init; }

	/// <summary>
	/// Gets an optional confidence, from 0 to 1, for a segment found automatically. It is shown in the tooltip.
	/// </summary>
	public double? Confidence { get; init; }

	/// <summary>
	/// Gets an optional name for where the segment came from, such as the detector or the playlist that produced it.
	/// It is shown in the tooltip.
	/// </summary>
	public string? Source { get; init; }

	/// <summary>
	/// Gets the length of the segment, or zero when its end is not after its start.
	/// </summary>
	public TimeSpan Length => End > Start ? End - Start : TimeSpan.Zero;

	/// <summary>
	/// Gets whether the segment has a positive length; segments that do not are ignored.
	/// </summary>
	public bool IsValid => End > Start;

	/// <summary>
	/// Derives segments from markers, marker to marker: after sorting, each marker opens a segment that runs to the
	/// next marker, and the last runs to <paramref name="duration"/>. Each segment takes its opening marker's
	/// <see cref="PDMediaMarker.Id"/>, kind, label, detail, colour and class.
	/// </summary>
	/// <param name="markers">The markers, in any order.</param>
	/// <param name="duration">The duration of the media, where the last segment ends.</param>
	/// <returns>The segments, in time order.</returns>
	public static IReadOnlyList<PDMediaSegment> FromMarkers(IEnumerable<PDMediaMarker?>? markers, TimeSpan duration)
		=> MediaSegmentBuilder.FromMarkers(markers, duration, new MediaSegmentsFromMarkersOptions());

	/// <summary>
	/// Derives segments from markers, marker to marker, with options to ignore some kinds, to end a segment without
	/// starting another, and to merge neighbouring segments of the same kind. See
	/// <see cref="FromMarkers(IEnumerable{PDMediaMarker}, TimeSpan)"/>.
	/// </summary>
	/// <param name="markers">The markers, in any order.</param>
	/// <param name="duration">The duration of the media, where the last segment ends.</param>
	/// <param name="options">The options.</param>
	/// <returns>The segments, in time order.</returns>
	public static IReadOnlyList<PDMediaSegment> FromMarkers(IEnumerable<PDMediaMarker?>? markers, TimeSpan duration, MediaSegmentsFromMarkersOptions options)
		=> MediaSegmentBuilder.FromMarkers(markers, duration, options);
}
