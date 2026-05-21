namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a start and end date/time range.
/// </summary>
public class TimeRange
{
	/// <summary>
	/// Initializes a new range covering the current day.
	/// </summary>
	public TimeRange()
	{
		StartTime = DateTime.Today.Date;
		EndTime = StartTime.AddDays(1);
	}

	/// <summary>
	/// Gets or sets the start date/time.
	/// </summary>
	public DateTime StartTime { get; set; }

	/// <summary>
	/// Gets or sets the end date/time.
	/// </summary>
	public DateTime EndTime { get; set; }

	/// <summary>
	/// Returns a human-readable representation of the range.
	/// </summary>
	/// <returns>Formatted range string.</returns>
	public override string ToString() => $"{StartTime:g} - {EndTime:g}";
}
