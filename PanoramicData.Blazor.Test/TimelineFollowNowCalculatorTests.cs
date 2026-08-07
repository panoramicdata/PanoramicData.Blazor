using PanoramicData.Blazor.Models;
using Shouldly;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for live timeline selection calculations.
/// </summary>
public class TimelineFollowNowCalculatorTests
{
	/// <summary>
	/// Verifies that existing consumers retain static timeline behaviour unless live following is requested.
	/// </summary>
	[Fact]
	public void Timeline_DefaultsToBackwardCompatibleStaticMode()
	{
		var timeline = new PDTimeline();

		timeline.FollowNow.ShouldBeFalse();
		timeline.IsFollowingNow.ShouldBeFalse();
		timeline.FollowNowSelection.ShouldBeTrue();
		timeline.FollowNowRefreshInterval.ShouldBe(TimeSpan.FromSeconds(1));
	}

	/// <summary>
	/// Verifies that a live selection retains its duration and ends at the new boundary.
	/// </summary>
	[Fact]
	public void CreateRollingSelection_PreservesDurationAtNewBoundary()
	{
		var boundary = new DateTime(2026, 8, 7, 17, 0, 1);

		var result = TimelineFollowNowCalculator.CreateRollingSelection(
			boundary,
			TimeSpan.FromSeconds(15),
			boundary.AddMinutes(-2));

		result.StartTime.ShouldBe(boundary.AddSeconds(-15));
		result.EndTime.ShouldBe(boundary);
	}

	/// <summary>
	/// Verifies that the rolling selection cannot extend before the timeline minimum.
	/// </summary>
	[Fact]
	public void CreateRollingSelection_ClampsToMinimum()
	{
		var minimum = new DateTime(2026, 8, 7, 17, 0, 0);
		var boundary = minimum.AddSeconds(5);

		var result = TimelineFollowNowCalculator.CreateRollingSelection(
			boundary,
			TimeSpan.FromSeconds(15),
			minimum);

		result.StartTime.ShouldBe(minimum);
		result.EndTime.ShouldBe(boundary);
	}
}
