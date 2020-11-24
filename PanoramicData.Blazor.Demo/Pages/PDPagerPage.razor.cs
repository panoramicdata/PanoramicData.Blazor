using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Demo.Data;
using System;

namespace PanoramicData.Blazor.Demo.Pages
{
	public partial class PDPagerPage : IDisposable
	{
		//private PDPager? _pager;
		private PageCriteria _pageCriteria = new PageCriteria(1);

		[CascadingParameter] protected EventManager? EventManager { get; set; }

		private string GoToPage { get; set; } = "1";

		private string TotalCount { get; set; } = "125";

		protected override void OnInitialized()
		{
			_pageCriteria.PageChanged += _pageCriteria_PageChanged;
			_pageCriteria.PageSizeChanged += _pageCriteria_PageSizeChanged;
			_pageCriteria.TotalCountChanged += _pageCriteria_TotalCountChanged;
		}

		public void Dispose()
		{
			_pageCriteria.PageChanged -= _pageCriteria_PageChanged;
			_pageCriteria.PageSizeChanged -= _pageCriteria_PageSizeChanged;
			_pageCriteria.TotalCountChanged -= _pageCriteria_TotalCountChanged;
		}

		private void _pageCriteria_TotalCountChanged(object sender, System.EventArgs e)
		{
			EventManager?.Add(new Event("TotalCountChanged", new EventArgument("TotalCount", _pageCriteria.TotalCount)));
		}

		private void _pageCriteria_PageSizeChanged(object sender, System.EventArgs e)
		{
			EventManager?.Add(new Event("PageSizeChanged", new EventArgument("PageSize", _pageCriteria.PageSize)));
		}

		private void _pageCriteria_PageChanged(object sender, System.EventArgs e)
		{
			EventManager?.Add(new Event("PageChanged", new EventArgument("Page", _pageCriteria.Page)));
		}

		private void OnGotoPage()
		{
			if (uint.TryParse(GoToPage, out uint page))
			{
				_pageCriteria.Page = page;
			}
		}

		private void OnSetTotalCount()
		{
			if (uint.TryParse(TotalCount, out uint count))
			{
				_pageCriteria.TotalCount = count;
			}
		}
	}
}
