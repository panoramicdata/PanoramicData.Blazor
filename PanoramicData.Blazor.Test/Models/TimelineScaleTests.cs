using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="TimelineScale"/>.</summary>
public class TimelineScaleTests
{
	// Wednesday 15 May 2024, 13:47:38.456
	private static readonly DateTime _when = new(2024, 5, 15, 13, 47, 38, 456, DateTimeKind.Unspecified);

	private static TimelineScale Scale(TimelineUnits unit, int count = 1) => new(unit.ToString(), unit, count);

	/// <summary>Units longer than an hour cannot be grouped into multiples.</summary>
	[Theory]
	[InlineData(TimelineUnits.Days)]
	[InlineData(TimelineUnits.Weeks)]
	[InlineData(TimelineUnits.Months)]
	[InlineData(TimelineUnits.Years)]
	public void Constructor_MultipleOfLongUnit_Throws(TimelineUnits unit)
	{
		var act = () => new TimelineScale("x", unit, 2);

		act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unitCount");
	}

	/// <summary>The constructor records the name, unit and count, and the scale describes itself by name.</summary>
	[Fact]
	public void Constructor_RecordsValues()
	{
		var scale = new TimelineScale("Quarter hours", TimelineUnits.Minutes, 15);

		scale.Name.Should().Be("Quarter hours");
		scale.UnitType.Should().Be(TimelineUnits.Minutes);
		scale.UnitCount.Should().Be(15);
		scale.ToString().Should().Be("Quarter hours");
		scale.CalendarWeekRule.Should().Be(System.Globalization.CalendarWeekRule.FirstDay);
		scale.CalendarDayOfWeek.Should().Be(DayOfWeek.Sunday);
	}

	/// <summary>Adding periods moves by the unit times the count times the number of periods.</summary>
	[Theory]
	[InlineData(TimelineUnits.Milliseconds, 100, 2, "2024-05-15 13:47:38.656")]
	[InlineData(TimelineUnits.Seconds, 10, 3, "2024-05-15 13:48:08.456")]
	[InlineData(TimelineUnits.Minutes, 15, 1, "2024-05-15 14:02:38.456")]
	[InlineData(TimelineUnits.Hours, 4, -1, "2024-05-15 09:47:38.456")]
	[InlineData(TimelineUnits.Days, 1, 20, "2024-06-04 13:47:38.456")]
	[InlineData(TimelineUnits.Weeks, 1, 2, "2024-05-29 13:47:38.456")]
	[InlineData(TimelineUnits.Months, 1, 9, "2025-02-15 13:47:38.456")]
	[InlineData(TimelineUnits.Years, 1, -4, "2020-05-15 13:47:38.456")]
	public void AddPeriods_MovesByPeriods(TimelineUnits unit, int count, int periods, string expected)
	{
		Scale(unit, count).AddPeriods(_when, periods).ToString("yyyy-MM-dd HH:mm:ss.fff").Should().Be(expected);
	}

	/// <summary>Adding no periods, or adding to the minimum date, returns the date unchanged.</summary>
	[Fact]
	public void AddPeriods_ZeroOrMinValue_IsUnchanged()
	{
		Scale(TimelineUnits.Days).AddPeriods(_when, 0).Should().Be(_when);
		Scale(TimelineUnits.Days).AddPeriods(DateTime.MinValue, 5).Should().Be(DateTime.MinValue);
	}

	/// <summary>The format pattern suits the unit, built on the default or a supplied date format.</summary>
	[Theory]
	[InlineData(TimelineUnits.Years, "yyyy", "yyyy")]
	[InlineData(TimelineUnits.Months, "MMM yyyy", "MMM yyyy")]
	[InlineData(TimelineUnits.Hours, "d HH:00", "dd/MM HH:00")]
	[InlineData(TimelineUnits.Minutes, "d HH:mm", "dd/MM HH:mm")]
	[InlineData(TimelineUnits.Days, "d", "dd/MM")]
	[InlineData(TimelineUnits.Seconds, "d", "dd/MM")]
	public void FormatPattern_SuitsUnit(TimelineUnits unit, string defaultPattern, string customPattern)
	{
		var scale = Scale(unit);

		scale.FormatPattern().Should().Be(defaultPattern);
		scale.FormatPattern("dd/MM").Should().Be(customPattern);
	}

