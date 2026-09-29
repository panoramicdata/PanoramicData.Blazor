using AwesomeAssertions;
using PanoramicData.Blazor.Models;
using System.Globalization;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="TimeRange"/>.</summary>
public class TimeRangeTests
{
	/// <summary>A new range covers today, from midnight to the following midnight.</summary>
	[Fact]
	public void New_CoversToday()
	{
		var range = new TimeRange();

		range.StartTime.Should().Be(DateTime.Today);
		range.EndTime.Should().Be(DateTime.Today.AddDays(1));
	}

	/// <summary>The range describes itself as its start and end in the general short format, joined by a dash.</summary>
	[Fact]
	public void ToString_ShowsStartAndEnd()
	{
		var start = new DateTime(2024, 1, 2, 3, 4, 0, DateTimeKind.Local);
		var end = new DateTime(2024, 1, 3, 5, 6, 0, DateTimeKind.Local);

		var range = new TimeRange { StartTime = start, EndTime = end };

		range.ToString().Should().Be($"{start.ToString("g", CultureInfo.CurrentCulture)} - {end.ToString("g", CultureInfo.CurrentCulture)}");
	}
}
