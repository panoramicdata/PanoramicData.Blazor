using PanoramicData.Blazor.Models;
using Shouldly;

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
		firstFilter.Value.ShouldBe("\"15/08/2023 21:26:07 +01:00\"");
	}

	[Fact]
	public void ParseMany_DateTimeWithoutTimeZone_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:>\"15/08/2023 21:26:07.000\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("\"15/08/2023 21:26:07.000\"");
	}

	[Fact]
	public void ParseMany_DateOnly_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:\"15/08/2023\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"15/08/2023\"");
	}

	[Fact]
	public void ParseMany_DateOnlyAlternativeFormat_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:\"15-08-2023\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"15-08-2023\"");
	}

	[Fact]
	public void ParseMany_DateAndTime_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("claimedAt:\"15/08/2023 21:00:00\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("claimedAt");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"15/08/2023 21:00:00\"");
	}

	[Fact]
	public void ParseMany_Double_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("price:>\"2.4\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("price");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("\"2.4\"");
		double.TryParse(firstFilter.Value.Replace("\"", string.Empty), out double _).ShouldBeTrue();
	}

	[Fact]
	public void ParseMany_Integer_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("count:>\"2\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("count");
		firstFilter.FilterType.ShouldBe(FilterTypes.GreaterThan);
		firstFilter.Value.ShouldBe("\"2\"");
		int.TryParse(firstFilter.Value.Replace("\"", string.Empty), out int _).ShouldBeTrue();
	}

	[Fact]
	public void ParseMany_String_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("name:\"A string\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("name");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"A string\"");
	}

	[Fact]
	public void ParseMany_Boolean_PreservesOriginalValue()
	{
		var filter = Filter.ParseMany("isActive:\"True\"").ToList();
		filter.Count.ShouldBe(1);
		var firstFilter = filter[0];
		firstFilter.Key.ShouldBe("isActive");
		firstFilter.FilterType.ShouldBe(FilterTypes.Equals);
		firstFilter.Value.ShouldBe("\"True\"");
		bool.TryParse(firstFilter.Value.Replace("\"", string.Empty), out bool _).ShouldBeTrue();
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

	#endregion
}