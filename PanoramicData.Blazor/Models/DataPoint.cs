namespace PanoramicData.Blazor.Models;

/// <summary>
/// Represents a single aggregated data point for chart and timeline components.
/// </summary>
public class DataPoint
{
	/// <summary>Gets or sets the number of source records aggregated into this data point.</summary>
	public int Count { get; set; }
	internal int PeriodIndex { get; set; }
	/// <summary>Gets or sets the per-series values for this data point. Each element corresponds to a series defined on the chart.</summary>
	public double[] SeriesValues { get; set; } = [];
	/// <summary>Gets or sets the start time of the time period this data point represents.</summary>
	public DateTime StartTime { get; set; }

	#region Class members
	/// <summary>Gets or sets the display label for the count column.</summary>
	public static string CountLabel { get; set; } = "Count";

	#endregion
}
