namespace PanoramicData.Blazor;

public partial class PDList<TItem> : IAsyncDisposable where TItem : class
{
	private int _totalItemCount;
	private TItem? _lastSelectedItem;
	private Selection<TItem> _selection = new();
	private IEnumerable<TItem> _displayedItems = Array.Empty<TItem>();
	private Func<TItem, string>? _compiledTextExpression;
	private PageCriteria? _pageInfo;

	[Parameter]
	public SelectionBehaviours AllCheckBoxWhenPartial { get; set; }

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
	public bool ShowAllCheckBox { get; set; }

	[Parameter]
	public bool ShowCheckBoxes { get; set; }

	[Parameter]
	public bool ShowPager { get; set; }

	[Parameter]
	public SortDirection SortDirection { get; set; }

	[Parameter]
	public Expression<Func<TItem, object>> SortExpression { get; set; }
		= (x) => x == null || x.ToString() == null ? string.Empty : x.ToString()!;

	[Parameter]
	public Expression<Func<TItem, string>>? TextExpression { get; set; }

	private string AllCheckBoxIconCls => _selection.AllSelected
		? "fa-check-square"
		: _selection.Items.Count == 0
			? "fa-square"
			: "fa-minus-square";

	private DataRequest<TItem> BuildRequest(bool observePaging = true)
	{
		var request = new DataRequest<TItem>();
		if (_pageInfo != null && observePaging)
		{
			request.Skip = (int)_pageInfo.PageRangeStart - 1;
			request.Take = (int)_pageInfo.PageSize;
		}
		if (SortExpression != null)
		{
			request.SortDirection = SortDirection;
			request.SortFieldExpression = SortExpression;
		}
		return request;
	}

	public Dictionary<string, object> CheckBoxAttributes(TItem item)
	{
		var iconCls = _selection.AllSelected || _selection.Items.Contains(item)
			? "far fa-check-square"
			: "far fa-square";
		var dict = new Dictionary<string, object>()
		{
			{ "class", $"me-2 {iconCls}" }
		};
		return dict;
	}

	public Task ClearAllAsync()
	{
		_selection.Items.Clear();
		_selection.AllSelected = false;
		return OnSelectionUpdated();
	}

	public Selection<TItem> Selection => _selection;

	public Dictionary<string, object> ItemAttributes(TItem? item)
	{
		var selectedCss = item is null ? false : !ShowCheckBoxes && (_selection.AllSelected || _selection.Items.Contains(item));
		var dict = new Dictionary<string, object>()
		{
			{ "class", $"list-item d-flex align-items-center {(SelectionMode == TableSelectionMode.None ? "" : "cursor-pointer")} {(selectedCss ? "selected" : "")}" }
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

	private void OnAllCheckBoxClicked()
	{
		if (_selection.AllSelected)
		{
			ClearAllAsync();
		}
		else if (_selection.Items.Count == 0)
		{
			SelectAllAsync();
		}
		else if (AllCheckBoxWhenPartial == SelectionBehaviours.SelectAll)
		{
			SelectAllAsync();
		}
		else
		{
			ClearAllAsync();
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
			// fetch data
			var request = BuildRequest();
			var response = await DataProvider.GetDataAsync(request, cancellationToken).ConfigureAwait(true);

			// update page info
			if (_pageInfo != null)
			{
				_pageInfo.TotalCount = (uint)(response.TotalCount ?? 0);
			}
			_totalItemCount = response.TotalCount ?? 0;

			// store items to render
			_displayedItems = response.Items;
		}
	}

	public Task SelectAllAsync()
	{
		_selection.Items.Clear();
		_selection.AllSelected = true;
		return OnSelectionUpdated();
	}

	private async Task UpdateSelectionAsync(MouseEventArgs args, TItem item)
	{
		if (SelectionMode == TableSelectionMode.None)
		{
			_selection.Items.Clear();
			_selection.AllSelected = false;
			return;
		}

		var updateLastSelected = true;
		if (SelectionMode == TableSelectionMode.Single)
		{
			var addItem = true;
			if (_selection.Items.Any(x => x == item))
			{
				if (ShowCheckBoxes)
				{
					// in effect toggle item
					addItem = false;
				}
				else
				{
					// ignore if currently selected
					return;
				}
			}

			// update selection
			_selection.Items.Clear();
			if (addItem)
			{
				_selection.Items.Add(item);
			}
			_selection.AllSelected = false; // can never be true with single selection
		}

		if (SelectionMode == TableSelectionMode.Multiple)
		{
			if (args.ShiftKey && _lastSelectedItem != null)
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
				_selection.Items.Clear();
				for (var i = idx1; i <= idx2; i++)
				{
					_selection.Items.Add(list[i]);
				}
				updateLastSelected = false;

				// update all selected?
				_selection.AllSelected = _totalItemCount > 0 && _selection.Items.Count == _totalItemCount;
				if (_selection.AllSelected)
				{
					_selection.Items.Clear();
				}
			}
			else if (args.CtrlKey || ShowCheckBoxes)
			{
				// toggle selection
				if (_selection.AllSelected)
				{
					// re-populate selection with all items
					var request = BuildRequest(false);
					var response = await DataProvider!.GetDataAsync(request, default).ConfigureAwait(true);
					_selection.Items.Clear();
					_selection.Items.AddRange(response.Items);
				}

				// TItem might need to override Equals operator
				if (_selection.Items.Contains(item))
				{
					_selection.Items.Remove(item);
				}
				else
				{
					_selection.Items.Add(item);
				}

				// update all selected?
				_selection.AllSelected = _totalItemCount > 0 && _selection.Items.Count == _totalItemCount;
				if (_selection.AllSelected)
				{
					_selection.Items.Clear();
				}
			}
			else
			{
				// ignore if currently selected
				if (_selection.Items.Contains(item) && _selection.Items.Count == 1)
				{
					return;
				}

				// clear previous selection and select single item
				_selection.Items.Clear();
				_selection.Items.Add(item);
				_selection.AllSelected = false;
			}
		}

		// remember this item for range selection
		if (updateLastSelected)
		{
			_lastSelectedItem = item;
		}

		// selection has been updated
		await OnSelectionUpdated().ConfigureAwait(true);
	}

	private async Task OnSelectionUpdated()
	{
		// selection has been updated
		var ea = new SelectionArgs<TItem> { Selection = Selection };
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