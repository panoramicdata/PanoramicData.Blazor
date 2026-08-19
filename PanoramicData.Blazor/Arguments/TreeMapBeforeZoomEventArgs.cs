namespace PanoramicData.Blazor.Arguments;

/// <summary>
/// Provides data for the event raised before a tree map changes its zoom target, and allows the
/// change to be cancelled.
/// </summary>
/// <typeparam name="TItem">The type of the item in the source hierarchy.</typeparam>
/// <param name="from">The item currently zoomed into, or null when showing the whole hierarchy.</param>
/// <param name="to">The item about to be zoomed into, or null when returning to the whole hierarchy.</param>
public sealed class TreeMapBeforeZoomEventArgs<TItem>(TItem? from, TItem? to) : CancelEventArgs
	where TItem : class
{
	/// <summary>
	/// Gets the item currently zoomed into, or null when the whole hierarchy is shown.
	/// </summary>
	public TItem? From { get; } = from;

	/// <summary>
	/// Gets the item that is about to be zoomed into, or null when returning to the whole hierarchy.
	/// </summary>
	public TItem? To { get; } = to;
}
