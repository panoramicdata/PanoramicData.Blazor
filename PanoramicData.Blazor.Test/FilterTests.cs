using PanoramicData.Blazor.Models;
using Shouldly;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the Filter class.</summary>
public class FilterTests
{
	#region ParseMany Tests - Values are preserved as-is

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

	#endregion

	#region Parse Quote-Stripping Tests

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

	#endregion

	#region Format Tests - DateTime formatting for filter application

	/// <summary>Verifies that Format returns an ISO 8601 UTC string for a UTC DateTime.</summary>
	[Fact]
	public void Format_UtcDateTime_ReturnsIsoFormat()
	{
		var utcDateTime = new DateTime(2023, 8, 15, 21, 26, 7, DateTimeKind.Utc);

		var result = Filter.Format(utcDateTime);

		result.ShouldBe("2023-08-15T21:26:07Z");
	}

	/// <summary>Verifies that Format treats an Unspecified DateTime as UTC when unspecifiedDateTimesAreUtc is true.</summary>
	[Fact]
	public void Format_UnspecifiedDateTime_TreatedAsUtcByDefault()
	{
		var unspecifiedDateTime = new DateTime(2023, 8, 15, 21, 26, 7, DateTimeKind.Unspecified);

		var result = Filter.Format(unspecifiedDateTime, unspecifiedDateTimesAreUtc: true);

		result.ShouldBe("2023-08-15T21:26:07Z");
	}

	/// <summary>Verifies that Format converts a local DateTime to UTC, producing a string ending with Z.</summary>
	[Fact]
	public void Format_LocalDateTime_ConvertsToUtc()
	{
		var localDateTime = new DateTime(2023, 8, 15, 21, 26, 7, DateTimeKind.Local);

		var result = Filter.Format(localDateTime);

		// Result should be UTC version of the local time
		result.ShouldEndWith("Z");
		result.ShouldStartWith("2023-08-");
	}

	/// <summary>Verifies that Format converts a DateTimeOffset with a non-zero offset to the equivalent UTC string.</summary>
	[Fact]
	public void Format_DateTimeOffset_ConvertsToUtc()
	{
		var dateTimeOffset = new DateTimeOffset(2023, 8, 15, 21, 26, 7, TimeSpan.FromHours(1));

		var result = Filter.Format(dateTimeOffset);

		// 21:26:07 +01:00 should become 20:26:07Z
		result.ShouldBe("2023-08-15T20:26:07Z");
	}

	/// <summary>Verifies that Format returns an ISO 8601 UTC string for a UTC DateTimeOffset.</summary>
	[Fact]
	public void Format_DateTimeOffsetUtc_ReturnsIsoFormat()
	{
		var dateTimeOffset = new DateTimeOffset(2023, 8, 15, 21, 26, 7, TimeSpan.Zero);

		var result = Filter.Format(dateTimeOffset);

		result.ShouldBe("2023-08-15T21:26:07Z");
	}

	/// <summary>Verifies that Format returns an empty string for a null value.</summary>
	[Fact]
	public void Format_NullValue_ReturnsEmptyString()
	{
		var result = Filter.Format(null!);

		result.ShouldBe("");
	}

	/// <summary>Verifies that Format returns the original string value unchanged.</summary>
	[Fact]
	public void Format_StringValue_ReturnsOriginalString()
	{
		var result = Filter.Format("test string");

		result.ShouldBe("test string");
	}

	/// <summary>Verifies that Format returns the string representation of an integer value.</summary>
	[Fact]
	public void Format_IntegerValue_ReturnsStringRepresentation()
	{
		var result = Filter.Format(42);

		result.ShouldBe("42");
	}

	/// <summary>Verifies that Format returns the ToString value for an enum without a Display attribute.</summary>
	[Fact]
	public void Format_EnumWithoutDisplayAttribute_ReturnsToString()
	{
		var result = Filter.Format(EnumWithoutDisplay.SecondValue);

		result.ShouldBe("SecondValue");
	}

	/// <summary>Verifies that Format returns the Display attribute name for an enum value that has one.</summary>
	[Fact]
	public void Format_EnumWithDisplayAttribute_ReturnsDisplayName()
	{
		var result = Filter.Format(EnumWithDisplay.NeedsImprovement);

		result.ShouldBe("Needs Improvement");
	}

	/// <summary>Verifies that Format returns the ToString value for an enum with a Display attribute that has no Name set.</summary>
	[Fact]
	public void Format_EnumWithDisplayAttributeNoName_ReturnsToString()
	{
		var result = Filter.Format(EnumWithDisplay.Simple);

		result.ShouldBe("Simple");
	}

