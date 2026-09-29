using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="TimelineDataPoint"/>.</summary>
public class TimelineDataPointTests
{
	/// <summary>The parameterless constructor gives an empty point.</summary>
	[Fact]
	public void DefaultConstructor_IsEmpty()
	{
		var point = new TimelineDataPoint();

		point.DateTime.Should().Be(default);
		point.Series.Should().Be(0);
		point.Value.Should().Be(0);
	}

	/// <summary>The value constructor captures the time, series and value, which remain settable.</summary>
	[Fact]
	public void ValueConstructor_CapturesValues()
	{
		var when = new DateTime(2024, 3, 4, 5, 6, 7, DateTimeKind.Utc);

		var point = new TimelineDataPoint(when, 2, 9.5);
		point.DateTime.Should().Be(when);
		point.Series.Should().Be(2);
		point.Value.Should().Be(9.5);

		point.Series = 3;
		point.Value = 1;
		point.DateTime = when.AddDays(1);

		point.Series.Should().Be(3);
		point.Value.Should().Be(1);
		point.DateTime.Should().Be(when.AddDays(1));
	}
}