	/// <summary>Major ticks fall at the start of the next larger unit.</summary>
	[Theory]
	[InlineData(TimelineUnits.Years, 1, "2024-06-01 05:00", true)]
	[InlineData(TimelineUnits.Years, 1, "2023-01-01 00:00", false)]
	[InlineData(TimelineUnits.Months, 1, "2024-01-20 00:00", true)]
	[InlineData(TimelineUnits.Months, 1, "2024-02-01 00:00", false)]
	[InlineData(TimelineUnits.Weeks, 1, "2024-01-07 00:00", true)]
	[InlineData(TimelineUnits.Weeks, 1, "2024-01-08 00:00", false)]
	[InlineData(TimelineUnits.Days, 1, "2024-03-01 12:00", true)]
	[InlineData(TimelineUnits.Days, 1, "2024-03-02 00:00", false)]
	[InlineData(TimelineUnits.Hours, 4, "2024-03-02 00:00", true)]
	[InlineData(TimelineUnits.Hours, 4, "2024-03-02 01:00", false)]
	[InlineData(TimelineUnits.Hours, 12, "2024-01-02 00:00", true)]
	[InlineData(TimelineUnits.Hours, 12, "2024-01-01 00:00", false)]
	[InlineData(TimelineUnits.Minutes, 5, "2024-01-01 07:00", true)]
	[InlineData(TimelineUnits.Minutes, 5, "2024-01-01 07:05", false)]
	[InlineData(TimelineUnits.Milliseconds, 100, "2024-01-01 07:05", true)]
	public void IsMajorTick_AtStartOfLargerUnit(TimelineUnits unit, int count, string when, bool expected)
	{
		Scale(unit, count).IsMajorTick(DateTime.Parse(when, System.Globalization.CultureInfo.InvariantCulture)).Should().Be(expected);
	}

	/// <summary>Periods between two dates are counted in the scale's units, rounded up by default or down on request.</summary>
	[Theory]
	[InlineData(TimelineUnits.Milliseconds, 100, 250, 3, 2)]
	[InlineData(TimelineUnits.Seconds, 1, 1500, 2, 1)]
	[InlineData(TimelineUnits.Minutes, 15, 20 * 60_000, 2, 1)]
	[InlineData(TimelineUnits.Hours, 1, 90 * 60_000, 2, 1)]
	[InlineData(TimelineUnits.Days, 1, 36 * 3_600_000, 2, 1)]
	[InlineData(TimelineUnits.Weeks, 1, 10 * 86_400_000L, 2, 1)]
	public void PeriodsBetween_CountsUnits(TimelineUnits unit, int count, long elapsedMs, int roundedUp, int roundedDown)
	{
		var scale = Scale(unit, count);
		var end = _when.AddMilliseconds(elapsedMs);

		scale.PeriodsBetween(_when, end).Should().Be(roundedUp);
		scale.PeriodsBetween(_when, end, false).Should().Be(roundedDown);
	}

