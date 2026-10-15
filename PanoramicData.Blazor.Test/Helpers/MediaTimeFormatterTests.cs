using AwesomeAssertions;
using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>Tests for <see cref="MediaTimeFormatter"/>.</summary>
public class MediaTimeFormatterTests
{
	/// <summary>Times format as m:ss, or h:mm:ss from an hour or when hours are asked for; fractions are dropped.</summary>
	[Theory]
	[InlineData(0, false, "0:00")]
	[InlineData(5.9, false, "0:05")]
	[InlineData(65, false, "1:05")]
	[InlineData(3599, false, "59:59")]
	[InlineData(3600, false, "1:00:00")]
	[InlineData(3723, false, "1:02:03")]
	[InlineData(65, true, "0:01:05")]
	[InlineData(-10, false, "0:00")]
	[InlineData(36000, false, "10:00:00")]
	public void Format_UsesHoursOnlyWhenNeeded(double seconds, bool includeHours, string expected)
		=> MediaTimeFormatter.Format(TimeSpan.FromSeconds(seconds), includeHours).Should().Be(expected);

	/// <summary>A position is shown with hours when the duration it belongs to has them, so the two line up.</summary>
	[Fact]
	public void Format_WithDuration_MatchesTheDurationLayout()
	{
		MediaTimeFormatter.Format(TimeSpan.FromSeconds(65), TimeSpan.FromHours(2)).Should().Be("0:01:05");
		MediaTimeFormatter.Format(TimeSpan.FromSeconds(65), TimeSpan.FromMinutes(10)).Should().Be("1:05");
		MediaTimeFormatter.Format(TimeSpan.FromSeconds(65), (TimeSpan?)null).Should().Be("1:05");
	}

	/// <summary>Missing, not-a-number and infinite seconds give null; negative seconds give zero.</summary>
	[Fact]
	public void FromSeconds_HandlesValuesTheBrowserCanReport()
	{
		MediaTimeFormatter.FromSeconds(null).Should().BeNull();
		MediaTimeFormatter.FromSeconds(double.NaN).Should().BeNull();
		MediaTimeFormatter.FromSeconds(double.PositiveInfinity).Should().BeNull();
		MediaTimeFormatter.FromSeconds(-3).Should().Be(TimeSpan.Zero);
		MediaTimeFormatter.FromSeconds(12.5).Should().Be(TimeSpan.FromSeconds(12.5));
		MediaTimeFormatter.FromSeconds(double.MaxValue).Should().NotBeNull();
	}

	/// <summary>Spoken text names each non-zero unit with the right plural, and says zero seconds for zero.</summary>
	[Theory]
	[InlineData(0, "0 seconds")]
	[InlineData(-5, "0 seconds")]
	[InlineData(1, "1 second")]
	[InlineData(60, "1 minute")]
	[InlineData(3723, "1 hour 2 minutes 3 seconds")]
	[InlineData(7200, "2 hours")]
	[InlineData(3601, "1 hour 1 second")]
	public void ToSpokenText_DescribesTheTime(double seconds, string expected)
		=> MediaTimeFormatter.ToSpokenText(TimeSpan.FromSeconds(seconds)).Should().Be(expected);
}
