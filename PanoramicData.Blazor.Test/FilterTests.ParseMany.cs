using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// ParseMany edge case tests for <see cref="Filter"/>.
/// </summary>
public partial class FilterTests
{
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
}