	/// <summary>Months and years between dates are counted as calendar boundaries crossed.</summary>
	[Fact]
	public void PeriodsBetween_MonthsAndYears_CountCalendarUnits()
	{
		Scale(TimelineUnits.Months).PeriodsBetween(_when, new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Unspecified)).Should().Be(3);
		Scale(TimelineUnits.Years).PeriodsBetween(_when, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)).Should().Be(2);
	}

	/// <summary>A period starts at the beginning of the unit, rounded down to a multiple of the count.</summary>
	[Theory]
	[InlineData(TimelineUnits.Milliseconds, 100, "2024-05-15 13:47:38.400")]
	[InlineData(TimelineUnits.Seconds, 10, "2024-05-15 13:47:30.000")]
	[InlineData(TimelineUnits.Minutes, 15, "2024-05-15 13:45:00.000")]
	[InlineData(TimelineUnits.Hours, 6, "2024-05-15 12:00:00.000")]
	[InlineData(TimelineUnits.Days, 1, "2024-05-15 00:00:00.000")]
	[InlineData(TimelineUnits.Weeks, 1, "2024-05-12 00:00:00.000")]
	[InlineData(TimelineUnits.Months, 1, "2024-05-01 00:00:00.000")]
	[InlineData(TimelineUnits.Years, 1, "2024-01-01 00:00:00.000")]
	public void PeriodStart_RoundsDown(TimelineUnits unit, int count, string expected)
	{
		Scale(unit, count).PeriodStart(_when).ToString("yyyy-MM-dd HH:mm:ss.fff").Should().Be(expected);
	}

	/// <summary>A period ends one scale step after it starts.</summary>
	[Theory]
	[InlineData(TimelineUnits.Milliseconds, 100, "2024-05-15 13:47:38.500")]
	[InlineData(TimelineUnits.Seconds, 10, "2024-05-15 13:47:40.000")]
	[InlineData(TimelineUnits.Minutes, 15, "2024-05-15 14:00:00.000")]
	[InlineData(TimelineUnits.Hours, 6, "2024-05-15 18:00:00.000")]
	[InlineData(TimelineUnits.Days, 1, "2024-05-16 00:00:00.000")]
	[InlineData(TimelineUnits.Weeks, 1, "2024-05-19 00:00:00.000")]
	[InlineData(TimelineUnits.Months, 1, "2024-06-01 00:00:00.000")]
	[InlineData(TimelineUnits.Years, 1, "2025-01-01 00:00:00.000")]
	public void PeriodEnd_IsOneStepAfterStart(TimelineUnits unit, int count, string expected)
	{
		Scale(unit, count).PeriodEnd(_when).ToString("yyyy-MM-dd HH:mm:ss.fff").Should().Be(expected);
	}

	/// <summary>Major tick labels show the next larger unit.</summary>
	[Theory]
	[InlineData(TimelineUnits.Milliseconds, "15/05 13:47:38")]
	[InlineData(TimelineUnits.Seconds, "15/05 13:47:38")]
	[InlineData(TimelineUnits.Minutes, "15/05 13:00")]
	[InlineData(TimelineUnits.Hours, "15/05")]
	[InlineData(TimelineUnits.Days, "2024-05")]
	[InlineData(TimelineUnits.Weeks, "2024")]
	[InlineData(TimelineUnits.Months, "2024")]
	[InlineData(TimelineUnits.Years, "2024")]
	public void TickLabelMajor_ShowsLargerUnit(TimelineUnits unit, string expected)
	{
		Scale(unit).TickLabelMajor(_when, "dd/MM").Should().Be(expected);
	}

	/// <summary>The default major tick label uses the invariant short date format.</summary>
	[Fact]
	public void TickLabelMajor_DefaultFormat()
	{
		Scale(TimelineUnits.Hours).TickLabelMajor(_when).Should().Be("05/15/2024");
	}

	/// <summary>Minor tick labels show the unit itself, and weeks show the week of the year.</summary>
	[Theory]
	[InlineData(TimelineUnits.Milliseconds, "456")]
	[InlineData(TimelineUnits.Seconds, "38")]
	[InlineData(TimelineUnits.Minutes, "47")]
	[InlineData(TimelineUnits.Hours, "13")]
	[InlineData(TimelineUnits.Days, "15")]
	[InlineData(TimelineUnits.Weeks, "20")]
	[InlineData(TimelineUnits.Months, "05")]
	[InlineData(TimelineUnits.Years, "24")]
	public void TickLabelMinor_ShowsUnit(TimelineUnits unit, string expected)
	{
		Scale(unit).TickLabelMinor(_when).Should().Be(expected);
	}

	/// <summary>Scales order by unit and then by count.</summary>
	[Fact]
	public void CompareTo_OrdersByUnitThenCount()
	{
		TimelineScale.Hours.CompareTo(TimelineScale.Hours).Should().Be(0);
		TimelineScale.Hours.CompareTo(TimelineScale.Days).Should().Be(-1);
		TimelineScale.Hours.CompareTo(TimelineScale.Hours4).Should().Be(-1);
		TimelineScale.Hours12.CompareTo(TimelineScale.Hours6).Should().Be(1);
		TimelineScale.Years.CompareTo(TimelineScale.Seconds).Should().Be(1);
	}

	/// <summary>Comparing with something other than a scale throws.</summary>
	[Fact]
	public void CompareTo_NonScale_Throws()
	{
		var act = () => TimelineScale.Days.CompareTo("Days");

		act.Should().Throw<ArgumentException>();
	}

	/// <summary>The predefined scales have the expected names, units and counts, in ascending order.</summary>
	[Fact]
	public void PredefinedScales_AreOrdered()
	{
		TimelineScale[] scales =
		[
			TimelineScale.Seconds, TimelineScale.Minutes, TimelineScale.Minutes5, TimelineScale.Minutes10, TimelineScale.Minutes15,
			TimelineScale.Hours, TimelineScale.Hours4, TimelineScale.Hours6, TimelineScale.Hours8, TimelineScale.Hours12,
			TimelineScale.Days, TimelineScale.Weeks, TimelineScale.Months, TimelineScale.Years
		];

		scales.Should().BeInAscendingOrder();
		scales.Select(s => s.Name).Should().Equal(
			"Seconds", "Minutes", "5 Minutes", "10 Minutes", "15 Minutes", "Hours", "4 Hours", "6 Hours", "8 Hours", "12 Hours",
			"Days", "Weeks", "Months", "Years");
	}
}
