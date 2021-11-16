using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;
using System;
using System.Linq;
using System.Text;

namespace PanoramicData.Blazor.Timeline
{
	public partial class PDStackedBar
	{
		[Parameter]
		public string DateFormat { get; set; } = "dd/MM/yy HH:mm";

		[Parameter]
		public DataPoint DataPoint { get; set; } = new DataPoint();

		[Parameter]
		public double MaxValue { get; set; } = 100;

		[Parameter]
		public TimelineOptions Options { get; set; } = new TimelineOptions();

		[Parameter]
		public double Padding { get; set; } = 1;

		[Parameter]
		public double X { get; set; }

		private string GetTitle()
		{
			var sb = new StringBuilder();
			sb.AppendLine(DataPoint.StartTime.ToString(DateFormat));
			if (DataPoint != null && DataPoint.SeriesValues.Length > 0)
			{
				for (var i = 0; i < Options.Series.Length; i++)
				{
					sb.Append(Options.Series[i].Label)
					  .Append(": ")
					  .AppendLine(DataPoint.SeriesValues[i].ToString(Options.Series[i].Format));
				}
			}
			return sb.ToString();
		}
	}
}
