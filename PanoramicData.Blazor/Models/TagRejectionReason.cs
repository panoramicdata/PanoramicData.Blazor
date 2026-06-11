namespace PanoramicData.Blazor.Models;

/// <summary>
/// The reason a tag was rejected by a PDTagInput.
/// </summary>
public enum TagRejectionReason
{
	/// <summary>
	/// The tag is already present (comparison respects the CaseSensitive parameter).
	/// </summary>
	Duplicate,

	/// <summary>
	/// The tag exceeds the maximum permitted length (MaxTagLength).
	/// </summary>
	TooLong,

	/// <summary>
	/// The maximum number of tags (MaxTags) has already been reached.
	/// </summary>
	MaxTagsReached,

	/// <summary>
	/// Free-text entry is disabled (AllowFreeText is false) and the tag does not match any suggestion.
	/// </summary>
	NotInSuggestions
}
