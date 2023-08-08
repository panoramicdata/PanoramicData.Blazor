namespace PanoramicData.Blazor;

public partial class PDList<TItem> : IAsyncDisposable where TItem : class
{
	private IEnumerable<TItem> _items = Array.Empty<TItem>();
	private Func<TItem, string>? _compiledTextExpression;
	private PageCriteria? _pageInfo;

	[Parameter]
	[EditorRequired]
	public IDataProviderService<TItem>? DataProvider { get; set; }

	[Parameter]
	public PageCriteria? PageInfo { get; set; }

	[Parameter]
	public bool ShowPager { get; set; }

	[Parameter]
	public SortDirection SortDirection { get; set; }

	[Parameter]
	public Expression<Func<TItem, object>> SortExpression { get; set; }
		= (x) => x == null || x.ToString() == null ? string.Empty : x.ToString()!;

	[Parameter]
	public Expression<Func<TItem, string>>? TextExpression { get; set; }

	public Dictionary<string, object> Attributes
	{
		get
		{
			var dict = new Dictionary<string, object>()
			{
				{ "class", $"pd-list {CssClass}{(IsVisible ? "" : " d-none")}{(IsEnabled ? "" : " disabled")}" },
				{ "title", ToolTip }
			};
			return dict;
		}
	}

	protected override Task OnParametersSetAsync()
	{
		if (TextExpression != null)
		{
			_compiledTextExpression = TextExpression.Compile();
		}

		if (PageInfo != _pageInfo)
		{
			if (_pageInfo != null)
			{
				_pageInfo.PageChanged -= PageInfo_PageChanged;
			}
			_pageInfo = PageInfo;
			if (_pageInfo != null)
			{
				_pageInfo.PageChanged += PageInfo_PageChanged;
			}
		}

		return RefreshAsync(default);
	}

	private async void PageInfo_PageChanged(object? sender, EventArgs e)
	{
		await RefreshAsync(default).ConfigureAwait(true);
		await InvokeAsync(() => StateHasChanged()).ConfigureAwait(true);
	}

	public async Task RefreshAsync(CancellationToken cancellationToken)
	{
		if (DataProvider != null)
		{
			// build request
			var request = new DataRequest<TItem>();
			if (_pageInfo != null)
			{
				request.Skip = (int)_pageInfo.PageRangeStart - 1;
				request.Take = (int)_pageInfo.PageSize;
			}
			if (SortExpression != null)
			{
				request.SortDirection = SortDirection;
				request.SortFieldExpression = SortExpression;
			}

			// fetch data
			var response = await DataProvider.GetDataAsync(request, cancellationToken).ConfigureAwait(true);

			// update page info
			if (_pageInfo != null)
			{
				_pageInfo.TotalCount = (uint)(response.TotalCount ?? 0);
			}

			// store items to render
			_items = response.Items;
		}
	}

	#region IAsyncDisposable

	public ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_pageInfo != null)
			{
				_pageInfo.PageChanged -= PageInfo_PageChanged;
			}

		}
		catch
		{
		}
		return ValueTask.CompletedTask;
	}

	#endregion
}