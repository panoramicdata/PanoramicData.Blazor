using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
using System;
using System.Linq;

namespace PanoramicData.Blazor.Timeline
{
	public partial class PDPlotArea
	{
		private double _maxYValue;

		[Parameter]
		public DataPoint[] DataPoints { get; set; } = Array.Empty<DataPoint>();

		[Parameter]
		public double Height { get; set; }

		[Parameter]
		public TimelineOptions Options { get; set; } = new TimelineOptions();

		[Parameter]
		public int TotalPeriods { get; set; }

		[Parameter]
		public double X { get; set; }

		[Parameter]
		public double Y { get; set; }

		protected override void OnParametersSet()
		{
			_maxYValue = DataPoints.Length == 0 ? 0 : DataPoints.Max(x => x.SeriesValues.Sum());
		}

		private string CalculateViewBox()
		{
			var x = 0;
			var y = 0;
			var width = TotalPeriods * Options.Bar.Width;
			var height = _maxYValue;
			return $"{x} {y} {width} {height}";
		}
	}
}
