namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines the minimum contract for items that can be displayed in list or selection controls such as combo boxes.
/// </summary>
public interface IDisplayItem
{
	/// <summary>Gets the CSS class string for the icon to display alongside the item, or an empty string when no icon is required.</summary>
	string IconCssClass { get; }

	/// <summary>Gets the unique identifier of this item.</summary>
	string Id { get; }

	/// <summary>Gets the display text for this item.</summary>
	string Text { get; }
}
