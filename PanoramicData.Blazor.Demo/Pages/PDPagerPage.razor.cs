using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDPagerPage
    {
		[CascadingParameter] protected EventManager? EventManager { get; set; }

		private int CurrentPage { get; set; } = 1;
		private int PageCount { get; set; } = 5;

		private void OnPageChanged(int page)
		{
			EventManager?.Add(new Event("PageChanged", new EventArgument("Page", page)));
			CurrentPage = page;
		}
	}
}
