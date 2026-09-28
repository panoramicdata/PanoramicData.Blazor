using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="TimelineSeries"/>.</summary>
public class TimelineSeriesTests
{
	/// <summary>A new series is a green series labelled "Series" with thousands formatting, and its members round-trip.</summary>
	[Fact]
	public void Members_DefaultAndRoundTrip()
	{
		var series = new TimelineSeries();
		series.Colour.Should().Be("Green");
		series.Format.Should().Be("0,0");
		series.Label.Should().Be("Series");

		series.Colour = "Red";
		series.Format = "0.00";
		series.Label = "Errors";

		series.Colour.Should().Be("Red");
		series.Format.Should().Be("0.00");
		series.Label.Should().Be("Errors");
	}
}
