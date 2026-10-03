using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using System.Globalization;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for the date formats recognised by <see cref="Filter.ParseDateTime(string?)"/>.</summary>
public class FilterDateFormatTests
{
	/// <summary>
	/// Pins how every string accepted before the US date and year/month/hour fixes (#197) parses: the value,
	/// the format that matched and the precision detected. Adding formats must not change any of these.
	/// </summary>
	/// <param name="input">The filter value.</param>
	/// <param name="expectedFormat">The format expected to match.</param>
	/// <param name="expectedPrecision">The precision expected to be detected.</param>
	/// <param name="expectedWallClock">The expected value, as an invariant <c>yyyy-MM-ddTHH:mm:ss.fff</c> string.</param>
	/// <param name="offsetMinutes">
	/// The offset the input carries, or null when it carries none. An input with an offset parses to the
	/// equivalent local time of the machine running the test, so the expectation is converted the same way.
	/// </param>
	[Theory]
	[InlineData("2024-05-15 09:5", "yyyy'-'MM'-'dd HH:m", DatePrecision.Minute, "2024-05-15T09:05:00.000", null)]
	[InlineData("2024-05-15 09:30", "yyyy'-'MM'-'dd HH:m", DatePrecision.Minute, "2024-05-15T09:30:00.000", null)]
	[InlineData("2024-05-15 09:30:15", "yyyy'-'MM'-'dd HH:mm:ss", DatePrecision.Second, "2024-05-15T09:30:15.000", null)]
	[InlineData("2024-05-15", "yyyy'-'MM'-'dd", DatePrecision.Day, "2024-05-15T00:00:00.000", null)]
	[InlineData("2024-05-15 09:30:15 Z", "yyyy'-'MM'-'dd HH:mm:ss K", DatePrecision.Second, "2024-05-15T09:30:15.000", 0)]
	[InlineData("2024-05-15 09:30:15 +02:00", "yyyy'-'MM'-'dd HH:mm:ss K", DatePrecision.Second, "2024-05-15T09:30:15.000", 120)]
	[InlineData("2024-05-15 09:30.123", "yyyy'-'MM'-'dd HH:m.fff", DatePrecision.Millisecond, "2024-05-15T09:30:00.123", null)]
	[InlineData("2024-05-15 09:30:15.123", "yyyy'-'MM'-'dd HH:mm:ss.fff", DatePrecision.Millisecond, "2024-05-15T09:30:15.123", null)]
	[InlineData("2024-05-15 09:30:15.123 Z", "yyyy'-'MM'-'dd HH:mm:ss.fff K", DatePrecision.Millisecond, "2024-05-15T09:30:15.123", 0)]
	[InlineData("15/05/2024 09:30", "dd'/'MM'/'yyyy HH:m", DatePrecision.Minute, "2024-05-15T09:30:00.000", null)]
	[InlineData("15/05/2024 09:30:15", "dd'/'MM'/'yyyy HH:mm:ss", DatePrecision.Second, "2024-05-15T09:30:15.000", null)]
	[InlineData("15/05/2024 09:30:15 Z", "dd'/'MM'/'yyyy HH:mm:ss K", DatePrecision.Second, "2024-05-15T09:30:15.000", 0)]
	[InlineData("15/05/2024", "dd'/'MM'/'yyyy", DatePrecision.Day, "2024-05-15T00:00:00.000", null)]
	[InlineData("03/04/2026", "dd'/'MM'/'yyyy", DatePrecision.Day, "2026-04-03T00:00:00.000", null)]
	[InlineData("15-05-2024 09:30", "dd'-'MM'-'yyyy HH:m", DatePrecision.Minute, "2024-05-15T09:30:00.000", null)]
	[InlineData("15-05-2024 09:30:15", "dd'-'MM'-'yyyy HH:mm:ss", DatePrecision.Second, "2024-05-15T09:30:15.000", null)]
	[InlineData("15-05-2024 09:30:15 Z", "dd'-'MM'-'yyyy HH:mm:ss K", DatePrecision.Second, "2024-05-15T09:30:15.000", 0)]
	[InlineData("15-05-2024", "dd'-'MM'-'yyyy", DatePrecision.Day, "2024-05-15T00:00:00.000", null)]
	[InlineData("15/05/2024 09:30.123", "dd'/'MM'/'yyyy HH:m.fff", DatePrecision.Millisecond, "2024-05-15T09:30:00.123", null)]
	[InlineData("15/05/2024 09:30:15.123", "dd'/'MM'/'yyyy HH:mm:ss.fff", DatePrecision.Millisecond, "2024-05-15T09:30:15.123", null)]
	[InlineData("15/05/2024 09:30:15.123 Z", "dd'/'MM'/'yyyy HH:mm:ss.fff K", DatePrecision.Millisecond, "2024-05-15T09:30:15.123", 0)]
	[InlineData("2024-05-15 09:30 +02:00", "yyyy'-'MM'-'dd HH:m zzz", DatePrecision.Minute, "2024-05-15T09:30:00.000", 120)]
	[InlineData("2024-05-15 +02:00", "yyyy'-'MM'-'dd zzz", DatePrecision.Day, "2024-05-15T00:00:00.000", 120)]
	[InlineData("2024-05-15 09:30.123 +02:00", "yyyy'-'MM'-'dd HH:m.fff zzz", DatePrecision.Millisecond, "2024-05-15T09:30:00.123", 120)]
	[InlineData("2024-05-15 09:30:15.123 +02:00", "yyyy'-'MM'-'dd HH:mm:ss.fff K", DatePrecision.Millisecond, "2024-05-15T09:30:15.123", 120)]
	[InlineData("15/05/2024 09:30 -05:00", "dd'/'MM'/'yyyy HH:m zzz", DatePrecision.Minute, "2024-05-15T09:30:00.000", -300)]
	[InlineData("15/05/2024 09:30:15 -05:00", "dd'/'MM'/'yyyy HH:mm:ss K", DatePrecision.Second, "2024-05-15T09:30:15.000", -300)]
	[InlineData("15/05/2024 -05:00", "dd'/'MM'/'yyyy zzz", DatePrecision.Day, "2024-05-15T00:00:00.000", -300)]
	[InlineData("15-05-2024 09:30 +02:00", "dd'-'MM'-'yyyy HH:m zzz", DatePrecision.Minute, "2024-05-15T09:30:00.000", 120)]
	[InlineData("15-05-2024 09:30:15 +02:00", "dd'-'MM'-'yyyy HH:mm:ss K", DatePrecision.Second, "2024-05-15T09:30:15.000", 120)]
	[InlineData("15-05-2024 +02:00", "dd'-'MM'-'yyyy zzz", DatePrecision.Day, "2024-05-15T00:00:00.000", 120)]
	[InlineData("15/05/2024 09:30.123 +02:00", "dd'/'MM'/'yyyy HH:m.fff zzz", DatePrecision.Millisecond, "2024-05-15T09:30:00.123", 120)]
	[InlineData("15/05/2024 09:30:15.123 +02:00", "dd'/'MM'/'yyyy HH:mm:ss.fff K", DatePrecision.Millisecond, "2024-05-15T09:30:15.123", 120)]
	[InlineData("05/15/2024 09:30 +02:00", "MM'/'dd'/'yyyy HH:m zzz", DatePrecision.Minute, "2024-05-15T09:30:00.000", 120)]
	[InlineData("05/15/2024 09:30:15 +02:00", "MM'/'dd'/'yyyy HH:mm:ss zzz", DatePrecision.Second, "2024-05-15T09:30:15.000", 120)]
	[InlineData("05/15/2024 +02:00", "MM'/'dd'/'yyyy zzz", DatePrecision.Day, "2024-05-15T00:00:00.000", 120)]
	[InlineData("05/15/2024 09:30:15 Z", "MM'/'dd'/'yyyy HH:mm:ss K", DatePrecision.Second, "2024-05-15T09:30:15.000", 0)]
	[InlineData("05/15/2024 09:30.123 +02:00", "MM'/'dd'/'yyyy HH:m.fff zzz", DatePrecision.Millisecond, "2024-05-15T09:30:00.123", 120)]
	[InlineData("05/15/2024 09:30:15.123 +02:00", "MM'/'dd'/'yyyy HH:mm:ss.fff zzz", DatePrecision.Millisecond, "2024-05-15T09:30:15.123", 120)]
	[InlineData("05/15/2024 09:30:15.123 Z", "MM'/'dd'/'yyyy HH:mm:ss.fff K", DatePrecision.Millisecond, "2024-05-15T09:30:15.123", 0)]
	[InlineData("\"2024-05-15\"", "yyyy'-'MM'-'dd", DatePrecision.Day, "2024-05-15T00:00:00.000", null)]
	public void ParseDateTime_PreviouslyAcceptedStrings_ParseAsBefore(string input, string expectedFormat, DatePrecision expectedPrecision, string expectedWallClock, int? offsetMinutes)
	{
		var parsed = Filter.ParseDateTime(input);

		parsed.Should().NotBeNull();
		parsed!.Format.Should().Be(expectedFormat);
		parsed.Precision.Should().Be(expectedPrecision);
		parsed.Value.Should().Be(Expected(expectedWallClock, offsetMinutes));
		parsed.Value.Kind.Should().Be(offsetMinutes is null ? DateTimeKind.Unspecified : DateTimeKind.Local);
	}

