using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Parse tests for <see cref="Filter"/> covering structural edge cases and quote stripping combined with operators.
/// </summary>
public partial class FilterTests
{
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
}
