namespace PanoramicData.Blazor.Helpers;

/// <summary>
/// Shared default values for <see cref="PDCalendar{TItem}"/>, kept outside the generic type so that one copy
/// serves every item type.
/// </summary>
internal static class CalendarDefaults
{
	/// <summary>
	/// Gets the days treated as the weekend unless the host says otherwise.
	/// </summary>
	public static IReadOnlyCollection<DayOfWeek> WeekendDays { get; } = [DayOfWeek.Saturday, DayOfWeek.Sunday];

	/// <summary>
	/// Gets every view, in the order the view switcher offers them.
	/// </summary>
	public static IReadOnlyList<CalendarView> AllViews { get; } = [CalendarView.Month, CalendarView.Week, CalendarView.Agenda];

	/// <summary>
	/// Gets the length of a day with no daylight saving change.
	/// </summary>
	public static TimeSpan StandardDay { get; } = TimeSpan.FromHours(24);
}
