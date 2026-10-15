namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Orders, filters and searches the segments of a <see cref="PDMediaPlayer"/>. Lookups by start use a binary search,
/// so they stay fast with many thousands of segments.
/// </summary>
public static class MediaSegmentNavigator
{
	/// <summary>
	/// Gets the kind of a segment with surrounding white space removed, or an empty string when it has none.
	/// </summary>
	/// <param name="segment">The segment.</param>
	/// <returns>The kind.</returns>
	public static string GetKind(PDMediaSegment segment)
	{
		ArgumentNullException.ThrowIfNull(segment);
		return segment.Kind?.Trim() ?? string.Empty;
	}

	/// <summary>
	/// Sorts segments by start and then by end, keeping the given order for ties, and drops nulls and segments whose
	/// end is not after their start.
	/// </summary>
	/// <param name="segments">The segments, in any order.</param>
	/// <returns>The sorted, valid segments.</returns>
	public static IReadOnlyList<PDMediaSegment> Sort(IEnumerable<PDMediaSegment?>? segments)
		=> segments is null ? [] : [.. segments.OfType<PDMediaSegment>().Where(s => s.IsValid).OrderBy(s => s.Start).ThenBy(s => s.End)];

	/// <summary>
	/// Keeps the segments whose kind is in a set.
	/// </summary>
	/// <param name="sorted">The segments, sorted.</param>
	/// <param name="visibleKinds">The kinds to keep, or <c>null</c> to keep every segment.</param>
	/// <returns>The kept segments, still sorted.</returns>
	public static IReadOnlyList<PDMediaSegment> Filter(IReadOnlyList<PDMediaSegment> sorted, IReadOnlySet<string>? visibleKinds)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		return MediaKinds.Filter(sorted, GetKind, visibleKinds);
	}

	/// <summary>
	/// Keeps the segments of one kind.
	/// </summary>
	/// <param name="sorted">The segments, sorted.</param>
	/// <param name="kind">The kind to keep.</param>
	/// <returns>The kept segments, still sorted.</returns>
	public static IReadOnlyList<PDMediaSegment> Filter(IReadOnlyList<PDMediaSegment> sorted, string kind)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		return MediaKinds.Filter(sorted, GetKind, kind);
	}

	/// <summary>
	/// Counts the segments of each kind, ordered by kind.
	/// </summary>
	/// <param name="segments">The segments.</param>
	/// <returns>Each kind with its number of segments.</returns>
	public static IReadOnlyList<KeyValuePair<string, int>> CountByKind(IEnumerable<PDMediaSegment> segments)
	{
		ArgumentNullException.ThrowIfNull(segments);
		return MediaKinds.CountByKind(segments, GetKind);
	}

	/// <summary>
	/// Finds the first segment that starts after <paramref name="cursor"/> plus <paramref name="tolerance"/>.
	/// </summary>
	/// <param name="sorted">The segments, sorted.</param>
	/// <param name="cursor">The position to search from.</param>
	/// <param name="tolerance">How far past the cursor a segment must start. Negative values count as zero.</param>
	/// <returns>The segment, or <c>null</c> when there is none.</returns>
	public static PDMediaSegment? Next(IReadOnlyList<PDMediaSegment> sorted, TimeSpan cursor, TimeSpan tolerance)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		return MediaTimelineSearch.Next(sorted, StartOf, cursor, tolerance);
	}

	/// <summary>
	/// Finds the last segment that starts before <paramref name="cursor"/> minus <paramref name="tolerance"/>, so that
	/// "previous" first returns to the start of the current segment and, within the tolerance of it, goes to the one
	/// before.
	/// </summary>
	/// <param name="sorted">The segments, sorted.</param>
	/// <param name="cursor">The position to search from.</param>
	/// <param name="tolerance">How far before the cursor a segment must start. Negative values count as zero.</param>
	/// <returns>The segment, or <c>null</c> when there is none.</returns>
	public static PDMediaSegment? Previous(IReadOnlyList<PDMediaSegment> sorted, TimeSpan cursor, TimeSpan tolerance)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		return MediaTimelineSearch.Previous(sorted, StartOf, cursor, tolerance);
	}

	/// <summary>
	/// Finds the segments that contain a position (start included, end excluded), in time order. Overlapping segments
	/// are all returned.
	/// </summary>
	/// <param name="sorted">The segments, sorted.</param>
	/// <param name="position">The position.</param>
	/// <returns>The segments containing the position.</returns>
	public static IReadOnlyList<PDMediaSegment> Active(IReadOnlyList<PDMediaSegment> sorted, TimeSpan position)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		var started = MediaTimelineSearch.Count(sorted, StartOf, position, inclusive: true);
		var active = new List<PDMediaSegment>();
		for (var i = 0; i < started; i++)
		{
			if (sorted[i].End > position)
			{
				active.Add(sorted[i]);
			}
		}

		return active;
	}

	/// <summary>
	/// Finds the timed line being spoken at a position: the last line that has started and, if it has an end, has not
	/// ended.
	/// </summary>
	/// <param name="lines">The lines, in any order.</param>
	/// <param name="position">The position.</param>
	/// <returns>The line, or <c>null</c>.</returns>
	public static PDMediaTextLine? ActiveLine(IReadOnlyList<PDMediaTextLine>? lines, TimeSpan position)
	{
		var line = lines?.Where(l => l.Start <= position).MaxBy(l => l.Start);
		return line is not null && (line.End is null || line.End > position) ? line : null;
	}

	private static TimeSpan StartOf(PDMediaSegment segment) => segment.Start;
}
