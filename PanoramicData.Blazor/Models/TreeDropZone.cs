namespace PanoramicData.Blazor.Models;

/// <summary>
/// Decides where a drop lands from the pointer's position within a tree row.
/// </summary>
internal static class TreeDropZone
{
	/// <summary>
	/// Gets the drop position for a pointer within a row: the top quarter is before, the bottom quarter is after
	/// and the middle is into. When dropping into the row is not allowed, the row splits into halves instead.
	/// </summary>
	/// <param name="offsetY">The pointer's distance from the top of the row, in pixels.</param>
	/// <param name="height">The row's height, in pixels.</param>
	/// <param name="allowInto">Whether dropping onto the row's node is allowed.</param>
	/// <returns>The drop position.</returns>
	internal static DropPosition GetPosition(double offsetY, double height, bool allowInto)
	{
		var fraction = height > 0 ? offsetY / height : 0.5;
		if (!allowInto)
		{
			return fraction < 0.5 ? DropPosition.Before : DropPosition.After;
		}

		if (fraction < 0.25)
		{
			return DropPosition.Before;
		}

		return fraction >= 0.75 ? DropPosition.After : DropPosition.Into;
	}
}
