using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the Filter class: its text form, validity, updating, construction and enum names.</summary>
public partial class FilterTests
{
	/// <summary>Verifies that ToString produces the expected filter string format for all supported filter types and quote-wraps multi-word values.</summary>
	[Theory]
	[InlineData(FilterTypes.Equals, "status", "Open", "", "status:Open")]
	[InlineData(FilterTypes.DoesNotEqual, "status", "Open", "", "status:!Open")]
	[InlineData(FilterTypes.StartsWith, "name", "Jo", "", "name:Jo*")]
	[InlineData(FilterTypes.EndsWith, "name", "son", "", "name:*son")]
	[InlineData(FilterTypes.Contains, "name", "oh", "", "name:*oh*")]
	[InlineData(FilterTypes.DoesNotContain, "name", "test", "", "name:!*test*")]
	[InlineData(FilterTypes.In, "status", "A,B", "", "status:In(A,B)")]
	[InlineData(FilterTypes.NotIn, "status", "A,B", "", "status:!In(A,B)")]
	[InlineData(FilterTypes.GreaterThan, "price", "10", "", "price:>10")]
	[InlineData(FilterTypes.GreaterThanOrEqual, "price", "10", "", "price:>=10")]
	[InlineData(FilterTypes.LessThan, "price", "50", "", "price:<50")]
	[InlineData(FilterTypes.LessThanOrEqual, "price", "50", "", "price:<=50")]
	[InlineData(FilterTypes.Range, "price", "10", "50", "price:>10|50<")]
	[InlineData(FilterTypes.IsNull, "status", "", "", "status:(null)")]
	[InlineData(FilterTypes.IsNotNull, "status", "", "", "status:!(null)")]
	[InlineData(FilterTypes.IsEmpty, "status", "", "", "status:(empty)")]
	[InlineData(FilterTypes.IsNotEmpty, "status", "", "", "status:!(empty)")]
	// multi-word values must be quoted so ParseMany's whitespace tokeniser does not split them
	[InlineData(FilterTypes.Equals, "name", "one two three", "", "name:\"one two three\"")]
	[InlineData(FilterTypes.DoesNotEqual, "name", "one two", "", "name:!\"one two\"")]
	[InlineData(FilterTypes.StartsWith, "name", "On Microsoft", "", "name:\"On Microsoft\"*")]
	[InlineData(FilterTypes.EndsWith, "name", "On Microsoft", "", "name:*\"On Microsoft\"")]
	[InlineData(FilterTypes.Contains, "name", "On Microsoft", "", "name:*\"On Microsoft\"*")]
	[InlineData(FilterTypes.DoesNotContain, "name", "On Microsoft", "", "name:!*\"On Microsoft\"*")]
	[InlineData(FilterTypes.GreaterThan, "name", "a b", "", "name:>\"a b\"")]
	[InlineData(FilterTypes.GreaterThanOrEqual, "name", "a b", "", "name:>=\"a b\"")]
	[InlineData(FilterTypes.LessThan, "name", "a b", "", "name:<\"a b\"")]
	[InlineData(FilterTypes.LessThanOrEqual, "name", "a b", "", "name:<=\"a b\"")]
	[InlineData(FilterTypes.Range, "name", "a b", "c d", "name:>\"a b\"|\"c d\"<")]
	public void ToString_AllFilterTypes_ProducesExpectedFormat(FilterTypes filterType, string key, string value, string value2, string expected)
	{
		var filter = new Filter(filterType, key, value, value2);

		filter.ToString().ShouldBe(expected);
	}

