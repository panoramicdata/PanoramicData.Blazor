using PanoramicData.Blazor.Models;
using Shouldly;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the Filter class: parsing values as written, formatting and date recognition.</summary>
public partial class FilterTests
{
	/// <summary>Verifies that ParseMany preserves a datetime string with timezone offset as the original value.</summary>
	[Fact]
	public void ParseMany_DateTimeWithTimeZone_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:>\"15/08/2023 21:26:07 +01:00\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("15/08/2023 21:26:07 +01:00");
	}

	/// <summary>Verifies that ParseMany preserves a datetime string without timezone offset as the original value.</summary>
	[Fact]
	public void ParseMany_DateTimeWithoutTimeZone_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:>\"15/08/2023 21:26:07.000\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("15/08/2023 21:26:07.000");
	}

	/// <summary>Verifies that ParseMany preserves a date-only string as the original value.</summary>
	[Fact]
	public void ParseMany_DateOnly_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:\"15/08/2023\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("15/08/2023");
	}

	/// <summary>Verifies that ParseMany preserves a date string in an alternative format as the original value.</summary>
	[Fact]
	public void ParseMany_DateOnlyAlternativeFormat_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:\"15-08-2023\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("15-08-2023");
	}

	/// <summary>Verifies that ParseMany preserves a date and time string as the original value.</summary>
	[Fact]
	public void ParseMany_DateAndTime_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:\"15/08/2023 21:00:00\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("15/08/2023 21:00:00");
	}

	/// <summary>Verifies that ParseMany preserves a double value string and that it is parseable as a double.</summary>
	[Fact]
	public void ParseMany_Double_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("price:>\"2.4\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("price");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("2.4");
		double.TryParse(firstFilter.Value, out double _).ShouldBeTrue();
	}

	/// <summary>Verifies that ParseMany preserves an integer value string and that it is parseable as an integer.</summary>
	[Fact]
	public void ParseMany_Integer_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("count:>\"2\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("count");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("2");
		int.TryParse(firstFilter.Value, out int _).ShouldBeTrue();
	}

	/// <summary>Verifies that ParseMany preserves a string value as the original value.</summary>
	[Fact]
	public void ParseMany_String_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("name:\"A string\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("name");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("A string");
	}

	/// <summary>Verifies that ParseMany preserves a boolean value string and that it is parseable as a bool.</summary>
	[Fact]
	public void ParseMany_Boolean_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("isActive:\"True\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("isActive");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("True");
		bool.TryParse(firstFilter.Value, out bool _).ShouldBeTrue();
	}

	/// <summary>Verifies that Parse returns a single-word value without modification.</summary>
	[Fact]
	public void Parse_SingleWordValue_ReturnsValueUnchanged()
	{
		var filter = Filter.Parse("status:Closed");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("Closed");
	}

	/// <summary>Verifies that Parse strips surrounding double quotes from a quoted multi-word value.</summary>
	[Fact]
	public void Parse_QuotedMultiWordValue_StripsQuotes()
	{
		var filter = Filter.Parse("status:\"Ready for Test\"");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("Ready for Test");
	}

	/// <summary>Verifies that ParseMany strips quotes from both filters when parsing multiple filters with quoted values.</summary>
	[Fact]
	public void ParseMany_MultipleFiltersWithQuotedValue_StripsQuotesFromBoth()
	{
		var filters = Filter.ParseMany("status:\"Ready for Test\" type:Bug").ToList();

		filters.Count.ShouldBe(2);
		filters[0].Key.ShouldBe("status");
		filters[0].Value.ShouldBe("Ready for Test");
		filters[1].Key.ShouldBe("type");
		filters[1].Value.ShouldBe("Bug");
	}

	/// <summary>Verifies that Parse strips quotes from both Value and Value2 in a range filter with quoted values.</summary>
	[Fact]
	public void Parse_RangeWithQuotedValues_StripsQuotesFromBoth()
	{
		var filter = Filter.Parse("price:>\"10\"|\"20\"<");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.Range);
		filter.Value.ShouldBe("10");
		filter.Value2.ShouldBe("20");
	}

	/// <summary>Verifies that Parse leaves the value unchanged when only a leading quote is present.</summary>
	[Fact]
	public void Parse_OnlyLeadingQuote_LeavesValueUnchanged()
	{
		var filter = Filter.Parse("status:\"OpenOnly");

		filter.Key.ShouldBe("status");
		filter.Value.ShouldBe("\"OpenOnly");
	}

	/// <summary>Verifies that Parse leaves the value unchanged when only a trailing quote is present.</summary>
	[Fact]
	public void Parse_OnlyTrailingQuote_LeavesValueUnchanged()
	{
		var filter = Filter.Parse("status:OpenOnly\"");

		filter.Key.ShouldBe("status");
		filter.Value.ShouldBe("OpenOnly\"");
	}

	/// <summary>Verifies that Parse strips balanced double quotes from an empty quoted value, producing an empty string.</summary>
	[Fact]
	public void Parse_EmptyQuotedValue_StripsQuotes()
	{
		var filter = Filter.Parse("status:\"\"");

		filter.Key.ShouldBe("status");
		filter.Value.ShouldBe(string.Empty);
	}
}

internal enum EnumWithoutDisplay
{
	FirstValue,
	SecondValue,
}

internal enum EnumWithDisplay
{
	[Display(Name = "Needs Improvement")]
	NeedsImprovement,

	[Display(Name = "In Progress")]
	InProgress,

	Simple,
}
