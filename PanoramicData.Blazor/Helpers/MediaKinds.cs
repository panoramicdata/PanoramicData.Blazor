namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Kind filtering and counting shared by the marker and segment navigators of <see cref="PDMediaPlayer"/>. Kinds are
/// compared without regard to case.
/// </summary>
internal static class MediaKinds
{
	/// <summary>
	/// Keeps the items whose kind is in a set, or every item when the set is <c>null</c>.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="items">The items.</param>
	/// <param name="kindOf">Gets the kind of an item.</param>
	/// <param name="visibleKinds">The kinds to keep.</param>
	/// <returns>The kept items, in their order.</returns>
	internal static IReadOnlyList<T> Filter<T>(IReadOnlyList<T> items, Func<T, string> kindOf, IReadOnlySet<string>? visibleKinds)
	{
		ArgumentNullException.ThrowIfNull(items);
		if (visibleKinds is null)
		{
			return items;
		}

		var kinds = new HashSet<string>(visibleKinds, MediaMarkerNavigator.KindComparer);
		return [.. items.Where(i => kinds.Contains(kindOf(i)))];
	}

	/// <summary>
	/// Keeps the items of one kind; surrounding white space is ignored and a null kind means items without a kind.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="items">The items.</param>
	/// <param name="kindOf">Gets the kind of an item.</param>
	/// <param name="kind">The kind to keep.</param>
	/// <returns>The kept items, in their order.</returns>
	internal static IReadOnlyList<T> Filter<T>(IReadOnlyList<T> items, Func<T, string> kindOf, string? kind)
	{
		ArgumentNullException.ThrowIfNull(items);
		var wanted = kind?.Trim() ?? string.Empty;
		return [.. items.Where(i => MediaMarkerNavigator.KindComparer.Equals(kindOf(i), wanted))];
	}

	/// <summary>
	/// Counts the items of each kind, ordered by kind.
	/// </summary>
	/// <typeparam name="T">The item type.</typeparam>
	/// <param name="items">The items.</param>
	/// <param name="kindOf">Gets the kind of an item.</param>
	/// <returns>Each kind with its number of items.</returns>
	internal static IReadOnlyList<KeyValuePair<string, int>> CountByKind<T>(IEnumerable<T> items, Func<T, string> kindOf)
	{
		ArgumentNullException.ThrowIfNull(items);
		return [.. items
			.GroupBy(kindOf, MediaMarkerNavigator.KindComparer)
			.Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
			.OrderBy(p => p.Key, MediaMarkerNavigator.KindComparer)];
	}
}
