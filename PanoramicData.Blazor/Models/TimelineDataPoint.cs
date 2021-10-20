using System;

namespace PanoramicData.Blazor.Models
{
	public class TimelineDataPoint
	{
		public TimelineDataPoint()
		{
		}

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
}
