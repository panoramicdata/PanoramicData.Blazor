namespace PanoramicData.Blazor.Enums;

/// <summary>
/// The layouts in which a <see cref="PDCalendar{TItem}"/> can display its items.
/// </summary>
public enum CalendarView
{
	/// <summary>
	/// A grid of whole weeks covering the focused month, with a cell per day.
	/// </summary>
	Month,

	/// <summary>
	/// Seven day columns against an hour axis, with items positioned by time.
	/// </summary>
	Week,

	/// <summary>
	/// A list of items grouped by day, starting at the focused date. Recommended for narrow screens.
	/// </summary>
	Agenda
}
