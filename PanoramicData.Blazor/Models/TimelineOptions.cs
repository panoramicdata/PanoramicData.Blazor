using System;

namespace PanoramicData.Blazor.Models
{
	public class TimelineOptions
	{
		public TimelineBarOptions Bar { get; set; } = new TimelineBarOptions();
		public TimelineColours Colours { get; set; } = new TimelineColours();
		public TimelineSeries[] Series { get; set; } = Array.Empty<TimelineSeries>();
		public TimelineSpinnerOptions Spinner { get; set; } = new TimelineSpinnerOptions();
		public TimelineYAxisOptions YAxis { get; set; } = new TimelineYAxisOptions();
	}

	public class TimelineBarOptions
	{
		public int Padding { get; set; } = 2;
		public int Width { get; set; } = 20;
	}

	public class TimelineSpinnerOptions
	{
		public int ArcStart { get; set; } = 15;
		public int ArcEnd { get; set; } = 345;
		public int Width { get; set; } = 6;
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
		public string PendingBackground { get; set; } = "#0000002e";
		public string SelectionBackground { get; set; } = "#04a0b563";
		public string SelectionBorder { get; set; } = "#04a0b5";
		public string Spinner { get; set; } = "DarkGray";
	}

	public class TimelineSeries
	{
		public string Colour { get; set; } = "Green";
		public string Format { get; set; } = "0,0";
		public string Label { get; set; } = "Series";
	}
}
