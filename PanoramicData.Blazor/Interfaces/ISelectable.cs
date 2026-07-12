namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Defines the minimum contract for items that support selection and enabled states within data-display components.
/// </summary>
public interface ISelectable
{
	/// <summary>Gets or sets a value indicating whether this item is currently selected.</summary>
	bool IsSelected { set; get; }

	/// <summary>Gets a value indicating whether this item can be interacted with.</summary>
	bool IsEnabled { get; }
}
