using PanoramicData.Blazor.Models;
using Shouldly;
using System.ComponentModel.DataAnnotations;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the Filter class: parsing values as written, formatting and date recognition.</summary>
public partial class FilterTests
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
