namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Where an item was dropped relative to its target.
/// </summary>
public enum DropPosition
{
	/// <summary>Onto the target itself, or in no particular order (<see cref="Arguments.DropEventArgs.Before"/> is null).</summary>
	Into,
	/// <summary>Immediately before the target, as its previous sibling.</summary>
	Before,
	/// <summary>Immediately after the target, as its next sibling (for a tree node, after its subtree).</summary>
	After
}