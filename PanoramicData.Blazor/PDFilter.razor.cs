using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;
using System;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDFilter
	{
		private PDDropDown _dropDown = null!;
		private string[] _values = Array.Empty<string>();

		[Parameter]
		public Filter Filter { get; set; } = new Filter();

		[Parameter]
		public EventCallback<Filter> FilterChanged { get; set; }

		[Parameter]
		public Func<Filter, Task<string[]>>? FetchValuesAsync { get; set; }

		[Parameter]
		public string IconCssClass { get; set; } = "fas fa-filter";

		private string CssClass => $"p-0 pd-filter {(HasFilter ? "filtered" : "")}";

		private bool HasFilter => !string.IsNullOrWhiteSpace(Filter.Value);

		private Task OnDropDownShown()
		{
			return RefreshValues();
		}

		private async Task OnDropDownKeyPress(int keyCode)
		{
			if(keyCode == 13)
			{
				await _dropDown.ToggleAsync().ConfigureAwait(true);
			}
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
			if(Filter.FilterType == FilterTypes.NoFilter)
			{
				await RefreshValues().ConfigureAwait(true);
			}
		}

		private async Task OnValueClicked(string value)
		{
			Filter.FilterType = FilterTypes.Equals;
			Filter.Value = value;
			await FilterChanged.InvokeAsync(Filter).ConfigureAwait(true);
		}

		private async Task RefreshValues()
		{
			if (FetchValuesAsync != null)
			{
				_values = await FetchValuesAsync.Invoke(Filter).ConfigureAwait(true);
			}
		}
	}
}
