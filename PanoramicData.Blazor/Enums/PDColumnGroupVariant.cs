namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Visual style used by <see cref="PDColumnGrouper{TItem}"/> when rendering its column group facets.
/// </summary>
public enum PDColumnGroupVariant
{
	/// <summary>A single joined control where the facets sit together as one unit.</summary>
	Segmented,

	/// <summary>Separated, rounded pills with a gap between each facet.</summary>
	Pills
}
