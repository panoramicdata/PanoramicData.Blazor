using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the Filter class: parsing every filter type and its edge cases.</summary>
public partial class FilterTests
{
	#region Parse - All Filter Types

	/// <summary>Verifies that Parse correctly identifies a DoesNotEqual filter from the ! prefix.</summary>
	[Fact]
	public void Parse_DoesNotEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!Closed");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("Closed");
	}

	/// <summary>Verifies that Parse correctly identifies a StartsWith filter from a trailing wildcard.</summary>
	[Fact]
	public void Parse_StartsWith_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:John*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.StartsWith);
		filter.Value.ShouldBe("John");
	}

	/// <summary>Verifies that Parse correctly identifies an EndsWith filter from a leading wildcard.</summary>
	[Fact]
	public void Parse_EndsWith_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:*son");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.EndsWith);
		filter.Value.ShouldBe("son");
	}

	/// <summary>Verifies that Parse correctly identifies a Contains filter from surrounding wildcards.</summary>
	[Fact]
	public void Parse_Contains_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:*oh*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.Contains);
		filter.Value.ShouldBe("oh");
	}

	/// <summary>Verifies that Parse correctly identifies a DoesNotContain filter from the !* prefix and trailing wildcard.</summary>
	[Fact]
	public void Parse_DoesNotContain_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:!*test*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.DoesNotContain);
		filter.Value.ShouldBe("test");
	}

	/// <summary>Verifies that Parse correctly identifies an In filter from the In() syntax.</summary>
	[Fact]
	public void Parse_In_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:In(A,B,C)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.In);
		filter.Value.ShouldBe("A,B,C");
	}

	/// <summary>Verifies that Parse correctly identifies a NotIn filter from the !In() syntax.</summary>
	[Fact]
	public void Parse_NotIn_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!In(A,B)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.NotIn);
		filter.Value.ShouldBe("A,B");
	}

	/// <summary>Verifies that Parse preserves quotes around multi-word items within an In() filter.</summary>
	[Fact]
	public void Parse_InWithQuotedMultiWordItems_PreservesQuotes()
	{
		var filter = Filter.Parse("name:In(\"Chain Test I\"|\"A - Test Schedule\")");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.In);
		filter.Value.ShouldBe("\"Chain Test I\"|\"A - Test Schedule\"");
	}

	/// <summary>Verifies that Parse preserves quotes around multi-word items within a !In() filter.</summary>
	[Fact]
	public void Parse_NotInWithQuotedMultiWordItems_PreservesQuotes()
	{
		var filter = Filter.Parse("name:!In(\"Chain Test I\"|\"A - Test Schedule\")");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.NotIn);
		filter.Value.ShouldBe("\"Chain Test I\"|\"A - Test Schedule\"");
	}

	/// <summary>Verifies that Parse correctly identifies a GreaterThan filter from the > prefix.</summary>
	[Fact]
	public void Parse_GreaterThan_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>100");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		filter.Value.ShouldBe("100");
	}

	/// <summary>Verifies that Parse correctly identifies a GreaterThanOrEqual filter from the >= prefix.</summary>
	[Fact]
	public void Parse_GreaterThanOrEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>=100");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.GreaterThanOrEqual);
		filter.Value.ShouldBe("100");
	}

	/// <summary>Verifies that Parse correctly identifies a LessThan filter from the &lt; prefix.</summary>
	[Fact]
	public void Parse_LessThan_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:<50");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.LessThan);
		filter.Value.ShouldBe("50");
	}

	/// <summary>Verifies that Parse correctly identifies a LessThanOrEqual filter from the &lt;= prefix.</summary>
	[Fact]
	public void Parse_LessThanOrEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:<=50");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.LessThanOrEqual);
		filter.Value.ShouldBe("50");
	}

	/// <summary>Verifies that Parse correctly identifies a Range filter, populating both Value and Value2.</summary>
	[Fact]
	public void Parse_Range_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>10|100<");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.Range);
		filter.Value.ShouldBe("10");
		filter.Value2.ShouldBe("100");
	}

	/// <summary>Verifies that Parse correctly identifies an IsNull filter from the (null) syntax.</summary>
	[Fact]
	public void Parse_IsNull_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:(null)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsNull);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that Parse correctly identifies an IsNotNull filter from the !(null) syntax.</summary>
	[Fact]
	public void Parse_IsNotNull_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!(null)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsNotNull);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that Parse correctly identifies an IsEmpty filter from the (empty) syntax.</summary>
	[Fact]
	public void Parse_IsEmpty_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:(empty)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsEmpty);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that Parse correctly identifies an IsNotEmpty filter from the !(empty) syntax.</summary>
	[Fact]
	public void Parse_IsNotEmpty_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!(empty)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsNotEmpty);
		filter.Value.ShouldBe(string.Empty);
	}

	#endregion

	#region Parse - Structural Edge Cases

	/// <summary>Verifies that Parse returns an empty filter when there is no colon separator in the input.</summary>
	[Fact]
	public void Parse_NoColon_ReturnsEmptyFilter()
	{
		var filter = Filter.Parse("noColonHere");

		filter.Key.ShouldBe(string.Empty);
		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that Parse splits on only the first colon, preserving additional colons as part of the value.</summary>
	[Fact]
	public void Parse_ValueContainingColon_SplitsOnFirstColon()
	{
		var filter = Filter.Parse("url:https://example.com");

		filter.Key.ShouldBe("url");
		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("https://example.com");
	}

	/// <summary>Verifies that Parse sets PropertyName from a key mappings dictionary when the key is found.</summary>
	[Fact]
	public void Parse_WithKeyMappings_SetsPropertyName()
	{
		var mappings = new Dictionary<string, string> { ["s"] = "Status" };

		var filter = Filter.Parse("s:Open", mappings);

		filter.Key.ShouldBe("s");
		filter.PropertyName.ShouldBe("Status");
		filter.Value.ShouldBe("Open");
	}

	/// <summary>Verifies that Parse correctly identifies an In filter from a lowercase "in" keyword.</summary>
	[Fact]
	public void Parse_InCaseInsensitive_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:in(A,B)");

		filter.FilterType.ShouldBe(FilterTypes.In);
		filter.Value.ShouldBe("A,B");
	}

	#endregion

	#region Parse - Quote Stripping Combined with Operators

	/// <summary>Verifies that Parse strips quotes from the value of a DoesNotEqual filter.</summary>
	[Fact]
	public void Parse_DoesNotEqualWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("status:!\"Ready for Test\"");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("Ready for Test");
	}

	/// <summary>Verifies that Parse strips quotes from the value of a GreaterThanOrEqual filter.</summary>
	[Fact]
	public void Parse_GreaterThanOrEqualWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("price:>=\"10.5\"");

		filter.FilterType.ShouldBe(FilterTypes.GreaterThanOrEqual);
		filter.Value.ShouldBe("10.5");
	}

	/// <summary>Verifies that Parse strips quotes from the value of a LessThan filter.</summary>
	[Fact]
	public void Parse_LessThanWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("price:<\"99\"");

		filter.FilterType.ShouldBe(FilterTypes.LessThan);
		filter.Value.ShouldBe("99");
	}

	/// <summary>Verifies that Parse strips quotes from the value of a Contains filter.</summary>
	[Fact]
	public void Parse_ContainsWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("name:*\"test value\"*");

		filter.FilterType.ShouldBe(FilterTypes.Contains);
		filter.Value.ShouldBe("test value");
	}

	/// <summary>Verifies that Parse strips quotes from the value of a StartsWith filter.</summary>
	[Fact]
	public void Parse_StartsWithWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("name:\"test value\"*");

		filter.FilterType.ShouldBe(FilterTypes.StartsWith);
		filter.Value.ShouldBe("test value");
	}

	/// <summary>Verifies that Parse strips quotes from the value of an EndsWith filter.</summary>
	[Fact]
	public void Parse_EndsWithWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("name:*\"test value\"");

		filter.FilterType.ShouldBe(FilterTypes.EndsWith);
		filter.Value.ShouldBe("test value");
	}

	/// <summary>Verifies that Parse strips quotes from the value of a DoesNotContain filter.</summary>
	[Fact]
	public void Parse_DoesNotContainWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("name:!*\"test value\"*");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotContain);
		filter.Value.ShouldBe("test value");
	}

	/// <summary>Verifies that Parse strips quotes from the value of a LessThanOrEqual filter.</summary>
	[Fact]
	public void Parse_LessThanOrEqualWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("price:<=\"50\"");

		filter.FilterType.ShouldBe(FilterTypes.LessThanOrEqual);
		filter.Value.ShouldBe("50");
	}

	/// <summary>Verifies that Parse strips quotes from the value of a GreaterThan filter.</summary>
	[Fact]
	public void Parse_GreaterThanWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("price:>\"10\"");

		filter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		filter.Value.ShouldBe("10");
	}

	#endregion

	#region ParseMany - Edge Cases

	/// <summary>Verifies that ParseMany yields no filters for an empty string.</summary>
	[Fact]
	public void ParseMany_EmptyString_YieldsNoFilters()
	{
		var filters = Filter.ParseMany("").ToList();

		filters.Count.ShouldBe(0);
	}

	/// <summary>Verifies that ParseMany yields no filters for a null string.</summary>
	[Fact]
	public void ParseMany_NullString_YieldsNoFilters()
	{
		var filters = Filter.ParseMany(null!).ToList();

		filters.Count.ShouldBe(0);
	}

	/// <summary>Verifies that ParseMany yields no filters for a whitespace-only string.</summary>
	[Fact]
	public void ParseMany_WhitespaceOnly_YieldsNoFilters()
	{
		var filters = Filter.ParseMany("   ").ToList();

		filters.Count.ShouldBe(0);
	}

	/// <summary>Verifies that ParseMany yields no filters when the input contains no colon separator.</summary>
	[Fact]
	public void ParseMany_NoColon_YieldsNoFilters()
	{
		var filters = Filter.ParseMany("noColonAnywhere").ToList();

		filters.Count.ShouldBe(0);
	}

	/// <summary>Verifies that ParseMany preserves the entire hash-delimited value including internal spaces.</summary>
	[Fact]
	public void ParseMany_HashDelimitedValue_PreservesSpaces()
	{
		var filters = Filter.ParseMany("name:#value with spaces#").ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("name");
		filters[0].Value.ShouldBe("#value with spaces#");
	}

	/// <summary>Verifies that ParseMany auto-closes an unterminated quoted value and strips the balanced pair.</summary>
	[Fact]
	public void ParseMany_UnterminatedQuote_AutoClosesAndParses()
	{
		var filters = Filter.ParseMany("name:\"unterminated").ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("name");
		// ParseMany auto-appends closing quote, then Parse strips the balanced pair
		filters[0].Value.ShouldBe("unterminated");
	}

	/// <summary>Verifies that ParseMany sets PropertyName from a key mappings dictionary for multiple filters.</summary>
	[Fact]
	public void ParseMany_WithKeyMappings_SetsPropertyNames()
	{
		var mappings = new Dictionary<string, string>
		{
			["s"] = "Status",
			["p"] = "Price"
		};

		var filters = Filter.ParseMany("s:Open p:>10", mappings).ToList();

		filters.Count.ShouldBe(2);
		filters[0].Key.ShouldBe("s");
		filters[0].PropertyName.ShouldBe("Status");
		filters[1].Key.ShouldBe("p");
		filters[1].PropertyName.ShouldBe("Price");
	}

	/// <summary>Verifies that ParseMany preserves a full navigation property path mapping as the PropertyName.</summary>
	[Fact]
	public void ParseMany_WithNavigationPropertyPathMapping_SetsEntityPathAsPropertyName()
	{
		// Regression guard: when a data provider explicitly maps a FilterKey to a navigation
		// property path (e.g. "Tenant.Name"), ParseMany must preserve the full dotted path
		// as PropertyName, NOT collapse it to the ViewModel property name ("TenantName").
		// The PDTable component derives "TenantName" from the column Field expression and
		// must not overwrite an explicit mapping that was registered by the data provider.
		var mappings = new Dictionary<string, string> { ["tenant-name"] = "Tenant.Name" };

		var filters = Filter.ParseMany("tenant-name:*Panoramic*", mappings).ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("tenant-name");
		filters[0].PropertyName.ShouldBe("Tenant.Name");
		filters[0].FilterType.ShouldBe(FilterTypes.Contains);
		filters[0].Value.ShouldBe("Panoramic");
	}

	/// <summary>Verifies that ParseMany auto-closes an unterminated hash-delimited value.</summary>
	[Fact]
	public void ParseMany_UnterminatedHash_AutoClosesAndParses()
	{
		var filters = Filter.ParseMany("name:#unterminated").ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("name");
		filters[0].Value.ShouldBe("#unterminated#");
	}

	/// <summary>Verifies that ParseMany treats an In() expression as a single filter token without splitting on commas.</summary>
	[Fact]
	public void ParseMany_InFilterWithMultipleItems_ParsedAsOneFilter()
	{
		// The tokeniser must not split In(...) even though it contains a comma and no spaces
		var filters = Filter.ParseMany("status:In(A,B,C)").ToList();

		filters.Count.ShouldBe(1);
		filters[0].FilterType.ShouldBe(FilterTypes.In);
		filters[0].Value.ShouldBe("A,B,C");
	}

	/// <summary>Verifies that ParseMany treats an In() expression with quoted multi-word items as a single filter token.</summary>
	[Fact]
	public void ParseMany_InFilterWithQuotedMultiWordItems_ParsedAsOneFilter()
	{
		// Each pipe-delimited item may be individually quoted; the tokeniser must not split on the
		// spaces inside the outer In(...) token because the quotes are tracked
		var filters = Filter.ParseMany("name:In(\"On Microsoft Schedule\"|\"Chain Test I\")").ToList();

		filters.Count.ShouldBe(1);
		filters[0].FilterType.ShouldBe(FilterTypes.In);
		filters[0].Value.ShouldBe("\"On Microsoft Schedule\"|\"Chain Test I\"");
	}

	/// <summary>Verifies that ParseMany treats a !In() expression with quoted multi-word items as a single filter token.</summary>
	[Fact]
	public void ParseMany_NotInFilterWithQuotedMultiWordItems_ParsedAsOneFilter()
	{
		var filters = Filter.ParseMany("name:!In(\"On Microsoft Schedule\"|\"Chain Test I\")").ToList();

		filters.Count.ShouldBe(1);
		filters[0].FilterType.ShouldBe(FilterTypes.NotIn);
		filters[0].Value.ShouldBe("\"On Microsoft Schedule\"|\"Chain Test I\"");
	}

	/// <summary>Verifies that an In filter with multi-word values round-trips through ToString and ParseMany as exactly one filter.</summary>
	[Fact]
	public void ToStringThenParseMany_InWithMultiWordValues_RoundTripProducesExactlyOneFilter()
	{
		var original = new Filter(FilterTypes.In, "name", "\"On Microsoft Schedule\"|\"Chain Test I\"");

		var filters = Filter.ParseMany(original.ToString()).ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("name");
		filters[0].FilterType.ShouldBe(FilterTypes.In);
		filters[0].Value.ShouldBe("\"On Microsoft Schedule\"|\"Chain Test I\"");
	}

	/// <summary>Verifies that a NotIn filter with multi-word values round-trips through ToString and ParseMany as exactly one filter.</summary>
	[Fact]
	public void ToStringThenParseMany_NotInWithMultiWordValues_RoundTripProducesExactlyOneFilter()
	{
		var original = new Filter(FilterTypes.NotIn, "name", "\"On Microsoft Schedule\"|\"Chain Test I\"");

		var filters = Filter.ParseMany(original.ToString()).ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("name");
		filters[0].FilterType.ShouldBe(FilterTypes.NotIn);
		filters[0].Value.ShouldBe("\"On Microsoft Schedule\"|\"Chain Test I\"");
	}

	/// <summary>Verifies that whitespace after the last token ends it without adding an empty filter.</summary>
	[Fact]
	public void ParseMany_TrailingWhitespace_IsIgnored()
	{
		var filters = Filter.ParseMany("Name:abc   ").ToList();

		filters.Count.ShouldBe(1);
		filters[0].Value.ShouldBe("abc");
	}

	#endregion
}
