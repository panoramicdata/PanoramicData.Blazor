namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a single value recorded at a specific date and time for a given data series in a <see cref="PanoramicData.Blazor.PDTimeline"/>.
/// </summary>
public class TimelineDataPoint
{
	/// <summary>
	/// Initializes a new instance of <see cref="TimelineDataPoint"/> with a default (zero) date and value.
	/// </summary>
	public TimelineDataPoint()
	{
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TimelineDataPoint"/> with the given values.
	/// </summary>
	/// <param name="dateTime">The date and time the data point occurred.</param>
	/// <param name="series">The zero-based series index.</param>
	/// <param name="value">The numeric value of the data point.</param>
	public TimelineDataPoint(DateTime dateTime, int series, double value)
	{
		DateTime = dateTime;
		Series = series;
		Value = value;
	}

	/// <summary>
	/// Gets or sets the date and time of the data point.
	/// </summary>
	public DateTime DateTime { get; set; }

	/// <summary>
	/// Gets or sets the index of the series the data point is for.
	/// </summary>
	/// <remarks>The index is zero-based.</remarks>
	public int Series { get; set; }

	/// <summary>
	/// Gets or sets the value of the data point.
	/// </summary>
	public double Value { get; set; }

}
