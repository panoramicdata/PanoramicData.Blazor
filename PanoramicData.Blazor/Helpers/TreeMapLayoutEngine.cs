namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Computes an area-proportional tree map layout using the squarified algorithm described by
/// Bruls, Huizing and van Wijk (2000).
/// </summary>
/// <remarks>
/// This class deliberately contains no Blazor, JavaScript interop or DOM types. It maps a weighted
/// tree onto a rectangle and returns positioned rectangles, which makes the geometry - the part most
/// likely to be wrong - testable without a renderer.
///
/// Squarification is preferred over the simpler slice and dice approach because it keeps rectangles
/// close to square. Slice and dice degenerates into long thin slivers that are neither readable nor
/// clickable once a hierarchy has any breadth to it.
/// </remarks>
public static class TreeMapLayoutEngine
{
	/// <summary>
	/// Lays out a hierarchy within the given area.
	/// </summary>
	/// <typeparam name="TItem">The type of the item in the source hierarchy.</typeparam>
	/// <param name="root">The root of the hierarchy. The root itself is not emitted; its children are laid out across the whole area.</param>
	/// <param name="childrenSelector">Returns the children of an item, or null or empty for a leaf.</param>
	/// <param name="sizeSelector">Returns the size of an item. Negative, NaN and infinite values are treated as zero.</param>
	/// <param name="width">The width of the layout area.</param>
	/// <param name="height">The height of the layout area.</param>
	/// <param name="maxDepth">The maximum number of levels to emit below the root. Items at the cut are emitted as aggregated rectangles that still account for their whole subtree.</param>
	/// <param name="nestedPadding">The inset applied to a branch rectangle before laying out its children, leaving room for a border.</param>
	/// <param name="headerHeight">Additional space reserved at the top of a branch rectangle for its own label, so that a parent label cannot collide with its children. Applied only where the rectangle is tall enough to spare it.</param>
	/// <param name="sizeMode">Determines whether a branch node's size includes its descendants.</param>
	/// <returns>The positioned rectangles, ordered parents before children so that later rectangles paint over earlier ones.</returns>
	public static IReadOnlyList<TreeMapRect<TItem>> Layout<TItem>(
		TItem? root,
		Func<TItem, IEnumerable<TItem>?>? childrenSelector,
		Func<TItem, double> sizeSelector,
		double width,
		double height,
		int maxDepth = 3,
		double nestedPadding = 0,
		double headerHeight = 0,
		TreeMapSizeMode sizeMode = TreeMapSizeMode.Aggregate) where TItem : class
	{
		ArgumentNullException.ThrowIfNull(sizeSelector);

		var results = new List<TreeMapRect<TItem>>();

		if (root is null || !IsUsable(width) || !IsUsable(height) || maxDepth < 1)
		{
			return results;
		}

		// Size the whole tree first. Sizes below the render cut still matter, because an aggregated
		// rectangle at the cut must account for everything beneath it.
		var sized = BuildSizedTree(root, childrenSelector, sizeSelector, sizeMode, []);
		if (sized is null || sized.Children.Count == 0)
		{
			return results;
		}

		LayoutChildren(sized, new Bounds(0, 0, width, height), 0, maxDepth, nestedPadding, headerHeight, results);

		return results;
	}

	private static void LayoutChildren<TItem>(
		SizedNode<TItem> parent,
		Bounds bounds,
		int depth,
		int maxDepth,
		double nestedPadding,
		double headerHeight,
		List<TreeMapRect<TItem>> results) where TItem : class
	{
		var children = parent.Children
			.Where(c => c.Size > 0)
			.OrderByDescending(c => c.Size)
			.ThenBy(c => c.Ordinal)
			.ToList();

		if (children.Count == 0 || !IsUsable(bounds.Width) || !IsUsable(bounds.Height))
		{
			return;
		}

		var totalSize = children.Sum(c => c.Size);
		if (totalSize <= 0)
		{
			return;
		}

		// Convert sizes into areas that exactly fill the available bounds.
		var scale = bounds.Width * bounds.Height / totalSize;
		var areas = children.Select(c => c.Size * scale).ToList();

		var placements = Squarify(areas, bounds);

		for (var i = 0; i < children.Count; i++)
		{
			var child = children[i];
			var rect = placements[i];
			var atCut = depth + 1 >= maxDepth;
			var hasChildren = child.Children.Count > 0;

			results.Add(new TreeMapRect<TItem>
			{
				Item = child.Item,
				X = rect.X,
				Y = rect.Y,
				Width = rect.Width,
				Height = rect.Height,
				Depth = depth,
				Size = child.Size,
				HasChildren = hasChildren,
				IsAggregated = atCut && hasChildren
			});

			if (hasChildren && !atCut)
			{
				var inner = rect.Deflate(nestedPadding, headerHeight);
				if (IsUsable(inner.Width) && IsUsable(inner.Height))
				{
					LayoutChildren(child, inner, depth + 1, maxDepth, nestedPadding, headerHeight, results);
				}
			}
		}
	}

