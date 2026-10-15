namespace PanoramicData.Blazor.Models;

/// <summary>
/// Options for <see cref="PDMediaSegment.FromMarkers(IEnumerable{PDMediaMarker}, TimeSpan, MediaSegmentsFromMarkersOptions)"/>.
/// </summary>
public sealed class MediaSegmentsFromMarkersOptions
{
	/// <summary>
	/// Gets the kinds of marker to ignore completely: they neither start nor end a segment. Kinds are compared
	/// without regard to case.
	/// </summary>
	public IReadOnlyCollection<string>? SkipKinds { get; init; }

	/// <summary>
	/// Gets the kinds of marker that end the running segment without starting another, leaving a gap until the next
	/// marker.
	/// </summary>
	public IReadOnlyCollection<string>? EndKinds { get; init; }

	/// <summary>
	/// Gets whether a segment that follows straight on from one of the same kind is merged into it. The merged
	/// segment keeps the first one's id, label and detail.
	/// </summary>
	public bool MergeAdjacentSameKind { get; init; }
}
