using System;

namespace PanoramicData.Blazor.Timeline
{
	public class DataPoint
	{
		public int PeriodIndex { get; set; }
		public double[] SeriesValues { get; set; } = Array.Empty<double>();
		public DateTime StartTime { get; set; }
	}
}
