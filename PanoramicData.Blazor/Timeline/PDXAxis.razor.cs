using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
using System;

namespace PanoramicData.Blazor.Timeline
{
	public partial class PDXAxis
	{
		[Parameter]
		public DataPoint[] DataPoints { get; set; } = Array.Empty<DataPoint>();

		[Parameter]
		public double Height { get; set; } = 10;

		[Parameter]
		public TimelineOptions Options { get; set; } = new TimelineOptions();

		[Parameter]
		public TimelineScales Scale { get; set; } = TimelineScales.Days;

		[Parameter]
		public int TotalPeriods { get; set; }

		[Parameter]
		public double X { get; set; }

		[Parameter]
		public double Y { get; set; }

		private string CalculateViewBox()
		{
			var x = 0;
			var y = 0;
			var width = TotalPeriods * Options.Bar.Width;
			var height = 20;
			return $"{x} {y} {width} {height}";
		}

		private TextInfo GetTextInfo(DateTime dt)
		{
			if (Scale == TimelineScales.Years)
			{
				return new TextInfo
				{
					OffsetX = 4,
					Text = dt.ToString("yy")
				};
			}
			else if (Scale == TimelineScales.Months)
			{
				return new TextInfo
				{
					Skip = 1,
					Text = dt.ToString("MMM yy")
				};
			}
			else if (Scale == TimelineScales.Weeks)
			{
				return new TextInfo
				{
					Skip = 3,
					Text = dt.ToString("dd/MM/yy")
				};
			}
			else if (Scale == TimelineScales.Days)
			{
				return new TextInfo
				{
					Skip = 2,
					Text = dt.ToString("dd/MM/yy")
				};
			}
			else if (Scale == TimelineScales.Hours8)
			{
				return new TextInfo
				{
					Skip = 2,
					Text = dt.ToString("dd/MM/yy")
				};
			}
			else if (Scale == TimelineScales.Hours4)
			{
				return new TextInfo
				{
					Skip = 5,
					Text = dt.ToString("dd/MM/yy")
				};
			}
			else if (Scale == TimelineScales.Hours)
			{
				return new TextInfo
				{
					Skip = 3,
					Text = dt.ToString("dd/MM/yy HH:00")
				};
			}
			else
			{
				return new TextInfo
				{
					Skip = 3,
					Text = dt.ToString("dd/MM/yy")
				};
			}
		}

		public class TextInfo
		{
			public int OffsetX { get; set; } = 3;
			public int OffsetY { get; set; } = 14;
			public int Skip { get; set; }
			public string Text { get; set; } = string.Empty;
		}
	}
}
