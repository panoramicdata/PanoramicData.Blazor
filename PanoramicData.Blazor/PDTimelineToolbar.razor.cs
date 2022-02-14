using Microsoft.AspNetCore.Components;
using System;

namespace PanoramicData.Blazor
{
	public partial class PDTimelineToolbar
	{
		[Parameter]
		public bool IsEnabled { get; set; } = true;

		[Parameter]
		public bool ShowRange { get; set; } = true;

		[Parameter]
		public bool ShowScale { get; set; } = true;

		[Parameter]
		public bool ShowSelection { get; set; } = true;

		[Parameter]
		public bool ShowZoomButtons { get; set; } = true;

		[Parameter]
		public PDTimeline? Timeline { get; set; }

		private string MinDateTimeDisplay => (Timeline?.MinDateTime ?? DateTime.Now).ToString($"{Timeline?.Options.General.DateFormat ?? "d"} HH:mm:ss");

		private string MaxDateTimeDisplay => (Timeline?.MaxDateTime ?? DateTime.Now).ToString($"{Timeline?.Options.General.DateFormat ?? "d"} HH:mm:ss");
		private string SelectionEndTimeDisplay => (Timeline?.GetSelection()?.EndTime ?? DateTime.MinValue).ToString($"{Timeline?.Options.General.DateFormat ?? "d"} HH:mm:ss");

		private string SelectionStartTimeDisplay => (Timeline?.GetSelection()?.StartTime ?? DateTime.MinValue).ToString($"{Timeline?.Options.General.DateFormat ?? "d"} HH:mm:ss");
	}
}
