namespace PanoramicData.Blazor.Models;

/// <summary>
/// One hour of elapsed time within a calendar day. A normal day has 24 slots; the day the clocks go
/// forward has fewer and the day they go back has more, because slots follow real elapsed time.
/// </summary>
/// <param name="Start">The instant the slot starts.</param>
/// <param name="End">The instant the slot ends. Usually one hour after the start, but the last slot of the day stops at midnight.</param>
/// <param name="LocalStart">The wall clock time at which the slot starts, in the calendar's time zone.</param>
/// <param name="Occurrence">How many earlier slots on the same day share this slot's local hour: 0 normally, 1 for the repeated hour when the clocks go back.</param>
public sealed record CalendarHourSlot(
	DateTimeOffset Start,
	DateTimeOffset End,
	DateTimeOffset LocalStart,
	int Occurrence)
{
	/// <summary>
	/// Gets the local hour of the day, from 0 to 23, at which the slot starts.
	/// </summary>
	public int Hour => LocalStart.Hour;

	/// <summary>
	/// Gets the UTC offset in force at the start of the slot.
	/// </summary>
	public TimeSpan Offset => LocalStart.Offset;

	/// <summary>
	/// Gets the length of the slot.
	/// </summary>
	public TimeSpan Duration => End - Start;
}
