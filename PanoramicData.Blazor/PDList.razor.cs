using PanoramicData.Blazor.Helpers;

namespace PanoramicData.Blazor;

/// <summary>
/// Generic list component with filtering, sorting, and selectable item behavior.
/// </summary>
/// <typeparam name="TItem">Item type.</typeparam>
public partial class PDList<TItem> : IAsyncDisposable where TItem : class
{
	private TItem? _lastSelectedItem;
	private string _filterText = string.Empty;
	private Func<TItem, string>? _compiledTextExpression;
	private IEnumerable<TItem> _allItems = [];

	/// <summary>
	/// Gets or sets optional state manager used to persist selection state.
	/// </summary>
	[CascadingParameter]
	public IAsyncStateManager? StateManager { get; set; }

	/// <summary>
	/// Determines the behavior of the 'All' checkbox when the selection is partial.
	/// </summary>
	[Parameter]
	public SelectionBehaviours AllCheckBoxWhenPartial { get; set; }

	/// <summary>
	/// An event callback that is invoked when the 'Apply' button is clicked.
	/// </summary>
	[Parameter]
	public EventCallback<Selection<TItem>> Apply { get; set; }

	/// <summary>
	/// An event callback that is invoked when the 'Cancel' button is clicked.
	/// </summary>
	[Parameter]
	public EventCallback Cancel { get; set; }

	/// <summary>
	/// Gets or sets whether to clear the selection when the filter text changes.
	/// </summary>
	[Parameter]
	public bool ClearSelectionOnFilter { get; set; } = true;

	/// <summary>
	/// Gets or sets the data provider service for the list.
	/// </summary>
	[Parameter]
	[EditorRequired]
	public IDataProviderService<TItem>? DataProvider { get; set; }

	/// <summary>
	/// Gets or sets whether to select all items by default.
	/// </summary>
	[Parameter]
	public bool DefaultToSelectAll { get; set; }

	/// <summary>
	/// A function to determine whether an item should be included in the filtered list.
	/// </summary>
	[Parameter]
	public Func<TItem, string, bool>? FilterIncludeFunction { get; set; }

	/// <remarks>
	/// Example: Overriding default Id.
	/// </remarks>
	[Parameter]
	public override string Id { get; set; } = $"pd-list-{ComponentIdSequence.Next()}";

	/// <summary>
	/// A function to get the key for a given item.
	/// </summary>
	[Parameter]
	public Func<TItem, object>? ItemKeyFunction { get; set; }

	/// <summary>
	/// A template for rendering each item in the list.
	/// </summary>
	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	/// <summary>
	/// Gets or sets the current selection.
	/// </summary>
	[Parameter]
	public Selection<TItem> Selection { get; set; } = new();

	/// <summary>
	/// An event callback that is invoked when the selection changes.
	/// </summary>
	[Parameter]
	public EventCallback<Selection<TItem>> SelectionChanged { get; set; }

	/// <summary>
	/// Gets or sets the selection mode for the list.
	/// </summary>
	[Parameter]
	public TableSelectionMode SelectionMode { get; set; }

	/// <summary>
	/// Gets or sets whether to show the 'All' checkbox.
	/// </summary>
	[Parameter]
	public bool ShowAllCheckBox { get; set; }

	/// <summary>
	/// Gets or sets whether to show the 'Apply' and 'Cancel' buttons.
	/// </summary>
	[Parameter]
	public bool ShowApplyCancelButtons { get; set; }

	/// <summary>
	/// Gets or sets whether to show checkboxes for each item.
	/// </summary>
	[Parameter]
	public bool ShowCheckBoxes { get; set; }

	/// <summary>
	/// Gets or sets whether to show the filter input.
	/// </summary>
	[Parameter]
	public bool ShowFilter { get; set; }

	/// <summary>
	/// Gets or sets the sort direction for the list.
	/// </summary>
	[Parameter]
	public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

	/// <summary>
	/// An expression to specify the sort order for the list.
	/// </summary>
	[Parameter]
	public Expression<Func<TItem, object>>? SortExpression { get; set; }

	/// <summary>
	/// An expression to specify the text to be displayed for each item.
	/// </summary>
	[Parameter]
	public Expression<Func<TItem, string>>? TextExpression { get; set; }

