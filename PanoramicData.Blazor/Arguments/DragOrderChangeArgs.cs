namespace PanoramicData.Blazor.Arguments;

public class DragOrderChangeArgs<TItem>
{
	public DragOrderChangeArgs(IEnumerable<TItem> items, TItem item)
	{
		Items = items;
		Item = item;
	}

	/// <summary>
	/// Gets the items in the new order.
	/// </summary>
	public IEnumerable<TItem> Items { get; private set; } = [];

	/// <summary>
	/// Gets the item that was moved.
	/// </summary>
	public TItem Item { get; private set; }
}
