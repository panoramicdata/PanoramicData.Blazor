namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Determines how a <c>PDTreeMap</c> chooses the fill colour for each rectangle.
/// </summary>
public enum TreeMapColourMode
{
	/// <summary>
	/// Colours are supplied by the caller via the ColourSelector parameter.
	/// </summary>
	Custom,

	/// <summary>
	/// Colours are taken from a built-in categorical palette, keyed off the CategorySelector parameter.
	/// </summary>
	Category,

	/// <summary>
	/// Colours are progressive shades of a single hue, determined by the depth of the node.
	/// </summary>
	Depth,

	/// <summary>
	/// Colours are taken from a sequential scale driven by the HeatSelector parameter, independent of node size.
	/// </summary>
	Heat
}