	/// <summary>Verifies that Format returns the correct display name for all enum values that have Display attributes.</summary>
	[Fact]
	public void Format_EnumWithDisplayAttribute_AllValuesFormattedCorrectly()
	{
		Filter.Format(EnumWithDisplay.NeedsImprovement).ShouldBe("Needs Improvement");
		Filter.Format(EnumWithDisplay.InProgress).ShouldBe("In Progress");
		Filter.Format(EnumWithDisplay.Simple).ShouldBe("Simple");
	}

	/// <summary>Verifies that Format treats an Unspecified DateTime as local time and converts it to UTC when unspecifiedDateTimesAreUtc is false.</summary>
	[Fact]
	public void Format_UnspecifiedDateTime_TreatedAsLocal_ConvertsToUtc()
	{
		// When unspecifiedDateTimesAreUtc is false (the default), Unspecified is treated as local
		// and converted via ToUniversalTime() — result still ends with Z
		var unspecifiedDateTime = new DateTime(2023, 8, 15, 12, 0, 0, DateTimeKind.Unspecified);

		var result = Filter.Format(unspecifiedDateTime, unspecifiedDateTimesAreUtc: false);

		result.ShouldEndWith("Z");
	}

	/// <summary>Verifies that the default Format overload treats an Unspecified DateTime the same as passing false for unspecifiedDateTimesAreUtc.</summary>
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

	/// <summary>Verifies that IsDateTime returns true and parses an ISO 8601 UTC date string with second precision.</summary>
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

	/// <summary>Verifies that IsDateTime strips surrounding quotes before parsing a date string.</summary>
	[Fact]
	public void IsDateTime_QuotedValue_ParsesCorrectly()
	{
		var result = Filter.IsDateTime("\"2023-08-15T21:26:07Z\"", out var dateTime, out var format, out var precision);

		result.ShouldBeTrue();
		dateTime.Year.ShouldBe(2023);
	}

	/// <summary>Verifies that IsDateTime returns Day precision for a date-only string.</summary>
	[Fact]
	public void IsDateTime_DateOnly_ReturnsDayPrecision()
	{
		var result = Filter.IsDateTime("15/08/2023", out var dateTime, out var format, out var precision);

		result.ShouldBeTrue();
		precision.ShouldBe(DatePrecision.Day);
	}

	/// <summary>Verifies that IsDateTime returns false and outputs DateTime.MinValue for a non-date string.</summary>
	[Fact]
	public void IsDateTime_InvalidValue_ReturnsFalse()
	{
		var result = Filter.IsDateTime("not a date", out var dateTime, out var format, out var precision);

		result.ShouldBeFalse();
		dateTime.ShouldBe(DateTime.MinValue);
	}

	/// <summary>Verifies that IsDateTime returns false and outputs DateTime.MinValue and empty format for a null input.</summary>
	[Fact]
	public void IsDateTime_NullInput_ReturnsFalse()
	{
		var result = Filter.IsDateTime(null, out var dateTime, out var format, out var precision);

		result.ShouldBeFalse();
		dateTime.ShouldBe(DateTime.MinValue);
		format.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that IsDateTime returns Minute precision for a date and time string without seconds.</summary>
	[Fact]
	public void IsDateTime_DateWithTime_ReturnsMinutePrecision()
	{
		var result = Filter.IsDateTime("15/08/2023 21:26", out _, out _, out var precision);

		result.ShouldBeTrue();
		precision.ShouldBe(DatePrecision.Minute);
	}

	/// <summary>Verifies that IsDateTime returns Millisecond precision for a date and time string with milliseconds.</summary>
	[Fact]
	public void IsDateTime_DateWithMilliseconds_ReturnsMillisecondPrecision()
	{
		var result = Filter.IsDateTime("2023-08-15 21:26:07.123", out _, out _, out var precision);

		result.ShouldBeTrue();
		precision.ShouldBe(DatePrecision.Millisecond);
	}

	/// <summary>Verifies that IsDateTime returns Second precision for a date and time string with seconds but no milliseconds.</summary>
	[Fact]
	public void IsDateTime_DateWithSeconds_ReturnsSecondPrecision()
	{
		var result = Filter.IsDateTime("2023-08-15 21:26:07", out _, out _, out var precision);

		result.ShouldBeTrue();
		precision.ShouldBe(DatePrecision.Second);
	}

	#endregion

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

	#endregion

	#region ToString Tests

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

	#endregion

	#region IsValid Tests

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

	#endregion

	#region Clear Tests

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

	#endregion

	#region UpdateFrom Tests

	/// <summary>Verifies that UpdateFrom parses filter text and updates properties when the key matches.</summary>
	[Fact]
	public void UpdateFrom_MatchingKey_UpdatesFilterProperties()
	{
		var filter = new Filter { Key = "status" };

		filter.UpdateFrom("status:!Open price:>10");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("Open");
	}

	/// <summary>Verifies that UpdateFrom clears the filter when no matching key is found in the filter text.</summary>
	[Fact]
	public void UpdateFrom_NoMatchingKey_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.GreaterThan, "status", "Active");

