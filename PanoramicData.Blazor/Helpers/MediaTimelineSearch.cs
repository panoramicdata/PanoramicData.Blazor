namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Binary searches over items sorted by a time, shared by the marker and segment navigators of
/// <see cref="PDMediaPlayer"/>.
/// </summary>
internal static class MediaTimelineSearch
{
	/// <summary>
	/// Counts the items whose time is at or before (or, when <paramref name="inclusive"/> is false, strictly before)
	/// a given time.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="sorted">The items, sorted by <paramref name="timeOf"/>.</param>
	/// <param name="timeOf">Gets the time of an item.</param>
	/// <param name="time">The time to compare with.</param>
	/// <param name="inclusive">Whether items at exactly <paramref name="time"/> count.</param>
	/// <returns>The number of items.</returns>
	internal static int Count<T>(IReadOnlyList<T> sorted, Func<T, TimeSpan> timeOf, TimeSpan time, bool inclusive)
	{
		int low = 0, high = sorted.Count;
		while (low < high)
		{
			var middle = low + ((high - low) / 2);
			var itemTime = timeOf(sorted[middle]);
			if (itemTime < time || (inclusive && itemTime == time))
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

	/// <summary>
	/// Finds the first item whose time is after <paramref name="cursor"/> plus <paramref name="tolerance"/>.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="sorted">The items, sorted by <paramref name="timeOf"/>.</param>
	/// <param name="timeOf">Gets the time of an item.</param>
	/// <param name="cursor">The position to search from.</param>
	/// <param name="tolerance">How far past the cursor an item must be; negative values count as zero.</param>
	/// <returns>The item, or the default when there is none.</returns>
	internal static T? Next<T>(IReadOnlyList<T> sorted, Func<T, TimeSpan> timeOf, TimeSpan cursor, TimeSpan tolerance)
	{
		var index = Count(sorted, timeOf, cursor + NonNegative(tolerance), inclusive: true);
		return index < sorted.Count ? sorted[index] : default;
	}

	/// <summary>
	/// Finds the last item whose time is before <paramref name="cursor"/> minus <paramref name="tolerance"/>.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="sorted">The items, sorted by <paramref name="timeOf"/>.</param>
	/// <param name="timeOf">Gets the time of an item.</param>
	/// <param name="cursor">The position to search from.</param>
	/// <param name="tolerance">How far before the cursor an item must be; negative values count as zero.</param>
	/// <returns>The item, or the default when there is none.</returns>
	internal static T? Previous<T>(IReadOnlyList<T> sorted, Func<T, TimeSpan> timeOf, TimeSpan cursor, TimeSpan tolerance)
	{
		var index = Count(sorted, timeOf, cursor - NonNegative(tolerance), inclusive: false) - 1;
		return index >= 0 ? sorted[index] : default;
	}

	/// <summary>
	/// Finds the last item whose time is at or before <paramref name="cursor"/>.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="sorted">The items, sorted by <paramref name="timeOf"/>.</param>
	/// <param name="timeOf">Gets the time of an item.</param>
	/// <param name="cursor">The position.</param>
	/// <returns>The item, or the default before the first.</returns>
	internal static T? Current<T>(IReadOnlyList<T> sorted, Func<T, TimeSpan> timeOf, TimeSpan cursor)
	{
		var index = Count(sorted, timeOf, cursor, inclusive: true) - 1;
		return index >= 0 ? sorted[index] : default;
	}

	private static TimeSpan NonNegative(TimeSpan value) => value < TimeSpan.Zero ? TimeSpan.Zero : value;
}
