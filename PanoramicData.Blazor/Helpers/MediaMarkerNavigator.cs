namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Orders, filters and searches the markers of a <see cref="PDMediaPlayer"/>. Lookups on a sorted list use a binary
/// search, so they stay fast with many thousands of markers.
/// </summary>
public static class MediaMarkerNavigator
{
	/// <summary>
	/// Gets the comparer used for marker kinds: kinds are compared without regard to case.
	/// </summary>
	public static StringComparer KindComparer => StringComparer.OrdinalIgnoreCase;

	/// <summary>
	/// Gets the kind of a marker with surrounding white space removed, or an empty string when it has none.
	/// </summary>
	/// <param name="marker">The marker.</param>
	/// <returns>The kind.</returns>
	public static string GetKind(PDMediaMarker marker)
	{
		ArgumentNullException.ThrowIfNull(marker);
		return marker.Kind?.Trim() ?? string.Empty;
	}

	/// <summary>
	/// Sorts markers by position, keeping the given order for markers at the same position, and drops nulls.
	/// </summary>
	/// <param name="markers">The markers, in any order.</param>
	/// <returns>The sorted markers.</returns>
	public static IReadOnlyList<PDMediaMarker> Sort(IEnumerable<PDMediaMarker?>? markers)
		=> markers is null ? [] : [.. markers.OfType<PDMediaMarker>().OrderBy(m => m.Position)];

	/// <summary>
	/// Keeps the markers whose kind is in a set.
	/// </summary>
	/// <param name="sorted">The markers, sorted by position.</param>
	/// <param name="visibleKinds">The kinds to keep, or <c>null</c> to keep every marker.</param>
	/// <returns>The kept markers, still sorted.</returns>
	public static IReadOnlyList<PDMediaMarker> Filter(IReadOnlyList<PDMediaMarker> sorted, IReadOnlySet<string>? visibleKinds)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		if (visibleKinds is null)
		{
			return sorted;
		}

		var kinds = new HashSet<string>(visibleKinds, KindComparer);
		return [.. sorted.Where(m => kinds.Contains(GetKind(m)))];
	}

	/// <summary>
	/// Keeps the markers of one kind.
	/// </summary>
	/// <param name="sorted">The markers, sorted by position.</param>
	/// <param name="kind">The kind to keep.</param>
	/// <returns>The kept markers, still sorted.</returns>
	public static IReadOnlyList<PDMediaMarker> Filter(IReadOnlyList<PDMediaMarker> sorted, string kind)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		var wanted = kind?.Trim() ?? string.Empty;
		return [.. sorted.Where(m => KindComparer.Equals(GetKind(m), wanted))];
	}

	/// <summary>
	/// Counts the markers of each kind, ordered by kind.
	/// </summary>
	/// <param name="markers">The markers.</param>
	/// <returns>Each kind with its number of markers.</returns>
	public static IReadOnlyList<KeyValuePair<string, int>> CountByKind(IEnumerable<PDMediaMarker> markers)
	{
		ArgumentNullException.ThrowIfNull(markers);
		return [.. markers
			.GroupBy(GetKind, KindComparer)
			.Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
			.OrderBy(p => p.Key, KindComparer)];
	}

	/// <summary>
	/// Finds the first marker that starts after <paramref name="cursor"/> plus <paramref name="tolerance"/>, so
	/// that a marker just reached is not found again.
	/// </summary>
	/// <param name="sorted">The markers, sorted by position.</param>
	/// <param name="cursor">The position to search from.</param>
	/// <param name="tolerance">How far past the cursor a marker must start. Negative values count as zero.</param>
	/// <returns>The marker, or <c>null</c> when there is none.</returns>
	public static PDMediaMarker? Next(IReadOnlyList<PDMediaMarker> sorted, TimeSpan cursor, TimeSpan tolerance)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		var index = CountAtOrBefore(sorted, cursor + NonNegative(tolerance));
		return index < sorted.Count ? sorted[index] : null;
	}

	/// <summary>
	/// Finds the last marker that starts before <paramref name="cursor"/> minus <paramref name="tolerance"/>. Within
	/// the tolerance of a marker's start this is the marker before it; further in, it is the marker itself, so
	/// "previous" first returns to the start of the current marker.
	/// </summary>
	/// <param name="sorted">The markers, sorted by position.</param>
	/// <param name="cursor">The position to search from.</param>
	/// <param name="tolerance">How far before the cursor a marker must start. Negative values count as zero.</param>
	/// <returns>The marker, or <c>null</c> when there is none.</returns>
	public static PDMediaMarker? Previous(IReadOnlyList<PDMediaMarker> sorted, TimeSpan cursor, TimeSpan tolerance)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		var index = CountBefore(sorted, cursor - NonNegative(tolerance)) - 1;
		return index >= 0 ? sorted[index] : null;
	}

	/// <summary>
	/// Finds the current marker: the last one that starts at or before <paramref name="cursor"/>.
	/// </summary>
	/// <param name="sorted">The markers, sorted by position.</param>
	/// <param name="cursor">The position.</param>
	/// <returns>The marker, or <c>null</c> before the first marker.</returns>
	public static PDMediaMarker? Current(IReadOnlyList<PDMediaMarker> sorted, TimeSpan cursor)
	{
		ArgumentNullException.ThrowIfNull(sorted);
		var index = CountAtOrBefore(sorted, cursor) - 1;
		return index >= 0 ? sorted[index] : null;
	}

	private static TimeSpan NonNegative(TimeSpan value) => value < TimeSpan.Zero ? TimeSpan.Zero : value;

	// the number of markers whose position is at or before the given time
	private static int CountAtOrBefore(IReadOnlyList<PDMediaMarker> sorted, TimeSpan time)
	{
		int low = 0, high = sorted.Count;
		while (low < high)
		{
			var middle = low + ((high - low) / 2);
			if (sorted[middle].Position <= time)
			{
				low = middle + 1;
			}
			else
			{
				high = middle;
			}
		}

		return low;
	}

	// the number of markers whose position is strictly before the given time
	private static int CountBefore(IReadOnlyList<PDMediaMarker> sorted, TimeSpan time)
	{
		int low = 0, high = sorted.Count;
		while (low < high)
		{
			var middle = low + ((high - low) / 2);
			if (sorted[middle].Position < time)
			{
				low = middle + 1;
			}
			else
			{
				high = middle;
			}
		}

		return low;
	}
}