	private string AllCheckBoxIconCls => Selection.AllSelected
		? "fa-check-square"
		: Selection.Items.Count == 0
			? "fa-square"
			: "fa-minus-square";

	private DataRequest<TItem> BuildRequest()
	{
		var request = new DataRequest<TItem>();
		if (SortExpression != null)
		{
			request.SortDirection = SortDirection;
			request.SortFieldExpression = SortExpression;
		}

		return request;
	}

	/// <summary>
	/// Clears all selected items.
	/// </summary>
	/// <returns>A task that completes when selection updates are propagated.</returns>
	public Task ClearAllAsync()
	{
		Selection.Items.Clear();
		Selection.AllSelected = false;
		return OnSelectionUpdatedAsync();
	}

	/// <summary>
	/// Determines whether an item is visible under the current filter.
	/// </summary>
	/// <param name="item">Item to test.</param>
	/// <returns>True if the item should be shown.</returns>
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
		return text.Contains(_filterText, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Restores persisted selection state after first render.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		// load previous state?
		if (firstRender && StateManager != null)
		{
			try
			{
				var state = await StateManager.LoadStateAsync<string>(Id).ConfigureAwait(true);
				if (!string.IsNullOrEmpty(state) && state != Constants.TokenNone)
				{
					RestoreSelection(state);
					StateHasChanged();
				}
			}
			catch
			{
				// BC-40 - fast page switching in Server Side blazor can lead to OnAfterRender call after page / objects disposed
			}
		}
	}

	/// <summary>
	/// Applies a persisted selection state: the (All) token, or the keys of the selected items.
	/// </summary>
	/// <param name="state">The persisted state, neither empty nor the (None) token.</param>
	private void RestoreSelection(string state)
	{
		if (state == Constants.TokenAll)
		{
			Selection.AllSelected = true;
			return;
		}

		// Without an ItemKeyFunction the selection was saved as Selection.ToString(), which separates
		// the items with ", ", so each id is trimmed or every item after the first fails to match.
		var ids = ItemKeyFunction is null
			? state.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			: state.Split(',', StringSplitOptions.RemoveEmptyEntries);
		foreach (var item in _allItems.Where(x => ItemVisible(x) && ids.Contains(GetStateKey(x))))
		{
			Selection.Items.Add(item);
		}
	}

	/// <summary>
	/// Gets the key under which an item's selection is persisted.
	/// </summary>
	/// <param name="item">The item.</param>
	/// <returns>The item key, from <see cref="ItemKeyFunction"/> when set, otherwise the item's trimmed text.</returns>
	private string? GetStateKey(TItem item)
		=> ItemKeyFunction is null
			? item.ToString()?.Trim() ?? string.Empty
			: ItemKeyFunction(item).ToString();

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

	/// <summary>
	/// Initializes default selection behavior.
	/// </summary>
	protected override void OnInitialized()
	{
		if (DefaultToSelectAll)
		{
			Selection.AllSelected = true;
		}
	}

	/// <summary>
	/// Compiles expressions and refreshes list data for current parameters.
	/// </summary>
	/// <returns>A refresh task.</returns>
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
			if (!Selection.AllSelected && Selection.Items.Count != 0)
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

	/// <summary>
	/// Refreshes list data from the configured data provider.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
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

	/// <summary>
	/// Selects all visible items.
	/// </summary>
	/// <returns>A task that completes when selection updates are propagated.</returns>
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

		var outcome = SelectionMode == TableSelectionMode.Single
			? UpdateSingleSelection(item)
			: await UpdateMultipleSelectionAsync(args, item).ConfigureAwait(true);
		if (outcome == SelectionOutcome.Ignored)
		{
			return;
		}

		// remember this item for range selection
		if (outcome == SelectionOutcome.Updated)
		{
			_lastSelectedItem = item;
		}

		// selection has been updated
		await OnSelectionUpdatedAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// The result of applying a click to the selection.
	/// </summary>
	private enum SelectionOutcome
	{
		/// <summary>The click left the selection unchanged.</summary>
		Ignored,

		/// <summary>The selection changed and the clicked item becomes the anchor for range selection.</summary>
		Updated,

		/// <summary>A range was selected; the existing range anchor is kept.</summary>
		RangeSelected
	}

