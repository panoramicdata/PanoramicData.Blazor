using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDPagerPage
	{
		private PDPager? _pager;
		[CascadingParameter] protected EventManager? EventManager { get; set; }

		private string GoToPage { get; set; } = "1";

		private string NewPageCount { get; set; } = "10";

		private async Task OnGotoPage()
		{
			if (_pager != null && uint.TryParse(GoToPage, out uint page))
			{
				await _pager.SetPageAsync(page).ConfigureAwait(true);
			}
		}

		private async Task OnSetPageCount()
		{
			if (_pager != null && uint.TryParse(NewPageCount, out uint count))
			{
				await _pager.SetPageCountAsync(count).ConfigureAwait(true);
			}
		}

		private void OnPageChanged(uint page)
		{
			EventManager?.Add(new Event("PageChanged", new EventArgument("Page", page)));
		}
	}
}
