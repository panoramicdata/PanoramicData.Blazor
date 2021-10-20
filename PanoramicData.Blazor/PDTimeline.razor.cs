using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDTimeline : IDisposable
	{
		private static int _seq;
		private PDCanvas _canvas = null!;
		private DotNetObjectReference<PDTimeline>? _objRef;

		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// Tuple = (datetime, series index, value)
		/// </summary>
		/// <remarks>index is zero based</remarks>
		[Parameter]
		public IDataProviderService<TimelineDataPoint>? DataProvider { get; set; }

		[Parameter]
		public string Id { get; set; } = $"pd-timeline-{+_seq}";

		[Parameter]
		public int Height { get; set; } = 100;

		[Parameter]
		public TimelineOptions Options { get; set; } = new TimelineOptions();

		[Parameter]
		public int Width { get; set; } = 400;

		private string CanvasId => $"{Id}-canvas";

		public async Task<DataPoint[]> FetchData()
		{
			if (DataProvider != null)
			{
				// fetch data
				var request = new DataRequest<TimelineDataPoint>();
				var response = await DataProvider.GetDataAsync(request, default).ConfigureAwait(true);

				// TODO: aggregate due to zoom

				// convert to graph data points
				var barIdx = 0;
				var points = new List<DataPoint>();
				// group by day for now
				foreach (var bucket in response.Items.GroupBy(x => x.DateTime.Date))
				{
					// sum each series for bucket
					var values = new List<double>();
					for (var seriesIdx = 0; seriesIdx < Options.Series.Length; seriesIdx++)
					{
						var sum = bucket.Where(x => x.Series == seriesIdx).Sum(x => x.Value);
						values.Add(sum);
					}
					points.Add(new DataPoint
					{
						BarIndex = barIdx++,
						SeriesValues = values.ToArray()
					});
				}
				return points.ToArray();
			}
			return Array.Empty<DataPoint>();
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				_objRef = DotNetObjectReference.Create(this);
				var data = await FetchData().ConfigureAwait(true);
				await JSRuntime.InvokeVoidAsync("panoramicData.timeline.init", CanvasId, Options, data, _objRef).ConfigureAwait(true);
				await Refresh().ConfigureAwait(true);
			}
		}

		public async Task Refresh()
		{
			if (DataProvider != null)
			{
				var data = await FetchData().ConfigureAwait(true);
				await JSRuntime.InvokeVoidAsync("panoramicData.timeline.setData", CanvasId, data).ConfigureAwait(true);
			}
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("panoramicData.timeline.term", CanvasId);
		}

		public class DataPoint
		{
			public int BarIndex { get; set; }
			public double[] SeriesValues { get; set; }
		}
	}
}