	/// <summary>Verifies that a filter serialized by ToString and deserialized by Parse preserves all filter properties for all filter types.</summary>
	[Theory]
	[InlineData(FilterTypes.Equals, "status", "Open", "")]
	[InlineData(FilterTypes.DoesNotEqual, "status", "Open", "")]
	[InlineData(FilterTypes.StartsWith, "name", "Jo", "")]
	[InlineData(FilterTypes.EndsWith, "name", "son", "")]
	[InlineData(FilterTypes.Contains, "name", "oh", "")]
	[InlineData(FilterTypes.DoesNotContain, "name", "test", "")]
	[InlineData(FilterTypes.In, "status", "A,B", "")]
	[InlineData(FilterTypes.NotIn, "status", "A,B", "")]
	[InlineData(FilterTypes.GreaterThan, "price", "10", "")]
	[InlineData(FilterTypes.GreaterThanOrEqual, "price", "10", "")]
	[InlineData(FilterTypes.LessThan, "price", "50", "")]
	[InlineData(FilterTypes.LessThanOrEqual, "price", "50", "")]
	[InlineData(FilterTypes.Range, "price", "10", "50")]
	[InlineData(FilterTypes.IsNull, "status", "", "")]
	[InlineData(FilterTypes.IsNotNull, "status", "", "")]
	[InlineData(FilterTypes.IsEmpty, "status", "", "")]
	[InlineData(FilterTypes.IsNotEmpty, "status", "", "")]
	// multi-word values: ToString() must quote them; Parse() must strip the quotes
	[InlineData(FilterTypes.Equals, "name", "one two three", "")]
	[InlineData(FilterTypes.DoesNotEqual, "name", "one two", "")]
	[InlineData(FilterTypes.StartsWith, "name", "On Microsoft", "")]
	[InlineData(FilterTypes.EndsWith, "name", "On Microsoft", "")]
	[InlineData(FilterTypes.Contains, "name", "On Microsoft Schedule", "")]
	[InlineData(FilterTypes.DoesNotContain, "name", "On Microsoft", "")]
	[InlineData(FilterTypes.GreaterThan, "name", "a b", "")]
	[InlineData(FilterTypes.GreaterThanOrEqual, "name", "a b", "")]
	[InlineData(FilterTypes.LessThan, "name", "a b", "")]
	[InlineData(FilterTypes.LessThanOrEqual, "name", "a b", "")]
	[InlineData(FilterTypes.Range, "name", "a b", "c d")]
	public void ToStringThenParse_RoundTrip_PreservesFilterProperties(FilterTypes filterType, string key, string value, string value2)
	{
		var original = new Filter(filterType, key, value, value2);

		var roundTripped = Filter.Parse(original.ToString());

		roundTripped.Key.ShouldBe(original.Key);
		roundTripped.FilterType.ShouldBe(original.FilterType);
		roundTripped.Value.ShouldBe(original.Value);
		roundTripped.Value2.ShouldBe(original.Value2);
	}

	/// <summary>Verifies that a filter with a multi-word value round-trips through ToString and ParseMany as exactly one filter for all filter types.</summary>
	[Theory]
	[InlineData(FilterTypes.Equals, "name", "one two three", "")]
	[InlineData(FilterTypes.DoesNotEqual, "name", "one two", "")]
	[InlineData(FilterTypes.StartsWith, "name", "On Microsoft", "")]
	[InlineData(FilterTypes.EndsWith, "name", "On Microsoft", "")]
	[InlineData(FilterTypes.Contains, "name", "On Microsoft Schedule", "")]
	[InlineData(FilterTypes.DoesNotContain, "name", "On Microsoft", "")]
	[InlineData(FilterTypes.GreaterThan, "name", "a b", "")]
	[InlineData(FilterTypes.GreaterThanOrEqual, "name", "a b", "")]
	[InlineData(FilterTypes.LessThan, "name", "a b", "")]
	[InlineData(FilterTypes.LessThanOrEqual, "name", "a b", "")]
	[InlineData(FilterTypes.Range, "name", "a b", "c d")]
	public void ToStringThenParseMany_MultiWordValue_RoundTripProducesExactlyOneFilter(FilterTypes filterType, string key, string value, string value2)
	{
		// Regression: before the fix, ToString() did not quote multi-word values, so ParseMany's
		// whitespace tokeniser would split them and only the first word was kept as a keyed filter.
		var original = new Filter(filterType, key, value, value2);

		var filters = Filter.ParseMany(original.ToString()).ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe(original.Key);
		filters[0].FilterType.ShouldBe(original.FilterType);
		filters[0].Value.ShouldBe(original.Value);
		filters[0].Value2.ShouldBe(original.Value2);
	}

