﻿namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDPagerPage : IDisposable
{
	//private PDPager? _pager;
	private readonly PageCriteria _pageCriteria = new(1);

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private string GoToPage { get; set; } = "1";

	private string TotalCount { get; set; } = "125";

	protected override void OnInitialized()
	{
		_pageCriteria.PageChanged += PageCriteria_PageChanged;
		_pageCriteria.PageSizeChanged += PageCriteria_PageSizeChanged;
		_pageCriteria.TotalCountChanged += PageCriteria_TotalCountChanged;
	}

	public void Dispose()
	{
		_pageCriteria.PageChanged -= PageCriteria_PageChanged;
		_pageCriteria.PageSizeChanged -= PageCriteria_PageSizeChanged;
		_pageCriteria.TotalCountChanged -= PageCriteria_TotalCountChanged;
	}

	private void PageCriteria_TotalCountChanged(object? sender, EventArgs e) => EventManager?.Add(new Event("TotalCountChanged", new EventArgument("TotalCount", _pageCriteria.TotalCount)));

	private void PageCriteria_PageSizeChanged(object? sender, EventArgs e) => EventManager?.Add(new Event("PageSizeChanged", new EventArgument("PageSize", _pageCriteria.PageSize)));

	private void PageCriteria_PageChanged(object? sender, EventArgs e) => EventManager?.Add(new Event("PageChanged", new EventArgument("Page", _pageCriteria.Page)));

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