	/// <summary>The ISO 8601 form ending in Z is read as UTC and converted to the machine's local time.</summary>
	[Fact]
	public void ParseDateTime_IsoEndingInZ_ParsesAsUtcConvertedToLocal()
	{
		var parsed = Filter.ParseDateTime("2024-05-15T09:30:15Z");

		parsed.Should().NotBeNull();
		parsed!.Format.Should().Be("yyyy-MM-ddTHH:mm:ssZ");
		parsed.Precision.Should().Be(DatePrecision.Second);
		parsed.Value.Should().Be(Expected("2024-05-15T09:30:15.000", 0));
		parsed.Value.Kind.Should().Be(DateTimeKind.Local);
	}

	/// <summary>Strings that are not dates, including plain numbers, are still rejected.</summary>
	/// <param name="input">A value that must not be taken for a date.</param>
	[Theory]
	[InlineData("2026")]
	[InlineData("2026-03")]
	[InlineData("12")]
	[InlineData("not a date")]
	[InlineData("")]
	[InlineData("31/02/2024")]
	public void ParseDateTime_NonDates_AreRejected(string input)
	{
		Filter.ParseDateTime(input).Should().BeNull();
	}

	/// <summary>A US-style date without a time zone parses as month, day, year (#197).</summary>
	/// <param name="input">The filter value.</param>
	/// <param name="expectedFormat">The format expected to match.</param>
	/// <param name="expectedPrecision">The precision expected to be detected.</param>
	/// <param name="expectedWallClock">The expected value, as an invariant <c>yyyy-MM-ddTHH:mm:ss.fff</c> string.</param>
	[Theory]
	[InlineData("03/14/2026", "MM'/'dd'/'yyyy", DatePrecision.Day, "2026-03-14T00:00:00.000")]
	[InlineData("05/24/2024", "MM'/'dd'/'yyyy", DatePrecision.Day, "2024-05-24T00:00:00.000")]
	[InlineData("03/14/2026 09:30", "MM'/'dd'/'yyyy HH:m", DatePrecision.Minute, "2026-03-14T09:30:00.000")]
	[InlineData("03/14/2026 09:30:15", "MM'/'dd'/'yyyy HH:mm:ss", DatePrecision.Second, "2026-03-14T09:30:15.000")]
	[InlineData("03/14/2026 09:30.123", "MM'/'dd'/'yyyy HH:m.fff", DatePrecision.Millisecond, "2026-03-14T09:30:00.123")]
	[InlineData("03/14/2026 09:30:15.123", "MM'/'dd'/'yyyy HH:mm:ss.fff", DatePrecision.Millisecond, "2026-03-14T09:30:15.123")]
	public void ParseDateTime_UsDateWithoutTimeZone_Parses(string input, string expectedFormat, DatePrecision expectedPrecision, string expectedWallClock)
	{
		var parsed = Filter.ParseDateTime(input);

		parsed.Should().NotBeNull();
		parsed!.Format.Should().Be(expectedFormat);
		parsed.Precision.Should().Be(expectedPrecision);
		parsed.Value.Should().Be(Expected(expectedWallClock, null));
	}

