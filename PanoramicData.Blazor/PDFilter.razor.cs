using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
using System;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDFilter
	{
		[Parameter]
		public Filter Filter { get; set; } = new Filter();

		[Parameter]
		public EventCallback<Filter> FilterChanged { get; set; }

		[Parameter]
		public string IconCssClass { get; set; } = "fas fa-filter";

		private string CssClass => $"p-0 pd-filter {(HasFilter ? "filtered" : "")}";

		private bool HasFilter => !string.IsNullOrWhiteSpace(Filter.Value);

		private Task OnDropDownShown()
		{
			// TODO: Fetch / cache values
			return Task.CompletedTask;
		}
		private async Task OnFilterTextChange(ChangeEventArgs args)
		{
			Filter.Value = args.Value.ToString();
			await FilterChanged.InvokeAsync(Filter).ConfigureAwait(true);
		}

		private async Task OnFilterTypeChanged(ChangeEventArgs args)
		{
			Filter.FilterType = (FilterTypes)Enum.Parse(typeof(FilterTypes), args.Value.ToString());
			await FilterChanged.InvokeAsync(Filter).ConfigureAwait(true);
		}
	}
}
