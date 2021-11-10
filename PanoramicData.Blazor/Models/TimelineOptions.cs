using System;

namespace PanoramicData.Blazor.Models
{
	public class TimelineOptions
	{
		public bool RightAlign { get; set; }
		public TimelineBarOptions Bar { get; set; } = new TimelineBarOptions();
		public TimelineColours Colours { get; set; } = new TimelineColours();
		public TimelineSeries[] Series { get; set; } = Array.Empty<TimelineSeries>();
	}

	public class TimelineBarOptions
	{
		public int Padding { get; set; } = 2;
		public int Width { get; set; } = 20;
	}

	public class TimelineColours
	{
		public string Background { get; set; } = "White";
		public string Border { get; set; } = "Silver";
		public string Foreground { get; set; } = "Black";
		public string HandleBackground { get; set; } = "Silver";
	}

	public class TimelineSeries
	{
		public string Label { get; set; } = "Series";
		public string Colour { get; set; } = "Green";
	}
}
