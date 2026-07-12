namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines the minimum contract for items that participate in drag-and-drop operations.
/// </summary>
public interface IDragItem
{
	/// <summary>Gets a value indicating whether this item may be dragged.</summary>
	bool CanDrag { get; }

	/// <summary>Gets the unique identifier of this item.</summary>
	string Id { get; }
}
