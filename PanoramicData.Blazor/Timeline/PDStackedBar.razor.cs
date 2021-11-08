using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;
using System;

namespace PanoramicData.Blazor.Timeline
{
	public partial class PDStackedBar
	{
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

		private void OnMouseDown(MouseEventArgs args)
		{
			Console.WriteLine($"Mouse Down: {args.ClientX} - {DataPoint.PeriodIndex}");
		}

		private void OnMouseUp(MouseEventArgs args)
		{
			Console.WriteLine($"Mouse Up: {args.ClientX} - {DataPoint.PeriodIndex}");
		}
	}
}
