namespace PanoramicData.Blazor.Arguments;

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
