namespace PanoramicData.Blazor.Models;

internal static class TimelineFollowNowCalculator
{
	internal static TimeRange CreateRollingSelection(DateTime end, TimeSpan duration, DateTime minimum)
	{
		var start = end - duration;
		if (start < minimum)
		{
			start = minimum;
		}

		return new TimeRange
		{
			StartTime = start,
			EndTime = end
		};
	}
}
