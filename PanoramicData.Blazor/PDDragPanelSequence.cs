namespace PanoramicData.Blazor;

/// <summary>
/// Numbers the default ids of <see cref="PDDragPanel{TItem}"/> instances. It lives outside the generic panel so that
/// panels of every item type share one sequence, and so never generate the same id.
/// </summary>
internal static class PDDragPanelSequence
{
	private static int _sequence;

	/// <summary>
	/// Gets the next number in the sequence.
	/// </summary>
	/// <returns>A number not returned before.</returns>
	internal static int Next() => Interlocked.Increment(ref _sequence);
}
