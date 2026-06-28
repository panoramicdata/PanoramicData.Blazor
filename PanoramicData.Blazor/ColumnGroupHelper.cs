namespace PanoramicData.Blazor;

/// <summary>
/// Pure helper logic for the column grouping / facet feature, kept separate from the components so the
/// rules can be unit tested without rendering.
/// </summary>
public static class ColumnGroupHelper
{
	/// <summary>
	/// Determines whether a column belongs to the currently active group facet.
	/// </summary>
	/// <param name="columnGroup">The column's group name, or null/empty when the column is ungrouped.</param>
	/// <param name="activeGroup">The active facet, or null/empty when all groups are shown.</param>
	/// <returns>
	/// True when the column should be shown for the active facet. Ungrouped columns are always shown
	/// (pinned), and when no facet is active every column is shown.
	/// </returns>
	public static bool IsInActiveGroup(string? columnGroup, string? activeGroup)
		=> string.IsNullOrEmpty(activeGroup)
			|| string.IsNullOrEmpty(columnGroup)
			|| string.Equals(columnGroup, activeGroup, StringComparison.Ordinal);

	/// <summary>
	/// Builds the ordered list of facet pills for a table.
	/// </summary>
	/// <param name="registeredGroups">
	/// Group metadata registered by <c>PDColumnGroup</c> wrappers, in registration order.
	/// </param>
	/// <param name="listableColumnGroupNames">
	/// The group name of every listable column (null/empty for ungrouped columns), used to compute counts
	/// and to discover groups declared only via a bare Group="..." string.
	/// </param>
	/// <returns>
	/// Registered groups first (ordered by <see cref="ColumnGroupContext.Ordinal"/> then registration order),
	/// followed by any string-only groups in first-seen order. Ungrouped columns produce no pill.
	/// </returns>
	public static List<ColumnGroupPill> BuildPills(
		IEnumerable<ColumnGroupContext> registeredGroups,
		IEnumerable<string?> listableColumnGroupNames)
	{
		ArgumentNullException.ThrowIfNull(registeredGroups);
		ArgumentNullException.ThrowIfNull(listableColumnGroupNames);

		// Count columns per group and remember the order in which each group was first seen.
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		var firstSeen = new List<string>();
		foreach (var name in listableColumnGroupNames)
		{
			if (string.IsNullOrEmpty(name))
			{
				continue;
			}

			if (counts.TryGetValue(name, out var count))
			{
				counts[name] = count + 1;
			}
			else
			{
				counts[name] = 1;
				firstSeen.Add(name);
			}
		}

		// De-duplicate registered groups by name, keeping the first registration.
		var registered = registeredGroups
			.Where(g => !string.IsNullOrEmpty(g.Name))
			.GroupBy(g => g.Name, StringComparer.Ordinal)
			.Select(g => g.First())
			.ToList();

		var pills = new List<ColumnGroupPill>();
		var seen = new HashSet<string>(StringComparer.Ordinal);

		// Registered groups first, ordered by ordinal (OrderBy is stable, preserving registration order).
		foreach (var context in registered.OrderBy(g => g.Ordinal))
		{
			if (!seen.Add(context.Name))
			{
				continue;
			}

			pills.Add(new ColumnGroupPill
			{
				Name = context.Name,
				Icon = context.Icon,
				Description = context.Description,
				Count = counts.TryGetValue(context.Name, out var count) ? count : 0
			});
		}

		// Then any groups referenced only by a bare Group="..." string (no PDColumnGroup metadata).
		foreach (var name in firstSeen)
		{
			if (seen.Add(name))
			{
				pills.Add(new ColumnGroupPill { Name = name, Count = counts[name] });
			}
		}

		return pills;
	}
}
