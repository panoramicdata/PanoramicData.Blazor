using System;

namespace PanoramicData.Blazor.Models
{
	public class TimelineOptions
	{
		public TimelineBarOptions Bar { get; set; } = new TimelineBarOptions();
		public TimelinePanOptions Pan { get; set; } = new TimelinePanOptions();
		public TimelineSelectionOptions Selection { get; set; }	= new TimelineSelectionOptions();
		public TimelineSeries[] Series { get; set; } = Array.Empty<TimelineSeries>();
		public TimelineSpinnerOptions Spinner { get; set; } = new TimelineSpinnerOptions();
		public TimelineXAxisOptions XAxis { get; set; } = new TimelineXAxisOptions();
		public TimelineYAxisOptions YAxis { get; set; } = new TimelineYAxisOptions();
	}

	public class TimelineBarOptions
	{
		public int Padding { get; set; } = 2;
		public int Width { get; set; } = 20;
	}

	public class TimelineSelectionOptions
	{
		public string BackgroundColour { get; set; } = "#04a0b563";
		public string BorderColour { get; set; } = "#04a0b5";
		public bool Enabled { get; set; } = true;
		public string HandleColour { get; set; } = "#7878789e";
		public int HandleWidth { get; set; } = 4;

	}

	public class TimelineSpinnerOptions
	{
		public int ArcStart { get; set; } = 15;
		public int ArcEnd { get; set; } = 345;
		public string Colour { get; set; } = "DarkGray";
		public int Width { get; set; } = 6;
	}

	public class TimelinePanOptions
	{
		public string Colour { get; set; } = "Silver";
		public int Height { get; set; } = 20;
	}

	public class TimelineXAxisOptions
	{
		public string MajorTickColour { get; set; } = "Black";
		public string MinorTickColour { get; set; } = "Silver";

	}

	public class TimelineYAxisOptions
	{
		public double? MaxValue { get; set; }
	}

	public class TimelineSeries
	{
		public string Colour { get; set; } = "Green";
		public string Format { get; set; } = "0,0";
		public string Label { get; set; } = "Series";
	}
}
