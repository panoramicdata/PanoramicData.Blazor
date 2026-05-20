using PanoramicData.Blazor.Models;
using Shouldly;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test;

public class FilterTests
{
	#region ParseMany Tests - Values are preserved as-is

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

	#endregion

	#region Parse Quote-Stripping Tests

	[Fact]
	public void Parse_SingleWordValue_ReturnsValueUnchanged()
	{
		var filter = Filter.Parse("status:Closed");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("Closed");
	}

	[Fact]
	public void Parse_QuotedMultiWordValue_StripsQuotes()
	{
		var filter = Filter.Parse("status:\"Ready for Test\"");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("Ready for Test");
	}

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

	[Fact]
	public void Parse_RangeWithQuotedValues_StripsQuotesFromBoth()
	{
		var filter = Filter.Parse("price:>\"10\"|\"20\"<");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.Range);
		filter.Value.ShouldBe("10");
		filter.Value2.ShouldBe("20");
	}

	[Fact]
	public void Parse_OnlyLeadingQuote_LeavesValueUnchanged()
	{
		var filter = Filter.Parse("status:\"OpenOnly");

		filter.Key.ShouldBe("status");
		filter.Value.ShouldBe("\"OpenOnly");
	}

	[Fact]
	public void Parse_OnlyTrailingQuote_LeavesValueUnchanged()
	{
		var filter = Filter.Parse("status:OpenOnly\"");

		filter.Key.ShouldBe("status");
		filter.Value.ShouldBe("OpenOnly\"");
	}

	[Fact]
	public void Parse_EmptyQuotedValue_StripsQuotes()
	{
		var filter = Filter.Parse("status:\"\"");

		filter.Key.ShouldBe("status");
		filter.Value.ShouldBe(string.Empty);
	}

	#endregion

	#region Format Tests - DateTime formatting for filter application

	[Fact]
	public void Format_UtcDateTime_ReturnsIsoFormat()
	{
		var utcDateTime = new DateTime(2023, 8, 15, 21, 26, 7, DateTimeKind.Utc);

		var result = Filter.Format(utcDateTime);

		result.ShouldBe("2023-08-15T21:26:07Z");
	}

	[Fact]
	public void Format_UnspecifiedDateTime_TreatedAsUtcByDefault()
	{
		var unspecifiedDateTime = new DateTime(2023, 8, 15, 21, 26, 7, DateTimeKind.Unspecified);

		var result = Filter.Format(unspecifiedDateTime, unspecifiedDateTimesAreUtc: true);

		result.ShouldBe("2023-08-15T21:26:07Z");
	}

	[Fact]
	public void Format_LocalDateTime_ConvertsToUtc()
	{
		var localDateTime = new DateTime(2023, 8, 15, 21, 26, 7, DateTimeKind.Local);

		var result = Filter.Format(localDateTime);

		// Result should be UTC version of the local time
		result.ShouldEndWith("Z");
		result.ShouldStartWith("2023-08-");
	}

	[Fact]
	public void Format_DateTimeOffset_ConvertsToUtc()
	{
		var dateTimeOffset = new DateTimeOffset(2023, 8, 15, 21, 26, 7, TimeSpan.FromHours(1));

		var result = Filter.Format(dateTimeOffset);

		// 21:26:07 +01:00 should become 20:26:07Z
		result.ShouldBe("2023-08-15T20:26:07Z");
	}

	[Fact]
	public void Format_DateTimeOffsetUtc_ReturnsIsoFormat()
	{
		var dateTimeOffset = new DateTimeOffset(2023, 8, 15, 21, 26, 7, TimeSpan.Zero);

		var result = Filter.Format(dateTimeOffset);

		result.ShouldBe("2023-08-15T21:26:07Z");
	}

	[Fact]
	public void Format_NullValue_ReturnsEmptyString()
	{
		var result = Filter.Format(null!);

		result.ShouldBe("");
	}

	[Fact]
	public void Format_StringValue_ReturnsOriginalString()
	{
		var result = Filter.Format("test string");

		result.ShouldBe("test string");
	}

	[Fact]
	public void Format_IntegerValue_ReturnsStringRepresentation()
	{
		var result = Filter.Format(42);

		result.ShouldBe("42");
	}

	[Fact]
	public void Format_EnumWithoutDisplayAttribute_ReturnsToString()
	{
		var result = Filter.Format(EnumWithoutDisplay.SecondValue);

		result.ShouldBe("SecondValue");
	}

	[Fact]
	public void Format_EnumWithDisplayAttribute_ReturnsDisplayName()
	{
		var result = Filter.Format(EnumWithDisplay.NeedsImprovement);

		result.ShouldBe("Needs Improvement");
	}

	[Fact]
	public void Format_EnumWithDisplayAttributeNoName_ReturnsToString()
	{
		var result = Filter.Format(EnumWithDisplay.Simple);

		result.ShouldBe("Simple");
	}

	[Fact]
	public void Format_EnumWithDisplayAttribute_AllValuesFormattedCorrectly()
	{
		Filter.Format(EnumWithDisplay.NeedsImprovement).ShouldBe("Needs Improvement");
		Filter.Format(EnumWithDisplay.InProgress).ShouldBe("In Progress");
		Filter.Format(EnumWithDisplay.Simple).ShouldBe("Simple");
	}

	[Fact]
	public void Format_UnspecifiedDateTime_TreatedAsLocal_ConvertsToUtc()
	{
		// When unspecifiedDateTimesAreUtc is false (the default), Unspecified is treated as local
		// and converted via ToUniversalTime() — result still ends with Z
		var unspecifiedDateTime = new DateTime(2023, 8, 15, 12, 0, 0, DateTimeKind.Unspecified);

		var result = Filter.Format(unspecifiedDateTime, unspecifiedDateTimesAreUtc: false);

		result.ShouldEndWith("Z");
	}

	[Fact]
	public void Format_DefaultOverload_TreatsUnspecifiedAsLocal()
	{
		// The no-arg overload passes false for unspecifiedDateTimesAreUtc
		var unspecifiedDateTime = new DateTime(2023, 8, 15, 12, 0, 0, DateTimeKind.Unspecified);
		var explicitResult = Filter.Format(unspecifiedDateTime, unspecifiedDateTimesAreUtc: false);

		var defaultResult = Filter.Format(unspecifiedDateTime);

		defaultResult.ShouldBe(explicitResult);
	}

	#endregion

	#region IsDateTime Tests

	[Fact]
	public void IsDateTime_ValidIsoFormat_ReturnsTrue()
	{
		var result = Filter.IsDateTime("2023-08-15T21:26:07Z", out var dateTime, out var format, out var precision);

		result.ShouldBeTrue();
		dateTime.Year.ShouldBe(2023);
		dateTime.Month.ShouldBe(8);
		// Note: Day may vary based on local timezone conversion
		precision.ShouldBe(DatePrecision.Second);
	}

	[Fact]
	public void IsDateTime_QuotedValue_ParsesCorrectly()
	{
		var result = Filter.IsDateTime("\"2023-08-15T21:26:07Z\"", out var dateTime, out var format, out var precision);

		result.ShouldBeTrue();
		dateTime.Year.ShouldBe(2023);
	}

	[Fact]
	public void IsDateTime_DateOnly_ReturnsDayPrecision()
	{
		var result = Filter.IsDateTime("15/08/2023", out var dateTime, out var format, out var precision);

		result.ShouldBeTrue();
		precision.ShouldBe(DatePrecision.Day);
	}

	[Fact]
	public void IsDateTime_InvalidValue_ReturnsFalse()
	{
		var result = Filter.IsDateTime("not a date", out var dateTime, out var format, out var precision);

		result.ShouldBeFalse();
		dateTime.ShouldBe(DateTime.MinValue);
	}

	[Fact]
	public void IsDateTime_NullInput_ReturnsFalse()
	{
		var result = Filter.IsDateTime(null, out var dateTime, out var format, out var precision);

		result.ShouldBeFalse();
		dateTime.ShouldBe(DateTime.MinValue);
		format.ShouldBe(string.Empty);
	}

	[Fact]
	public void IsDateTime_DateWithTime_ReturnsMinutePrecision()
	{
		var result = Filter.IsDateTime("15/08/2023 21:26", out _, out _, out var precision);

		result.ShouldBeTrue();
		precision.ShouldBe(DatePrecision.Minute);
	}

	[Fact]
	public void IsDateTime_DateWithMilliseconds_ReturnsMillisecondPrecision()
	{
		var result = Filter.IsDateTime("2023-08-15 21:26:07.123", out _, out _, out var precision);

		result.ShouldBeTrue();
		precision.ShouldBe(DatePrecision.Millisecond);
	}

	[Fact]
	public void IsDateTime_DateWithSeconds_ReturnsSecondPrecision()
	{
		var result = Filter.IsDateTime("2023-08-15 21:26:07", out _, out _, out var precision);

		result.ShouldBeTrue();
		precision.ShouldBe(DatePrecision.Second);
	}

	#endregion

	#region Parse - All Filter Types

	[Fact]
	public void Parse_DoesNotEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!Closed");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("Closed");
	}

	[Fact]
	public void Parse_StartsWith_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:John*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.StartsWith);
		filter.Value.ShouldBe("John");
	}

	[Fact]
	public void Parse_EndsWith_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:*son");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.EndsWith);
		filter.Value.ShouldBe("son");
	}

	[Fact]
	public void Parse_Contains_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:*oh*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.Contains);
		filter.Value.ShouldBe("oh");
	}

	[Fact]
	public void Parse_DoesNotContain_ParsesCorrectly()
	{
		var filter = Filter.Parse("name:!*test*");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.DoesNotContain);
		filter.Value.ShouldBe("test");
	}

	[Fact]
	public void Parse_In_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:In(A,B,C)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.In);
		filter.Value.ShouldBe("A,B,C");
	}

	[Fact]
	public void Parse_NotIn_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!In(A,B)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.NotIn);
		filter.Value.ShouldBe("A,B");
	}

	[Fact]
	public void Parse_InWithQuotedMultiWordItems_PreservesQuotes()
	{
		var filter = Filter.Parse("name:In(\"Chain Test I\"|\"A - Test Schedule\")");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.In);
		filter.Value.ShouldBe("\"Chain Test I\"|\"A - Test Schedule\"");
	}

	[Fact]
	public void Parse_NotInWithQuotedMultiWordItems_PreservesQuotes()
	{
		var filter = Filter.Parse("name:!In(\"Chain Test I\"|\"A - Test Schedule\")");

		filter.Key.ShouldBe("name");
		filter.FilterType.ShouldBe(FilterTypes.NotIn);
		filter.Value.ShouldBe("\"Chain Test I\"|\"A - Test Schedule\"");
	}

	[Fact]
	public void Parse_GreaterThan_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>100");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		filter.Value.ShouldBe("100");
	}

	[Fact]
	public void Parse_GreaterThanOrEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>=100");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.GreaterThanOrEqual);
		filter.Value.ShouldBe("100");
	}

	[Fact]
	public void Parse_LessThan_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:<50");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.LessThan);
		filter.Value.ShouldBe("50");
	}

	[Fact]
	public void Parse_LessThanOrEqual_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:<=50");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.LessThanOrEqual);
		filter.Value.ShouldBe("50");
	}

	[Fact]
	public void Parse_Range_ParsesCorrectly()
	{
		var filter = Filter.Parse("price:>10|100<");

		filter.Key.ShouldBe("price");
		filter.FilterType.ShouldBe(FilterTypes.Range);
		filter.Value.ShouldBe("10");
		filter.Value2.ShouldBe("100");
	}

	[Fact]
	public void Parse_IsNull_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:(null)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsNull);
		filter.Value.ShouldBe(string.Empty);
	}

	[Fact]
	public void Parse_IsNotNull_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:!(null)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsNotNull);
		filter.Value.ShouldBe(string.Empty);
	}

	[Fact]
	public void Parse_IsEmpty_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:(empty)");

		filter.Key.ShouldBe("status");
		filter.FilterType.ShouldBe(FilterTypes.IsEmpty);
		filter.Value.ShouldBe(string.Empty);
	}

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

	[Fact]
	public void Parse_NoColon_ReturnsEmptyFilter()
	{
		var filter = Filter.Parse("noColonHere");

		filter.Key.ShouldBe(string.Empty);
		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	[Fact]
	public void Parse_ValueContainingColon_SplitsOnFirstColon()
	{
		var filter = Filter.Parse("url:https://example.com");

		filter.Key.ShouldBe("url");
		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("https://example.com");
	}

	[Fact]
	public void Parse_WithKeyMappings_SetsPropertyName()
	{
		var mappings = new Dictionary<string, string> { ["s"] = "Status" };

		var filter = Filter.Parse("s:Open", mappings);

		filter.Key.ShouldBe("s");
		filter.PropertyName.ShouldBe("Status");
		filter.Value.ShouldBe("Open");
	}

	[Fact]
	public void Parse_InCaseInsensitive_ParsesCorrectly()
	{
		var filter = Filter.Parse("status:in(A,B)");

		filter.FilterType.ShouldBe(FilterTypes.In);
		filter.Value.ShouldBe("A,B");
	}

	#endregion

	#region Parse - Quote Stripping Combined with Operators

	[Fact]
	public void Parse_DoesNotEqualWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("status:!\"Ready for Test\"");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("Ready for Test");
	}

	[Fact]
	public void Parse_GreaterThanOrEqualWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("price:>=\"10.5\"");

		filter.FilterType.ShouldBe(FilterTypes.GreaterThanOrEqual);
		filter.Value.ShouldBe("10.5");
	}

	[Fact]
	public void Parse_LessThanWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("price:<\"99\"");

		filter.FilterType.ShouldBe(FilterTypes.LessThan);
		filter.Value.ShouldBe("99");
	}

	[Fact]
	public void Parse_ContainsWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("name:*\"test value\"*");

		filter.FilterType.ShouldBe(FilterTypes.Contains);
		filter.Value.ShouldBe("test value");
	}

	[Fact]
	public void Parse_StartsWithWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("name:\"test value\"*");

		filter.FilterType.ShouldBe(FilterTypes.StartsWith);
		filter.Value.ShouldBe("test value");
	}

	[Fact]
	public void Parse_EndsWithWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("name:*\"test value\"");

		filter.FilterType.ShouldBe(FilterTypes.EndsWith);
		filter.Value.ShouldBe("test value");
	}

	[Fact]
	public void Parse_DoesNotContainWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("name:!*\"test value\"*");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotContain);
		filter.Value.ShouldBe("test value");
	}

	[Fact]
	public void Parse_LessThanOrEqualWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("price:<=\"50\"");

		filter.FilterType.ShouldBe(FilterTypes.LessThanOrEqual);
		filter.Value.ShouldBe("50");
	}

	[Fact]
	public void Parse_GreaterThanWithQuotes_StripsQuotes()
	{
		var filter = Filter.Parse("price:>\"10\"");

		filter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		filter.Value.ShouldBe("10");
	}

	#endregion

	#region ParseMany - Edge Cases

	[Fact]
	public void ParseMany_EmptyString_YieldsNoFilters()
	{
		var filters = Filter.ParseMany("").ToList();

		filters.Count.ShouldBe(0);
	}

	[Fact]
	public void ParseMany_NullString_YieldsNoFilters()
	{
		var filters = Filter.ParseMany(null!).ToList();

		filters.Count.ShouldBe(0);
	}

	[Fact]
	public void ParseMany_WhitespaceOnly_YieldsNoFilters()
	{
		var filters = Filter.ParseMany("   ").ToList();

		filters.Count.ShouldBe(0);
	}

	[Fact]
	public void ParseMany_NoColon_YieldsNoFilters()
	{
		var filters = Filter.ParseMany("noColonAnywhere").ToList();

		filters.Count.ShouldBe(0);
	}

	[Fact]
	public void ParseMany_HashDelimitedValue_PreservesSpaces()
	{
		var filters = Filter.ParseMany("name:#value with spaces#").ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("name");
		filters[0].Value.ShouldBe("#value with spaces#");
	}

	[Fact]
	public void ParseMany_UnterminatedQuote_AutoClosesAndParses()
	{
		var filters = Filter.ParseMany("name:\"unterminated").ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("name");
		// ParseMany auto-appends closing quote, then Parse strips the balanced pair
		filters[0].Value.ShouldBe("unterminated");
	}

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

	[Fact]
	public void ParseMany_UnterminatedHash_AutoClosesAndParses()
	{
		var filters = Filter.ParseMany("name:#unterminated").ToList();

		filters.Count.ShouldBe(1);
		filters[0].Key.ShouldBe("name");
		filters[0].Value.ShouldBe("#unterminated#");
	}

	#endregion

	#region ToString Tests

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
	public void ToString_AllFilterTypes_ProducesExpectedFormat(FilterTypes filterType, string key, string value, string value2, string expected)
	{
		var filter = new Filter(filterType, key, value, value2);

		filter.ToString().ShouldBe(expected);
	}

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
	public void ToStringThenParse_RoundTrip_PreservesFilterProperties(FilterTypes filterType, string key, string value, string value2)
	{
		var original = new Filter(filterType, key, value, value2);

		var roundTripped = Filter.Parse(original.ToString());

		roundTripped.Key.ShouldBe(original.Key);
		roundTripped.FilterType.ShouldBe(original.FilterType);
		roundTripped.Value.ShouldBe(original.Value);
		roundTripped.Value2.ShouldBe(original.Value2);
	}

	#endregion

	#region IsValid Tests

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

	#endregion

	#region Clear Tests

	[Fact]
	public void Clear_ResetsFilterTypeAndValue()
	{
		var filter = new Filter(FilterTypes.GreaterThan, "price", "100");

		filter.Clear();

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	#endregion

	#region UpdateFrom Tests

	[Fact]
	public void UpdateFrom_MatchingKey_UpdatesFilterProperties()
	{
		var filter = new Filter { Key = "status" };

		filter.UpdateFrom("status:!Open price:>10");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("Open");
	}

	[Fact]
	public void UpdateFrom_NoMatchingKey_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.GreaterThan, "status", "Active");

		filter.UpdateFrom("price:>10");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	[Fact]
	public void UpdateFrom_EmptyText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom("");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	[Fact]
	public void UpdateFrom_CaseInsensitiveKeyMatch_UpdatesFilter()
	{
		var filter = new Filter { Key = "Status" };

		filter.UpdateFrom("status:Open");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("Open");
	}

	[Fact]
	public void UpdateFrom_NullText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom(null!);

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	[Fact]
	public void UpdateFrom_WhitespaceText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom("   ");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	#endregion

	#region Constructor Tests

	[Fact]
	public void Constructor_ObjectValue_UsesToString()
	{
		var filter = new Filter(FilterTypes.Equals, "status", (object)42);

		filter.Value.ShouldBe("42");
	}

	[Fact]
	public void Constructor_NullObjectValue_UsesEmptyString()
	{
		var filter = new Filter(FilterTypes.Equals, "status", (object)null!);

		filter.Value.ShouldBe(string.Empty);
	}

	#endregion

	#region GetMemberName Tests

	[Fact]
	public void GetMemberName_DisplayName_ReturnsMemberName()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "Needs Improvement");

		result.ShouldBe("NeedsImprovement");
	}

	[Fact]
	public void GetMemberName_AnotherDisplayName_ReturnsMemberName()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "In Progress");

		result.ShouldBe("InProgress");
	}

	[Fact]
	public void GetMemberName_RawMemberName_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "NeedsImprovement");

		result.ShouldBe("NeedsImprovement");
	}

	[Fact]
	public void GetMemberName_NoDisplayAttribute_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "Simple");

		result.ShouldBe("Simple");
	}

	[Fact]
	public void GetMemberName_EnumWithNoDisplayAttributes_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithoutDisplay), "SecondValue");

		result.ShouldBe("SecondValue");
	}

	[Fact]
	public void GetMemberName_UnknownValue_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "not a match");

		result.ShouldBe("not a match");
	}

	[Theory]
	[InlineData("Needs Improvement", "NeedsImprovement")]
	[InlineData("In Progress", "InProgress")]
	[InlineData("Simple", "Simple")]
	[InlineData("NeedsImprovement", "NeedsImprovement")]
	public void GetMemberName_RoundTrip_FormatThenGetMemberName(string displayName, string expectedMemberName)
	{
		// Simulate the round-trip: Format() produces the display name, GetMemberName() reverses it
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), displayName);

		result.ShouldBe(expectedMemberName);
	}

	#endregion

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