	/// <summary>A date with only an hour is detected at hour precision (#197).</summary>
	[Fact]
	public void ParseDateTime_DateAndHour_HasHourPrecision()
	{
		var parsed = Filter.ParseDateTime("2026-03-14 09");

		parsed.Should().NotBeNull();
		parsed!.Format.Should().Be("yyyy'-'MM'-'dd HH");
		parsed.Precision.Should().Be(DatePrecision.Hour);
		parsed.Value.Should().Be(new DateTime(2026, 3, 14, 9, 0, 0, DateTimeKind.Unspecified));
	}

	/// <summary>A year, or a year and month, is recognised only when asked for, at year or month precision (#197).</summary>
	/// <param name="input">The filter value.</param>
	/// <param name="expectedFormat">The format expected to match.</param>
	/// <param name="expectedPrecision">The precision expected to be detected.</param>
	/// <param name="year">The expected year.</param>
	/// <param name="month">The expected month.</param>
	[Theory]
	[InlineData("2026", "yyyy", DatePrecision.Year, 2026, 1)]
	[InlineData("2026-03", "yyyy'-'MM", DatePrecision.Month, 2026, 3)]
	public void IsDateTime_YearAndMonth_RecognisedWhenRequested(string input, string expectedFormat, DatePrecision expectedPrecision, int year, int month)
	{
		Filter.IsDateTime(input, true, out var dateTime, out var format, out var precision).Should().BeTrue();

		format.Should().Be(expectedFormat);
		precision.Should().Be(expectedPrecision);
		dateTime.Should().Be(new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified));

		Filter.IsDateTime(input, false, out _, out _, out _).Should().BeFalse();
	}

	/// <summary>Asking for year and month formats does not change how any other string parses.</summary>
	[Fact]
	public void IsDateTime_YearAndMonthRequested_OtherStringsUnchanged()
	{
		foreach (var input in new[] { "2024-05-15", "15/05/2024 09:30", "2024-05-15 09:30:15 +02:00", "03/14/2026", "2024-05-15T09:30:15Z", "2026-03-14 09" })
		{
			var expected = Filter.ParseDateTime(input);
			Filter.IsDateTime(input, true, out var actual, out var format, out var precision).Should().BeTrue();

			expected.Should().NotBeNull();
			actual.Should().Be(expected!.Value);
			format.Should().Be(expected.Format);
			precision.Should().Be(expected.Precision);
		}
	}

	private static DateTime Expected(string wallClock, int? offsetMinutes)
	{
		var value = DateTime.ParseExact(wallClock, "yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
		return offsetMinutes is null
			? value
			: new DateTimeOffset(value, TimeSpan.FromMinutes(offsetMinutes.Value)).LocalDateTime;
	}
}
