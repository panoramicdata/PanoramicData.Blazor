using PanoramicData.Blazor.Extensions;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>Tests for the DateTimeExtensions extension methods.</summary>
public class DateTimeExtensionsTests
{
    /// <summary>Verifies that TotalMonthsSince returns the correct number of complete months between two dates.</summary>
    [Theory]
    [InlineData("2020-01-01", "2020-01-31", 0)]   // same month
    [InlineData("2020-01-01", "2020-02-01", 1)]   // exactly one month
    [InlineData("2020-01-01", "2020-03-15", 3)]   // steps: Jan->Feb->Mar->Apr(>Mar15) = 3
    [InlineData("2020-01-01", "2021-01-01", 12)]  // one year
    [InlineData("2020-01-15", "2020-04-10", 3)]   // steps: Jan15->Feb15->Mar15->Apr15(>Apr10) = 3
    public void WhenCalculatingTotalMonthsSinceThenReturnsExpected(
        string startStr, string endStr, int expected)
    {
        var start = DateTime.Parse(startStr);
        var end = DateTime.Parse(endStr);

        end.TotalMonthsSince(start).ShouldBe(expected);
    }

    /// <summary>Verifies that TotalMonthsSince returns zero when the end date is before the start date.</summary>
    [Fact]
    public void WhenEndIsBeforeStartThenTotalMonthsSinceReturnsZero()
    {
        var start = new DateTime(2020, 6, 1);
        var end = new DateTime(2020, 1, 1);

        end.TotalMonthsSince(start).ShouldBe(0);
    }

    /// <summary>Verifies that TotalYearsSince returns the correct number of complete years between two dates.</summary>
    [Theory]
    [InlineData("2020-01-01", "2020-12-31", 0)]   // same year
    [InlineData("2020-01-01", "2021-01-01", 1)]   // exactly one year
    [InlineData("2020-01-01", "2023-06-15", 4)]   // steps: 2020->2021->2022->2023->2024(>Jun15) = 4
    [InlineData("2019-06-15", "2020-01-01", 1)]   // steps: Jun2019->Jun2020(>Jan2020) = 1
    public void WhenCalculatingTotalYearsSinceThenReturnsExpected(
        string startStr, string endStr, int expected)
    {
        var start = DateTime.Parse(startStr);
        var end = DateTime.Parse(endStr);

        end.TotalYearsSince(start).ShouldBe(expected);
    }

    /// <summary>Verifies that TotalYearsSince returns zero when the end date is before the start date.</summary>
    [Fact]
    public void WhenEndIsBeforeStartThenTotalYearsSinceReturnsZero()
    {
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2020, 1, 1);

        end.TotalYearsSince(start).ShouldBe(0);
    }

    /// <summary>Verifies that TotalMonthsSince returns zero when both dates are the same.</summary>
    [Fact]
    public void WhenSameDateThenTotalMonthsSinceReturnsZero()
    {
        var date = new DateTime(2020, 6, 15);

        date.TotalMonthsSince(date).ShouldBe(0);
    }

    /// <summary>Verifies that TotalYearsSince returns zero when both dates are the same.</summary>
    [Fact]
    public void WhenSameDateThenTotalYearsSinceReturnsZero()
    {
        var date = new DateTime(2020, 6, 15);

        date.TotalYearsSince(date).ShouldBe(0);
    }
}
