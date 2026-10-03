using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// ParseDateTime tests for <see cref="Filter"/>.
/// </summary>
public partial class FilterTests
{
	/// <summary>Verifies that ParseDateTime parses an ISO 8601 UTC date string with second precision.</summary>
	[Fact]
	public void ParseDateTime_ValidIsoFormat_ParsesWithSecondPrecision()
	{
		var parsed = Filter.ParseDateTime("2023-08-15T21:26:07Z");

		parsed.ShouldNotBeNull();
		parsed.Value.Year.ShouldBe(2023);
		parsed.Value.Month.ShouldBe(8);
		// Note: Day may vary based on local timezone conversion
		parsed.Precision.ShouldBe(DatePrecision.Second);
	}

	/// <summary>Verifies that ParseDateTime strips surrounding quotes before parsing a date string.</summary>
	[Fact]
	public void ParseDateTime_QuotedValue_ParsesCorrectly()
	{
		var parsed = Filter.ParseDateTime("\"2023-08-15T21:26:07Z\"");

		parsed.ShouldNotBeNull();
		parsed.Value.Year.ShouldBe(2023);
	}

	/// <summary>Verifies that ParseDateTime returns Day precision for a date-only string.</summary>
	[Fact]
	public void ParseDateTime_DateOnly_ReturnsDayPrecision()
	{
		var parsed = Filter.ParseDateTime("15/08/2023");

		parsed.ShouldNotBeNull();
		parsed.Precision.ShouldBe(DatePrecision.Day);
	}

	/// <summary>Verifies that ParseDateTime returns null for a non-date string.</summary>
	[Fact]
	public void ParseDateTime_InvalidValue_ReturnsNull()
	{
		Filter.ParseDateTime("not a date").ShouldBeNull();
	}

	/// <summary>Verifies that ParseDateTime returns null for a null input.</summary>
	[Fact]
	public void ParseDateTime_NullInput_ReturnsNull()
	{
		Filter.ParseDateTime(null).ShouldBeNull();
	}

	/// <summary>Verifies that ParseDateTime returns Minute precision for a date and time string without seconds.</summary>
	[Fact]
	public void ParseDateTime_DateWithTime_ReturnsMinutePrecision()
	{
		Filter.ParseDateTime("15/08/2023 21:26")!.Precision.ShouldBe(DatePrecision.Minute);
	}

	/// <summary>Verifies that ParseDateTime returns Millisecond precision for a date and time string with milliseconds.</summary>
	[Fact]
	public void ParseDateTime_DateWithMilliseconds_ReturnsMillisecondPrecision()
	{
		Filter.ParseDateTime("2023-08-15 21:26:07.123")!.Precision.ShouldBe(DatePrecision.Millisecond);
	}

	/// <summary>Verifies that ParseDateTime returns Second precision for a date and time string with seconds but no milliseconds.</summary>
	[Fact]
	public void ParseDateTime_DateWithSeconds_ReturnsSecondPrecision()
	{
		Filter.ParseDateTime("2023-08-15 21:26:07")!.Precision.ShouldBe(DatePrecision.Second);
	}
}
