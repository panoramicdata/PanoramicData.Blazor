namespace PanoramicData.Blazor;

public partial class PDList<TItem> : IAsyncDisposable where TItem : class
{
	private TItem? _lastSelectedItem;
	private string _filterText = string.Empty;
	private Func<TItem, string>? _compiledTextExpression;
	private IEnumerable<TItem> _allItems = [];

	[CascadingParameter]
	public IAsyncStateManager? StateManager { get; set; }

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
	public bool DefaultToSelectAll { get; set; }

	[Parameter]
	public Func<TItem, string, bool>? FilterIncludeFunction { get; set; }

	/// <remarks>
	/// Example: Overriding default Id.
	/// </remarks>
	[Parameter]
	public override string Id { get; set; } = $"pd-list-{++Sequence}";

	[Parameter]
	public Func<TItem, object>? ItemKeyFunction { get; set; }

	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	[Parameter]
	public Selection<TItem> Selection { get; set; } = new();

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

	private string AllCheckBoxIconCls => Selection.AllSelected
		? "fa-check-square"
		: Selection.Items.Count == 0
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
		Selection.Items.Clear();
		Selection.AllSelected = false;
		return OnSelectionUpdatedAsync();
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

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// load previous state?
		if (firstRender && StateManager != null)
		{
			try
			{
				var state = await StateManager.LoadStateAsync<string>(Id).ConfigureAwait(true);
				if (state != null && state != string.Empty && state != Constants.TokenNone)
				{
					if (state == Constants.TokenAll)
					{
						Selection.AllSelected = true;
					}
					else
					{
						var ids = state.Split(',', StringSplitOptions.RemoveEmptyEntries);
						foreach (var item in _allItems.Where(x => ItemVisible(x)))
						{
							var itemKey = ItemKeyFunction is null
								? item.ToString() ?? string.Empty
								: ItemKeyFunction(item).ToString();
							if (ids.Contains(itemKey))
							{
								Selection.Items.Add(item);
							}
						}
					}

					StateHasChanged();
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	private Task OnApplyAsync() => Apply.InvokeAsync(Selection);

	private Task OnCancelAsync() => Cancel.InvokeAsync();

	private async Task OnCheckBoxClickedAsync(MouseEventArgs args, TItem? item)
	{
		if (IsEnabled)
		{
			if (item is null)
			{
				// 'All' checkbox
				if (Selection.AllSelected)
				{
					await ClearAllAsync();
				}
				else if (Selection.Items.Count == 0)
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

	protected override void OnInitialized()
	{
		if (DefaultToSelectAll)
		{
			Selection.AllSelected = true;
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

	private async Task OnSelectionUpdatedAsync()
	{
		// persist selection?
		if (StateManager != null)
		{
			// save (All) token or individual ids
			var state = Selection.AllSelected ? Constants.TokenAll : string.Empty;
			if (!Selection.AllSelected && Selection.Items.Any())
			{
				state = ItemKeyFunction != null
					? string.Join(",", Selection.Items.Select(x => ItemKeyFunction(x).ToString()).ToArray())
					: Selection.ToString();
			}

			await StateManager.SaveStateAsync(Id, state).ConfigureAwait(true);
		}

		// selection has been updated
		await SelectionChanged.InvokeAsync(Selection).ConfigureAwait(true);
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
		Selection.Items.Clear();
		Selection.AllSelected = true;
		return OnSelectionUpdatedAsync();
	}

	private async Task UpdateSelectionAsync(MouseEventArgs args, TItem item)
	{
		if (SelectionMode == TableSelectionMode.None)
		{
			Selection.Items.Clear();
			Selection.AllSelected = false;
			return;
		}

		var updateLastSelected = true;
		if (SelectionMode == TableSelectionMode.Single)
		{
			var addItem = true;
			if (Selection.Items.Any(x => x == item))
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
			Selection.Items.Clear();
			if (addItem)
			{
				Selection.Items.Add(item);
			}

			Selection.AllSelected = false; // can never be true with single selection
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
					(idx2, idx1) = (idx1, idx2);
				}

				Selection.Items.Clear();
				for (var i = idx1; i <= idx2; i++)
				{
					Selection.Items.Add(list[i]);
				}

				updateLastSelected = false;

				// update all selected?
				Selection.AllSelected = Selection.Items.Count == _allItems.Count();
				if (Selection.AllSelected)
				{
					Selection.Items.Clear();
				}
			}
			else if (args.CtrlKey || ShowCheckBoxes)
			{
				// toggle selection
				if (Selection.AllSelected)
				{
					// re-populate selection with all items
					var request = BuildRequest(false);
					var response = await DataProvider!.GetDataAsync(request, default).ConfigureAwait(true);
					Selection.Items.Clear();
					Selection.Items.AddRange(response.Items);
				}

				// TItem might need to override Equals operator
				if (Selection.Items.Contains(item))
				{
					Selection.Items.Remove(item);
				}
				else
				{
					Selection.Items.Add(item);
				}

				// update all selected?
				Selection.AllSelected = Selection.Items.Count == _allItems.Count();
				if (Selection.AllSelected)
				{
					Selection.Items.Clear();
				}
			}
			else
			{
				// ignore if currently selected
				if (Selection.Items.Contains(item) && Selection.Items.Count == 1)
				{
					return;
				}

				// clear previous selection and select single item
				Selection.Items.Clear();
				Selection.Items.Add(item);
				Selection.AllSelected = false;
			}
		}

		// remember this item for range selection
		if (updateLastSelected)
		{
			_lastSelectedItem = item;
		}

		// selection has been updated
		await OnSelectionUpdatedAsync().ConfigureAwait(true);
	}

	#region Attributes

	public Dictionary<string, object> CheckBoxAttributes(TItem item)
	{
		var iconCls = Selection.AllSelected || Selection.Items.Contains(item)
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
		var selectedCss = item is not null && !ShowCheckBoxes && (Selection.AllSelected || Selection.Items.Contains(item));
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
			{ "id", Id },
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