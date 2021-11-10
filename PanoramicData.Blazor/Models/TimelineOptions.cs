using System;

namespace PanoramicData.Blazor.Models
{
	public class TimelineOptions
	{
		public TimelineBarOptions Bar { get; set; } = new TimelineBarOptions();
		public TimelineColours Colours { get; set; } = new TimelineColours();
		public TimelineSeries[] Series { get; set; } = Array.Empty<TimelineSeries>();
		public TimelineYAxisOptions YAxis { get; set; } = new TimelineYAxisOptions();
	}

	public class TimelineBarOptions
	{
		public int Padding { get; set; } = 2;
		public int Width { get; set; } = 20;
	}

	public class TimelineYAxisOptions
	{
		public double? MaxValue { get; set; }
	}

	public class TimelineColours
	{
		public string Background { get; set; } = "White";
		public string Border { get; set; } = "Silver";
		public string Foreground { get; set; } = "Black";
		public string HandleBackground { get; set; } = "Silver";
		public string MajorTick { get; set; } = "Black";
		public string MinorTick { get; set; } = "Silver";
	}

	public class TimelineSeries
	{
		public string Colour { get; set; } = "Green";
		public string Format { get; set; } = "0,0";
		public string Label { get; set; } = "Series";
	}
}
