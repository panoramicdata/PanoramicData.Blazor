using System;

namespace PanoramicData.Blazor.Models
{
	public class DataPoint
	{
		internal int PeriodIndex { get; set; }
		public double[] SeriesValues { get; set; } = Array.Empty<double>();
		public DateTime StartTime { get; set; }
	}
}
