namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies the data type of a filter field, which determines which filter operators are available.
/// </summary>
public enum FilterDataTypes
{
	/// <summary>Free-text string comparisons such as Contains, StartsWith, and Equals.</summary>
	Text,
	/// <summary>Numeric comparisons such as GreaterThan, LessThan, and Range.</summary>
	Numeric,
	/// <summary>Date and time comparisons.</summary>
	Date,
	/// <summary>Enum member selection using Equals and In-list operators.</summary>
	Enum,
	/// <summary>Boolean true/false equality.</summary>
	Bool
}
