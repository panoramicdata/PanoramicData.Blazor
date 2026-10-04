using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Format tests for <see cref="Filter"/>: DateTime formatting for filter application, and enum display names.
/// </summary>
public partial class FilterTests
{
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
}