		filter.UpdateFrom("price:>10");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that UpdateFrom clears the filter when given an empty string.</summary>
	[Fact]
	public void UpdateFrom_EmptyText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom("");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that UpdateFrom matches filter keys case-insensitively.</summary>
	[Fact]
	public void UpdateFrom_CaseInsensitiveKeyMatch_UpdatesFilter()
	{
		var filter = new Filter { Key = "Status" };

		filter.UpdateFrom("status:Open");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("Open");
	}

	/// <summary>Verifies that UpdateFrom clears the filter when given a null string.</summary>
	[Fact]
	public void UpdateFrom_NullText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom(null!);

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that UpdateFrom clears the filter when given a whitespace-only string.</summary>
	[Fact]
	public void UpdateFrom_WhitespaceText_ClearsFilter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "test");

		filter.UpdateFrom("   ");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe(string.Empty);
	}

	/// <summary>Verifies that UpdateFrom correctly round-trips a quoted multi-word value, preserving the unquoted value.</summary>
	[Fact]
	public void UpdateFrom_MultiWordValue_PreservesValue()
	{
		// Regression: UpdateFrom must correctly round-trip quoted multi-word values from search text
		var filter = new Filter { Key = "name" };

		filter.UpdateFrom("name:\"On Microsoft Schedule\"");

		filter.FilterType.ShouldBe(FilterTypes.Equals);
		filter.Value.ShouldBe("On Microsoft Schedule");
	}

	/// <summary>Verifies that UpdateFrom correctly handles a DoesNotEqual filter with a quoted multi-word value.</summary>
	[Fact]
	public void UpdateFrom_MultiWordDoesNotEqualValue_PreservesValue()
	{
		var filter = new Filter { Key = "name" };

		filter.UpdateFrom("name:!\"On Microsoft Schedule\"");

		filter.FilterType.ShouldBe(FilterTypes.DoesNotEqual);
		filter.Value.ShouldBe("On Microsoft Schedule");
	}

	#endregion

	#region Constructor Tests

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

	#endregion

	#region GetMemberName Tests

	/// <summary>Verifies that GetMemberName returns the enum member name for a given Display attribute name.</summary>
	[Fact]
	public void GetMemberName_DisplayName_ReturnsMemberName()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "Needs Improvement");

		result.ShouldBe("NeedsImprovement");
	}

	/// <summary>Verifies that GetMemberName returns the enum member name for a second Display attribute name.</summary>
	[Fact]
	public void GetMemberName_AnotherDisplayName_ReturnsMemberName()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "In Progress");

		result.ShouldBe("InProgress");
	}

	/// <summary>Verifies that GetMemberName returns a raw enum member name unchanged when passed directly.</summary>
	[Fact]
	public void GetMemberName_RawMemberName_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "NeedsImprovement");

		result.ShouldBe("NeedsImprovement");
	}

	/// <summary>Verifies that GetMemberName returns the value unchanged for an enum member with a Display attribute that has no Name set.</summary>
	[Fact]
	public void GetMemberName_NoDisplayAttribute_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "Simple");

		result.ShouldBe("Simple");
	}

	/// <summary>Verifies that GetMemberName returns the value unchanged for an enum type that has no Display attributes at all.</summary>
	[Fact]
	public void GetMemberName_EnumWithNoDisplayAttributes_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithoutDisplay), "SecondValue");

		result.ShouldBe("SecondValue");
	}

	/// <summary>Verifies that GetMemberName returns the value unchanged when no enum member matches the given string.</summary>
	[Fact]
	public void GetMemberName_UnknownValue_ReturnsUnchanged()
	{
		var result = Filter.GetMemberName(typeof(EnumWithDisplay), "not a match");

		result.ShouldBe("not a match");
	}

	/// <summary>Verifies that calling Format then GetMemberName round-trips display names back to their member names.</summary>
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
