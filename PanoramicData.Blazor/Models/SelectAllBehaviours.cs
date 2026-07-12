namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies the effect of activating the "select all" control in a multi-selection component.
/// </summary>
public enum SelectionBehaviours
{
	/// <summary>Selecting all selects every available item.</summary>
	SelectAll,
	/// <summary>Selecting all clears the current selection.</summary>
	ClearAll
}
