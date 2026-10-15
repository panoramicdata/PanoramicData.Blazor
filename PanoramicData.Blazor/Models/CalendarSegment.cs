namespace PanoramicData.Blazor.Models;

/// <summary>
/// The part of a calendar item that falls on one day. An item that spans midnight produces one
/// segment for each day it touches.
/// </summary>
/// <typeparam name="TItem">The type of the calendar item.</typeparam>
/// <param name="Item">The item this segment belongs to.</param>
/// <param name="Date">The day, in the calendar's time zone, that this segment falls on.</param>
/// <param name="Start">The start of the segment, which is the later of the item start and the start of the day.</param>
/// <param name="End">The end of the segment, which is the earlier of the item end and the end of the day.</param>
/// <param name="ContinuesBefore">True when the item started on an earlier day.</param>
/// <param name="ContinuesAfter">True when the item carries on into a later day.</param>
public sealed record CalendarSegment<TItem>(
	TItem Item,
	DateOnly Date,
	DateTimeOffset Start,
	DateTimeOffset End,
	bool ContinuesBefore,
	bool ContinuesAfter)
{
	/// <summary>
	/// Gets the zero-based column the segment occupies among the segments it overlaps in time.
	/// </summary>
	public int Column { get; init; }

	/// <summary>
	/// Gets the number of columns shared by the group of mutually overlapping segments this segment belongs to.
	/// </summary>
	public int ColumnCount { get; init; } = 1;

	/// <summary>
	/// Gets a value indicating whether the item covers more than this one day.
	/// </summary>
	public bool IsMultiDay => ContinuesBefore || ContinuesAfter;
}
