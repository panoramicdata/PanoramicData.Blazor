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

		var (counts, firstSeen) = CountGroups(listableColumnGroupNames);

		// Registered groups first, de-duplicated by name (keeping the first registration) and ordered by
		// ordinal (OrderBy is stable, preserving registration order).
		var pills = registeredGroups
			.Where(g => !string.IsNullOrEmpty(g.Name))
			.GroupBy(g => g.Name, StringComparer.Ordinal)
			.Select(g => g.First())
			.OrderBy(g => g.Ordinal)
			.Select(context => new ColumnGroupPill
			{
				Name = context.Name,
				Icon = context.Icon,
				Description = context.Description,
				Count = counts.GetValueOrDefault(context.Name)
			})
			.ToList();

		// Then any groups referenced only by a bare Group="..." string (no PDColumnGroup metadata).
		var seen = new HashSet<string>(pills.Select(p => p.Name), StringComparer.Ordinal);
		foreach (var name in firstSeen.Where(seen.Add))
		{
			pills.Add(new ColumnGroupPill { Name = name, Count = counts[name] });
		}

		return pills;
	}

	/// <summary>
	/// Counts the columns in each group and records the order in which the groups were first seen.
	/// </summary>
	/// <param name="columnGroupNames">The group name of every column; null/empty names are ignored.</param>
	/// <returns>The number of columns in each group, and the distinct group names in first-seen order.</returns>
	private static (Dictionary<string, int> Counts, List<string> FirstSeen) CountGroups(IEnumerable<string?> columnGroupNames)
	{
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		var firstSeen = new List<string>();
		foreach (var name in columnGroupNames)
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

		return (counts, firstSeen);
	}
}