	private SelectionOutcome UpdateSingleSelection(TItem item)
	{
		var isSelected = Selection.Items.Any(x => x == item);

		// ignore if currently selected, unless check boxes are shown, in which case the click toggles the item
		if (isSelected && !ShowCheckBoxes)
		{
			return SelectionOutcome.Ignored;
		}

		// update selection
		Selection.Items.Clear();
		if (!isSelected)
		{
			Selection.Items.Add(item);
		}

		Selection.AllSelected = false; // can never be true with single selection
		return SelectionOutcome.Updated;
	}

	private async Task<SelectionOutcome> UpdateMultipleSelectionAsync(MouseEventArgs args, TItem item)
	{
		if (args.ShiftKey && _lastSelectedItem != null)
		{
			SelectRange(_lastSelectedItem, item);
			return SelectionOutcome.RangeSelected;
		}

		if (args.CtrlKey || ShowCheckBoxes)
		{
			await ToggleItemAsync(item).ConfigureAwait(true);
			return SelectionOutcome.Updated;
		}

		// ignore if currently selected
		if (Selection.Items.Contains(item) && Selection.Items.Count == 1)
		{
			return SelectionOutcome.Ignored;
		}

		// clear previous selection and select single item
		Selection.Items.Clear();
		Selection.Items.Add(item);
		Selection.AllSelected = false;
		return SelectionOutcome.Updated;
	}

	private void SelectRange(TItem anchor, TItem item)
	{
		var list = _allItems.ToList();
		var idx1 = list.IndexOf(anchor);
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

		UpdateAllSelected();
	}

	private async Task ToggleItemAsync(TItem item)
	{
		if (Selection.AllSelected)
		{
			// re-populate selection with all items
			var request = BuildRequest();
			var response = await DataProvider!.GetDataAsync(request, default).ConfigureAwait(true);
			Selection.Items.Clear();
			Selection.Items.AddRange(response.Items);
		}

		// TItem might need to override Equals operator
		if (!Selection.Items.Remove(item))
		{
			Selection.Items.Add(item);
		}

		UpdateAllSelected();
	}

	/// <summary>
	/// Switches to the 'all selected' state, which holds no individual items, once every item is selected.
	/// </summary>
	private void UpdateAllSelected()
	{
		Selection.AllSelected = Selection.Items.Count == _allItems.Count();
		if (Selection.AllSelected)
		{
			Selection.Items.Clear();
		}
	}

	#region Attributes

	/// <summary>
	/// Builds checkbox element attributes for an item.
	/// </summary>
	/// <param name="item">Item used to derive checkbox state.</param>
	/// <returns>Attribute dictionary.</returns>
	public Dictionary<string, object> CheckBoxAttributes(TItem item)
	{
		var iconCls = Selection.AllSelected || Selection.Items.Contains(item)
			? "far fa-check-square"
			: "far fa-square";
		var dict = new Dictionary<string, object>
		{
			{ "class", $"me-2 {iconCls}" }
		};
		return dict;
	}

	/// <summary>
	/// Builds attributes for a rendered list item.
	/// </summary>
	/// <param name="item">List item, if available.</param>
	/// <returns>Attribute dictionary.</returns>
	public Dictionary<string, object> ItemAttributes(TItem? item)
	{
		var selectedCss = item is not null && !ShowCheckBoxes && (Selection.AllSelected || Selection.Items.Contains(item));
		var dict = new Dictionary<string, object>
		{
			{ "class", $"list-item d-flex align-items-center {(SelectionMode == TableSelectionMode.None || !IsEnabled ? "" : "cursor-pointer")} {(selectedCss ? "selected" : "")}" }
		};
		return dict;
	}

	/// <summary>
	/// Builds attributes for the list container.
	/// </summary>
	/// <returns>Attribute dictionary.</returns>
	public Dictionary<string, object> ListAttributes()
	{
		var dict = new Dictionary<string, object>
		{
			{ "class", $"pd-list {CssClass}{(IsVisible ? "" : " d-none")}{(IsEnabled ? "" : " disabled")}" },
			{ "id", Id },
			{ "title", ToolTip }
		};
		return dict;
	}

	#endregion

	#region IAsyncDisposable

	/// <summary>
	/// Disposes component resources.
	/// </summary>
	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}

	#endregion
}