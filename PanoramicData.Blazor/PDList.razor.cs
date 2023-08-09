namespace PanoramicData.Blazor;

public partial class PDList<TItem> : IAsyncDisposable where TItem : class
{
	private TItem? _lastSelectedItem;
	private List<TItem> _selectedItems = new();
	private IEnumerable<TItem> _displayedItems = Array.Empty<TItem>();
	private Func<TItem, string>? _compiledTextExpression;
	private PageCriteria? _pageInfo;

	[Parameter]
	[EditorRequired]
	public IDataProviderService<TItem>? DataProvider { get; set; }

	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	[Parameter]
	public PageCriteria? PageInfo { get; set; }

	[Parameter]
	public EventCallback<SelectionArgs<TItem>> SelectionChanged { get; set; }

	[Parameter]
	public TableSelectionMode SelectionMode { get; set; }

	[Parameter]
	public bool ShowPager { get; set; }

	[Parameter]
	public SortDirection SortDirection { get; set; }

	[Parameter]
	public Expression<Func<TItem, object>> SortExpression { get; set; }
		= (x) => x == null || x.ToString() == null ? string.Empty : x.ToString()!;

	[Parameter]
	public Expression<Func<TItem, string>>? TextExpression { get; set; }

	public Dictionary<string, object> ItemAttributes(TItem item)
	{
		var dict = new Dictionary<string, object>()
		{
			{ "class", $"list-item {(SelectionMode == TableSelectionMode.None ? "" : "cursor-pointer")} {(_selectedItems.Contains(item) ? "selected" : "")}" }
		};
		return dict;
	}
	public Dictionary<string, object> ListAttributes()
	{
		var dict = new Dictionary<string, object>()
		{
			{ "class", $"pd-list {CssClass}{(IsVisible ? "" : " d-none")}{(IsEnabled ? "" : " disabled")}" },
			{ "title", ToolTip }
		};
		return dict;
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
			_displayedItems = response.Items;
		}
	}

	private async Task UpdateSelectionAsync(MouseEventArgs args, TItem item)
	{
		if (SelectionMode == TableSelectionMode.None)
		{
			return;
		}

		if (SelectionMode == TableSelectionMode.Single)
		{
			// ignore if currently selected
			if (_selectedItems.Any(x => x == item))
			{
				return;
			}

			// simply clear current selection
			_selectedItems.Clear();
			_selectedItems.Add(item);
		}

		if (SelectionMode == TableSelectionMode.Multiple)
		{
			if (args.CtrlKey)
			{
				// toggle selection
				if (_selectedItems.Contains(item))
				{
					_selectedItems.Remove(item);
				}
				else
				{
					_selectedItems.Add(item);
				}
			}
			else if (args.ShiftKey && _lastSelectedItem != null)
			{
				// range selection
				var list = _displayedItems.ToList();
				var idx1 = list.IndexOf(_lastSelectedItem);
				var idx2 = list.IndexOf(item);
				if (idx2 < idx1)
				{
					var idxT = idx1;
					idx1 = idx2;
					idx2 = idxT;
				}
				_selectedItems.Clear();
				for (var i = idx1; i <= idx2; i++)
				{
					_selectedItems.Add(list[i]);
				}
			}
			else
			{
				// ignore if currently selected
				if (_selectedItems.Contains(item) && _selectedItems.Count == 1)
				{
					return;
				}

				// clear previous selection and select single item
				_selectedItems.Clear();
				_selectedItems.Add(item);
			}
		}

		// remember this item for range selection
		_lastSelectedItem = item;

		// selection has been updated
		var ea = new SelectionArgs<TItem> { SelectedItems = _selectedItems };
		await SelectionChanged.InvokeAsync(ea).ConfigureAwait(true);
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