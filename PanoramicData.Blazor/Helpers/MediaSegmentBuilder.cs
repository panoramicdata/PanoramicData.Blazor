namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Derives <see cref="PDMediaSegment"/>s from <see cref="PDMediaMarker"/>s, marker to marker.
/// </summary>
internal static class MediaSegmentBuilder
{
	/// <summary>
	/// Derives segments from markers: after sorting, each marker opens a segment that runs to the next marker (or, for
	/// the last, to <paramref name="duration"/>). Markers of a skipped kind are ignored; markers of an end kind close
	/// the running segment without opening another. Segments that would be empty, or start at or after the end of the
	/// media, are left out.
	/// </summary>
	/// <param name="markers">The markers, in any order.</param>
	/// <param name="duration">The duration of the media.</param>
	/// <param name="options">The options.</param>
	/// <returns>The segments, in time order.</returns>
	internal static IReadOnlyList<PDMediaSegment> FromMarkers(IEnumerable<PDMediaMarker?>? markers, TimeSpan duration, MediaSegmentsFromMarkersOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		var skip = ToSet(options.SkipKinds);
		var end = ToSet(options.EndKinds);
		var segments = new List<PDMediaSegment>();
		PDMediaMarker? open = null;
		foreach (var marker in MediaMarkerNavigator.Sort(markers).Where(m => !skip.Contains(MediaMarkerNavigator.GetKind(m))))
		{
			AddSegment(segments, open, marker.Position, duration);
			open = end.Contains(MediaMarkerNavigator.GetKind(marker)) ? null : marker;
		}

		AddSegment(segments, open, duration, duration);
		return options.MergeAdjacentSameKind ? MergeAdjacentSameKind(segments) : segments;
	}

	private static HashSet<string> ToSet(IReadOnlyCollection<string>? kinds)
		=> new((kinds ?? []).Select(k => k?.Trim() ?? string.Empty), MediaMarkerNavigator.KindComparer);

	// closes the segment opened by a marker at the given time, keeping it only if it has a length within the media
	private static void AddSegment(List<PDMediaSegment> segments, PDMediaMarker? opening, TimeSpan closing, TimeSpan duration)
	{
		if (opening is null)
		{
			return;
		}

		var start = opening.Position < TimeSpan.Zero ? TimeSpan.Zero : opening.Position;
		var endTime = closing > duration ? duration : closing;
		if (endTime > start)
		{
			segments.Add(new PDMediaSegment
			{
				Id = opening.Id,
				Start = start,
				End = endTime,
				Kind = MediaMarkerNavigator.GetKind(opening),
				Label = opening.Label,
				Detail = opening.Detail,
				Color = opening.Color,
				CssClass = opening.CssClass
			});
		}
	}

	private static List<PDMediaSegment> MergeAdjacentSameKind(List<PDMediaSegment> segments)
	{
		var merged = new List<PDMediaSegment>(segments.Count);
		foreach (var segment in segments)
		{
			if (merged.Count > 0 && Continues(merged[^1], segment))
			{
				merged[^1] = Extend(merged[^1], segment.End);
			}
			else
			{
				merged.Add(segment);
			}
		}

		return merged;
	}

	private static bool Continues(PDMediaSegment previous, PDMediaSegment next)
		=> previous.End == next.Start && MediaMarkerNavigator.KindComparer.Equals(previous.Kind, next.Kind);

	private static PDMediaSegment Extend(PDMediaSegment segment, TimeSpan end) => new()
	{
		Id = segment.Id,
		Start = segment.Start,
		End = end,
		Kind = segment.Kind,
		Label = segment.Label,
		Detail = segment.Detail,
		Color = segment.Color,
		CssClass = segment.CssClass
	};
}
