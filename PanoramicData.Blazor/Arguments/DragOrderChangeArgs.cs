namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// Provides arguments raised when a drag-and-drop operation reorders items in a list.
/// </summary>
/// <typeparam name="TItem">The type of item that was reordered.</typeparam>
/// <param name="items">The full collection of items in the new order.</param>
/// <param name="item">The item that was moved.</param>
public class DragOrderChangeArgs<TItem>(IEnumerable<TItem> items, TItem item)
{
	/// <summary>
	/// Gets the items in the new order.
	/// </summary>
	public IEnumerable<TItem> Items { get; private set; } = items;

	/// <summary>
	/// Gets the item that was moved.
	/// </summary>
	public TItem Item { get; private set; } = item;
}