	/// <summary>
	/// Places a list of areas within the given bounds, growing each row only while doing so improves
	/// the worst aspect ratio in that row.
	/// </summary>
	private static List<Bounds> Squarify(List<double> areas, Bounds bounds)
	{
		var placements = new Bounds[areas.Count];
		var remaining = bounds;
		var index = 0;

		while (index < areas.Count)
		{
			var shortSide = Math.Min(remaining.Width, remaining.Height);
			if (!IsUsable(shortSide))
			{
				// Nothing usable is left; collapse the remainder onto the edge rather than emitting
				// invalid geometry.
				for (var i = index; i < areas.Count; i++)
				{
					placements[i] = new Bounds(remaining.X, remaining.Y, 0, 0);
				}

				break;
			}

			var rowEnd = index;
			var rowSum = areas[index];
			var bestWorst = Worst(areas[index], areas[index], rowSum, shortSide);

			while (rowEnd + 1 < areas.Count)
			{
				var candidateSum = rowSum + areas[rowEnd + 1];

				// Areas are sorted descending, so the first entry is the largest of the row and the
				// candidate is the smallest.
				var candidateWorst = Worst(areas[index], areas[rowEnd + 1], candidateSum, shortSide);
				if (candidateWorst > bestWorst)
				{
					break;
				}

				bestWorst = candidateWorst;
				rowSum = candidateSum;
				rowEnd++;
			}

			remaining = PlaceRow(areas, index, rowEnd, rowSum, remaining, placements);
			index = rowEnd + 1;
		}

		return [.. placements];
	}

	/// <summary>
	/// Lays a row of areas along the shorter side of the remaining bounds and returns what is left.
	/// </summary>
	private static Bounds PlaceRow(
		List<double> areas,
		int start,
		int end,
		double rowSum,
		Bounds remaining,
		Bounds[] placements)
	{
		var horizontal = remaining.Width <= remaining.Height;

		if (horizontal)
		{
			// The row is a strip across the top, its depth determined by how much area it must hold.
			var rowHeight = rowSum / remaining.Width;
			var x = remaining.X;

			for (var i = start; i <= end; i++)
			{
				var itemWidth = rowHeight > 0 ? areas[i] / rowHeight : 0;
				placements[i] = new Bounds(x, remaining.Y, itemWidth, rowHeight);
				x += itemWidth;
			}

			return new Bounds(
				remaining.X,
				remaining.Y + rowHeight,
				remaining.Width,
				Math.Max(0, remaining.Height - rowHeight));
		}

		// The row is a strip down the left-hand side.
		var rowWidth = rowSum / remaining.Height;
		var y = remaining.Y;

		for (var i = start; i <= end; i++)
		{
			var itemHeight = rowWidth > 0 ? areas[i] / rowWidth : 0;
			placements[i] = new Bounds(remaining.X, y, rowWidth, itemHeight);
			y += itemHeight;
		}

		return new Bounds(
			remaining.X + rowWidth,
			remaining.Y,
			Math.Max(0, remaining.Width - rowWidth),
			remaining.Height);
	}

	/// <summary>
	/// Returns the worst (highest) aspect ratio that would result from a row holding the given
	/// largest and smallest areas, summing to <paramref name="rowSum"/>, laid along a side of
	/// length <paramref name="shortSide"/>.
	/// </summary>
	private static double Worst(double rowMax, double rowMin, double rowSum, double shortSide)
	{
		if (rowSum <= 0 || rowMin <= 0 || shortSide <= 0)
		{
			return double.MaxValue;
		}

		var sideSquared = shortSide * shortSide;
		var sumSquared = rowSum * rowSum;

		return Math.Max(
			sideSquared * rowMax / sumSquared,
			sumSquared / (sideSquared * rowMin));
	}

	private static SizedNode<TItem>? BuildSizedTree<TItem>(
		TItem item,
		Func<TItem, IEnumerable<TItem>?>? childrenSelector,
		Func<TItem, double> sizeSelector,
		TreeMapSizeMode sizeMode,
		HashSet<TItem> ancestry) where TItem : class
	{
		// Guard against a cyclic graph being presented as a tree; without this a symlink loop in a
		// file system would recurse until the stack gave out.
		if (!ancestry.Add(item))
		{
			return null;
		}

		try
		{
			var ownSize = Sanitise(sizeSelector(item));
			var children = new List<SizedNode<TItem>>();
			var ordinal = 0;

			var rawChildren = childrenSelector?.Invoke(item);
			if (rawChildren is not null)
			{
				foreach (var rawChild in rawChildren)
				{
					if (rawChild is null)
					{
						continue;
					}

					var child = BuildSizedTree(rawChild, childrenSelector, sizeSelector, sizeMode, ancestry);
					if (child is not null)
					{
						child.Ordinal = ordinal++;
						children.Add(child);
					}
				}
			}

			var size = sizeMode == TreeMapSizeMode.Aggregate
				? ownSize + children.Sum(c => c.Size)
				: ownSize;

			return new SizedNode<TItem>(item, size, children);
		}
		finally
		{
			ancestry.Remove(item);
		}
	}

	private static double Sanitise(double value)
		=> double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? 0 : value;

	private static bool IsUsable(double value)
		=> !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;

	private sealed class SizedNode<TItem>(TItem item, double size, List<SizedNode<TItem>> children) where TItem : class
	{
		public TItem Item { get; } = item;

		public double Size { get; } = size;

		public List<SizedNode<TItem>> Children { get; } = children;

		public int Ordinal { get; set; }
	}

	private readonly record struct Bounds(double X, double Y, double Width, double Height)
	{
		/// <summary>
		/// Insets the bounds, optionally reserving extra space at the top for a label. The header is
		/// dropped rather than applied when the rectangle is too short to spare it, so that a small
		/// branch still shows its children instead of collapsing to nothing.
		/// </summary>
		public Bounds Deflate(double padding, double headerHeight)
		{
			var pad = Math.Max(0, padding);
			var header = Math.Max(0, headerHeight);

			// Only reserve the header when doing so leaves a usable area behind.
			if (header > 0 && Height - (pad * 2) - header < Math.Max(header, 8))
			{
				header = 0;
			}

			var newWidth = Width - (pad * 2);
			var newHeight = Height - (pad * 2) - header;

			return newWidth <= 0 || newHeight <= 0
				? new Bounds(X, Y, 0, 0)
				: new Bounds(X + pad, Y + pad + header, newWidth, newHeight);
		}
	}
}
