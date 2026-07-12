namespace PanoramicData.Blazor.Models;

/// <summary>
/// Specifies the comparison operator used when evaluating a filter predicate.
/// </summary>
public enum FilterTypes
{
	/// <summary>Matches items whose field value equals the filter value.</summary>
	[Display(Name = "Equals")]
	Equals,
	/// <summary>Matches items whose field value does not equal the filter value.</summary>
	[Display(Name = "Does not equal")]
	DoesNotEqual,
	/// <summary>Matches items whose field value starts with the filter value.</summary>
	[Display(Name = "Starts with")]
	StartsWith,
	/// <summary>Matches items whose field value ends with the filter value.</summary>
	[Display(Name = "Ends with")]
	EndsWith,
	/// <summary>Matches items whose field value contains the filter value.</summary>
	[Display(Name = "Contains")]
	Contains,
	/// <summary>Matches items whose field value does not contain the filter value.</summary>
	[Display(Name = "Does not contain")]
	DoesNotContain,
	/// <summary>Matches items whose field value is one of a pipe-separated list of values.</summary>
	[Display(Name = "In list")]
	In,
	/// <summary>Matches items whose field value is greater than the filter value.</summary>
	[Display(Name = "Greater than")]
	GreaterThan,
	/// <summary>Matches items whose field value is greater than or equal to the filter value.</summary>
	[Display(Name = "Greater than or equal")]
	GreaterThanOrEqual,
	/// <summary>Matches items whose field value is less than the filter value.</summary>
	[Display(Name = "Less than")]
	LessThan,
	/// <summary>Matches items whose field value is less than or equal to the filter value.</summary>
	[Display(Name = "Less than or equal")]
	LessThanOrEqual,
	/// <summary>Matches items whose field value falls within an inclusive range defined by two filter values.</summary>
	[Display(Name = "Range")]
	Range,
	/// <summary>Matches items whose field value is null.</summary>
	[Display(Name = "Is Null")]
	IsNull,
	/// <summary>Matches items whose field value is not null.</summary>
	[Display(Name = "Is Not Null")]
	IsNotNull,
	/// <summary>Matches items whose string field value is an empty string.</summary>
	[Display(Name = "Is Empty")]
	IsEmpty,
	/// <summary>Matches items whose string field value is not empty.</summary>
	[Display(Name = "Is Not Empty")]
	IsNotEmpty,
	/// <summary>Matches items whose field value is not in a pipe-separated list of values.</summary>
	[Display(Name = "Not In list")]
	NotIn
}
