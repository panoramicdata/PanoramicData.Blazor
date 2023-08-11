namespace PanoramicData.Blazor;

public partial class PDList<TItem> : IAsyncDisposable where TItem : class
{
	private TItem? _lastSelectedItem;
	private string _filterText = string.Empty;
	private Selection<TItem> _selection = new();
	private Func<TItem, string>? _compiledTextExpression;
	private IEnumerable<TItem> _allItems = Array.Empty<TItem>();

	[Parameter]
	public SelectionBehaviours AllCheckBoxWhenPartial { get; set; }

	[Parameter]
	public EventCallback<Selection<TItem>> Apply { get; set; }

	[Parameter]
	public EventCallback Cancel { get; set; }

	[Parameter]
	public bool ClearSelectionOnFilter { get; set; } = true;

	[Parameter]
	[EditorRequired]
	public IDataProviderService<TItem>? DataProvider { get; set; }

	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	[Parameter]
	public Func<TItem, string, bool>? FilterIncludeFunction { get; set; }

	[Parameter]
	public EventCallback<Selection<TItem>> SelectionChanged { get; set; }

	[Parameter]
	public TableSelectionMode SelectionMode { get; set; }

	[Parameter]
	public bool ShowAllCheckBox { get; set; }

	[Parameter]
	public bool ShowApplyCancelButtons { get; set; }

	[Parameter]
	public bool ShowCheckBoxes { get; set; }

	[Parameter]
	public bool ShowFilter { get; set; }

	[Parameter]
	public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

	[Parameter]
	public Expression<Func<TItem, object>>? SortExpression { get; set; }

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
		if (SortExpression != null)
		{
			request.SortDirection = SortDirection;
			request.SortFieldExpression = SortExpression;
		}
		return request;
	}

	public Task ClearAllAsync()
	{
		_selection.Items.Clear();
		_selection.AllSelected = false;
		return OnSelectionUpdated();
	}

	public bool ItemVisible(TItem item)
	{
		if (string.IsNullOrWhiteSpace(_filterText))
		{
			return true;
		}

		// user supplied logic?
		if (FilterIncludeFunction != null)
		{
			return FilterIncludeFunction(item, _filterText);
		}

		// default implementation
		var text = _compiledTextExpression is null
			? (item.ToString() ?? string.Empty)
			: _compiledTextExpression.Invoke(item);
		return text.ToLower(CultureInfo.InvariantCulture)
				   .Contains(_filterText.ToLower(CultureInfo.InvariantCulture));
	}

	public Selection<TItem> Selection => _selection;

	private Task OnApplyAsync() => Apply.InvokeAsync(Selection);

	private Task OnCancelAsync() => Cancel.InvokeAsync();

	private async Task OnCheckBoxClickedAsync(MouseEventArgs args, TItem? item)
	{
		if (IsEnabled)
		{
			if (item is null)
			{
				// 'All' checkbox
				if (_selection.AllSelected)
				{
					await ClearAllAsync();
				}
				else if (_selection.Items.Count == 0)
				{
					await SelectAllAsync();
				}
				else if (AllCheckBoxWhenPartial == SelectionBehaviours.SelectAll)
				{
					await SelectAllAsync();
				}
				else
				{
					await ClearAllAsync();
				}
			}
			else
			{
				// item checkbox
				await UpdateSelectionAsync(args, item);
			}
		}
	}

	private async Task OnFilterTextChangedAsync(string newValue)
	{
		if (newValue != _filterText)
		{
			_filterText = newValue;
			if (ClearSelectionOnFilter)
			{
				await ClearAllAsync().ConfigureAwait(true);
			}
		}
	}

	protected override Task OnParametersSetAsync()
	{
		if (TextExpression != null)
		{
			_compiledTextExpression = TextExpression.Compile();
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

			// store items to render
			_allItems = response.Items;
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
				var list = _allItems.ToList();
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
				_selection.AllSelected = _selection.Items.Count == _allItems.Count();
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
				_selection.AllSelected = _selection.Items.Count == _allItems.Count();
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
		await SelectionChanged.InvokeAsync(Selection).ConfigureAwait(true);
	}


	#region Attributes

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

	public Dictionary<string, object> ItemAttributes(TItem? item)
	{
		var selectedCss = item is null ? false : !ShowCheckBoxes && (_selection.AllSelected || _selection.Items.Contains(item));
		var dict = new Dictionary<string, object>()
		{
			{ "class", $"list-item d-flex align-items-center {(SelectionMode == TableSelectionMode.None || !IsEnabled ? "" : "cursor-pointer")} {(selectedCss ? "selected" : "")}" }
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

	#endregion

	#region IAsyncDisposable

	public ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);

		}
		catch
		{
		}
		return ValueTask.CompletedTask;
	}

	#endregion
}