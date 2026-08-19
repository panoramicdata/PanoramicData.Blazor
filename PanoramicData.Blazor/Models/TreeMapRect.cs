namespace PanoramicData.Blazor.Models;

/// <summary>
/// A single positioned rectangle produced by the tree map layout, describing where one item
/// of the source hierarchy should be drawn.
/// </summary>
/// <typeparam name="TItem">The type of the item in the source hierarchy.</typeparam>
public sealed class TreeMapRect<TItem> where TItem : class
{
	/// <summary>
	/// Gets the item from the source hierarchy that this rectangle represents.
	/// </summary>
	public required TItem Item { get; init; }

	/// <summary>
	/// Gets the distance from the left edge of the layout area to the left edge of this rectangle.
	/// </summary>
	public required double X { get; init; }

	/// <summary>
	/// Gets the distance from the top edge of the layout area to the top edge of this rectangle.
	/// </summary>
	public required double Y { get; init; }

	/// <summary>
	/// Gets the width of this rectangle.
	/// </summary>
	public required double Width { get; init; }

	/// <summary>
	/// Gets the height of this rectangle.
	/// </summary>
	public required double Height { get; init; }

	/// <summary>
	/// Gets the depth of this rectangle below the layout root, where the root's immediate children are at depth zero.
	/// </summary>
	public required int Depth { get; init; }

	/// <summary>
	/// Gets the effective size of this item, as used to determine its area.
	/// </summary>
	public required double Size { get; init; }

	/// <summary>
	/// Gets a value indicating whether the source item has children.
	/// </summary>
	public required bool HasChildren { get; init; }

	/// <summary>
	/// Gets a value indicating whether this rectangle stands in for a subtree that was not drawn
	/// because the maximum render depth was reached. Its <see cref="Size"/> still accounts for the
	/// whole subtree, so displayed totals reconcile with the true total.
	/// </summary>
	public required bool IsAggregated { get; init; }

	/// <summary>
	/// Gets the area of this rectangle.
	/// </summary>
	public double Area => Width * Height;
}
