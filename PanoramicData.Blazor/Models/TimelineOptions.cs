using System;

namespace PanoramicData.Blazor.Models
{
	public class TimelineOptions
	{
		public const string MAIN_COLOUR = "#404040d1";

		public TimelineBarOptions Bar { get; set; } = new TimelineBarOptions();
		public TimelineGeneralOptions General { get; set; } = new TimelineGeneralOptions();
		public TimelineIndicatorOptions Indicator { get; set; } = new TimelineIndicatorOptions();
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

	public class TimelineGeneralOptions
	{
		public bool AllowDisableSelection { get; set; }

		public bool AutoRefresh { get; set; } = true;

		public string DateFormat { get; set; } = "dd/MM/yy";

		public bool FetchAll { get; set; }

		public bool RestrictZoomOut { get; set; }

		public TimelineScale[] Scales { get; set; } = new[]
		{
			TimelineScale.Minutes,
			TimelineScale.Hours,
			TimelineScale.Hours4,
			TimelineScale.Hours6,
			TimelineScale.Hours8,
			TimelineScale.Hours12,
			TimelineScale.Days,
			TimelineScale.Weeks,
			TimelineScale.Months,
			TimelineScale.Years
		};
	}

	public class TimelineIndicatorOptions
	{
		public string BackgroundColour { get; set; } = TimelineOptions.MAIN_COLOUR;
		public string Colour { get; set; } = "whitesmoke";
		public int Padding { get; set; } = 5;
		public int Width { get; set; } = 20;
	}

	public class TimelineSelectionOptions
	{
		public string BackgroundColour { get; set; } = "#04a0b563";
		public string BorderColour { get; set; } = "#04a0b5";
		public bool CanChangeEnd { get; set; } = true;
		public bool CanChangeStart { get; set; } = true;
		public bool Enabled { get; set; } = true;
		public string HandleColour { get; set; } = "#7878789e";
		public int HandleWidth { get; set; } = 4;

	}

	public class TimelineSpinnerOptions
	{
		public int ArcStart { get; set; } = 15;
		public int ArcEnd { get; set; } = 345;
		public string Colour { get; set; } = TimelineOptions.MAIN_COLOUR;
		public int Width { get; set; } = 6;
	}

	public class TimelinePanOptions
	{
		public string BorderColour { get; set; } = "#91919142";
		public int BorderWidth { get; set; } = 5;
		public string Colour { get; set; } = TimelineOptions.MAIN_COLOUR;
		public int Height { get; set; } = 20;
	}

	public class TimelineXAxisOptions
	{
		public string Colour { get; set; } = "Black";
		public string MajorTickColour { get; set; } = "#3e3e3e";
		public string MinorTickColour { get; set; } = "#838383";

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