	/// <summary>Verifies that ToString does not double-quote values that are already quoted, preventing malformed output.</summary>
	[Theory]
	[InlineData(FilterTypes.Equals, "name", "On Microsoft Schedule", "name:\"On Microsoft Schedule\"")]
	[InlineData(FilterTypes.DoesNotEqual, "name", "On Microsoft Schedule", "name:!\"On Microsoft Schedule\"")]
	[InlineData(FilterTypes.GreaterThan, "name", "a b", "name:>\"a b\"")]
	[InlineData(FilterTypes.LessThan, "name", "a b", "name:<\"a b\"")]
	[InlineData(FilterTypes.Range, "name", "a b", "c d")]
	public void ToString_WhenValueAlreadyQuoted_DoesNotDoubleQuote(FilterTypes filterType, string key, string value, string value2)
	{
		// Regression: PDFilter was storing pre-quoted values (e.g. "\"On Microsoft Schedule\"") into
		// Filter.Value, then ToString() wrapped them in quotes again, producing doubled quotes like
		// name:""On Microsoft Schedule"". Filter.Value must always hold the raw unquoted value.
		var preQuoted = value.Contains(' ') ? $"\"{value}\"" : value;
		var preQuoted2 = value2.Contains(' ') ? $"\"{value2}\"" : value2;
		var filter = new Filter(filterType, key, preQuoted, preQuoted2);

		var result = filter.ToString();

		result.ShouldNotContain("\"\"");
	}

	/// <summary>Verifies that IsValid returns the expected result for various filter type and value combinations.</summary>
	[Theory]
	[InlineData(FilterTypes.Equals, "test", "", true)]
	[InlineData(FilterTypes.Equals, "", "", false)]
	[InlineData(FilterTypes.Equals, "  ", "", false)]
	[InlineData(FilterTypes.Range, "10", "20", true)]
	[InlineData(FilterTypes.Range, "10", "", false)]
	[InlineData(FilterTypes.Range, "", "20", false)]
	[InlineData(FilterTypes.IsNull, "", "", true)]
	[InlineData(FilterTypes.IsNotNull, "", "", true)]
	[InlineData(FilterTypes.IsEmpty, "", "", true)]
	[InlineData(FilterTypes.IsNotEmpty, "", "", true)]
	public void IsValid_ReturnsExpectedResult(FilterTypes filterType, string value, string value2, bool expected)
	{
		var filter = new Filter { FilterType = filterType, Value = value, Value2 = value2 };

		filter.IsValid.ShouldBe(expected);
	}

	/// <summary>Verifies that Clear resets the FilterType to Equals and clears the Value.</summary>
	[Fact]
	public void Clear_ResetsFilterTypeAndValue()
	{
		var filter = new Filter(FilterTypes.GreaterThan, "price", "100");

		filter.Clear();

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that Clear also resets Value2, preventing stale range state.</summary>
	[Fact]
	public void Clear_ResetsValue2()
	{
		// Regression: Clear() previously left Value2 intact, which could cause stale Range state
		var filter = new Filter(FilterTypes.Range, "price", "10", "50");

		filter.Clear();

		filter.Value2.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that the constructor converts an object value to its string representation via ToString.</summary>
	[Fact]
	public void Constructor_ObjectValue_UsesToString()
	{
		var filter = new Filter(FilterTypes.Equals, "status", (object)42);

		filter.Value.ShouldBe("42");
	}

	/// <summary>Verifies that the constructor uses an empty string for a null object value.</summary>
	[Fact]
	public void Constructor_NullObjectValue_UsesEmptyString()
	{
		var filter = new Filter(FilterTypes.Equals, "status", (object)null!);

		filter.Value.ShouldBe(string.Empty);
	}
}
