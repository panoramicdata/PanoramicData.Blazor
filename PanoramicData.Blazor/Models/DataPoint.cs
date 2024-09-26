namespace PanoramicData.Blazor.Models;

public class DataPoint
{
	public int Count { get; set; }
	internal int PeriodIndex { get; set; }
	public double[] SeriesValues { get; set; } = [];
	public DateTime StartTime { get; set; }

	#region Class members
	public static string CountLabel { get; set; } = "Count";

	#endregion
}
