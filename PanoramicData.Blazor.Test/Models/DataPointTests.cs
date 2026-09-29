using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="DataPoint"/>.</summary>
public class DataPointTests
{
	/// <summary>A new data point is empty.</summary>
	[Fact]
	public void New_IsEmpty()
	{
		var point = new DataPoint();

		point.Count.Should().Be(0);
		point.PeriodIndex.Should().Be(0);
		point.SeriesValues.Should().BeEmpty();
		point.StartTime.Should().Be(default);
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var start = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

		var point = new DataPoint { Count = 3, PeriodIndex = 7, SeriesValues = [1.5, 2.5], StartTime = start };

		point.Count.Should().Be(3);
		point.PeriodIndex.Should().Be(7);
		point.SeriesValues.Should().Equal(1.5, 2.5);
		point.StartTime.Should().Be(start);
	}

	/// <summary>The shared count label defaults to "Count".</summary>
	/// <remarks>Not changed here: it is process-wide state, and tests in other classes run in parallel.</remarks>
	[Fact]
	public void CountLabel_DefaultsToCount()
	{
		DataPoint.CountLabel.Should().Be("Count");
	}
}
