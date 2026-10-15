namespace PanoramicData.Blazor.Test.Helpers;

/// <summary>
/// Time zones used by the calendar tests: a real zone with daylight saving, and custom zones whose
/// changes fall at awkward times, so that edge cases do not depend on any one machine's zone data.
/// </summary>
internal static class CalendarTestZones
{
	/// <summary>
	/// Gets the United Kingdom zone: clocks go forward at 01:00 on 29 March 2026 and back at 02:00 on 25 October 2026.
	/// </summary>
	public static TimeZoneInfo London { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

	/// <summary>
	/// Gets a zone at UTC-4 whose clocks go forward an hour at midnight on 6 September, so that midnight itself does not
	/// exist that day, and back at midnight on 5 April.
	/// </summary>
	public static TimeZoneInfo MidnightForward { get; } = CreateZone(
		"Test/MidnightForward",
		new DateTime(1, 1, 1, 0, 0, 0),
		new DateTime(1, 1, 1, 0, 0, 0));

	/// <summary>
	/// Gets a zone at UTC-4 whose clocks go forward an hour at 02:00 on 6 September and back at 01:00 on 5 April, so that
	/// midnight on 5 April occurs twice.
	/// </summary>
	public static TimeZoneInfo MidnightRepeated { get; } = CreateZone(
		"Test/MidnightRepeated",
		new DateTime(1, 1, 1, 2, 0, 0),
		new DateTime(1, 1, 1, 1, 0, 0));

	private static TimeZoneInfo CreateZone(string id, DateTime forwardTime, DateTime backTime)
	{
		var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
			new DateTime(2000, 1, 1),
			new DateTime(2099, 12, 31),
			TimeSpan.FromHours(1),
			TimeZoneInfo.TransitionTime.CreateFixedDateRule(forwardTime, 9, 6),
			TimeZoneInfo.TransitionTime.CreateFixedDateRule(backTime, 4, 5));

		return TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(-4), id, "Standard", "Daylight", [rule]);
	}
}
