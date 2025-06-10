namespace PanoramicData.Blazor;
public partial class PDComboBox<TItem>
{
	[Parameter] required public List<TItem> Items { get; set; }
	[Parameter] required public Func<TItem, string> ItemToString { get; set; }
	[Parameter] public EventCallback SelectedItemChanged { get; set; }
	[Parameter] required public TItem SelectedItem { get; set; }
	[Parameter] public string Placeholder { get; set; } = "Type to search...";
	[Parameter] public Func<TItem, object>? OrderBy { get; set; }
	[Parameter] public int MaxResults { get; set; } = 5;
	[Parameter] public bool IsDisabled { get; set; }
	[Parameter] public bool IsReadOnly { get; set; }
	[Parameter] public string NoResultsText { get; set; } = "No results found";
	[Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

	private string _searchText = "";
	private List<TItem> _filteredItems = [];
	private bool _showDropdown;
	private CancellationTokenSource? _blurToken;
	private int _activeIndex = -1;
	private bool _suppressOnInput;

	public string CssClass { get; set; } = "default-combobox";

	private void FilterItems(ChangeEventArgs e)
	{
		if (_suppressOnInput)
		{
			return;
		}

		_searchText = e.Value?.ToString() ?? string.Empty;
		_searchText = _searchText.Trim();

		var query = Items
			.Where(i => ItemToString(i).Contains(_searchText, StringComparison.OrdinalIgnoreCase));

		if (OrderBy is not null)
		{
			query = query.OrderBy(OrderBy);
		}
		else
		{
			query = query.OrderBy(ItemToString);
		}

		_filteredItems = [.. query.Take(MaxResults)];
		_showDropdown = true;
		_activeIndex = _filteredItems.Count > 0 ? 0 : -1;
	}

	private void SelectItem(TItem item)
	{
		SelectedItem = item;
		_suppressOnInput = true;
		_searchText = ItemToString(item);
		_suppressOnInput = false;
		_filteredItems.Clear();
		_showDropdown = false;
		_activeIndex = -1;
		SelectedItemChanged.InvokeAsync(item);
	}

	private void ShowDropdown()
	{
		_showDropdown = true;
	}

	private async void HideDropdownWithDelay()
	{
		_blurToken?.Cancel();
		_blurToken = new CancellationTokenSource();

		try
		{
			await Task.Delay(200, _blurToken.Token);
			_showDropdown = false;
			StateHasChanged();
		}
		catch (TaskCanceledException) { }
	}

	private void ClearInput()
	{
		_searchText = "";
		_filteredItems.Clear();
		_activeIndex = -1;
		SelectedItem = default!;
		SelectedItemChanged.InvokeAsync(SelectedItem);
	}

	private void OnInputKeyDown(KeyboardEventArgs e)
	{
		// Always handle Escape to close the dropdown and revert to selected item
		if (e.Key == "Escape")
		{
			_showDropdown = false;
			_suppressOnInput = true;
			_searchText = SelectedItem is not null ? ItemToString(SelectedItem) : "";
			_suppressOnInput = false;
			_filteredItems.Clear();
			_activeIndex = -1;
			StateHasChanged();
			return;
		}

		// If up/down is pressed and dropdown is not open, open and filter
		if ((e.Key == "ArrowDown" || e.Key == "ArrowUp") && (!_showDropdown || _filteredItems.Count == 0))
		{
			FilterItems(new ChangeEventArgs { Value = _searchText });
			_showDropdown = true;
			_activeIndex = _filteredItems.Count > 0 ? 0 : -1;
			StateHasChanged();
			return;
		}

		if (!_showDropdown || _filteredItems.Count == 0)
		{
			return;
		}

		var shouldUpdate = false;

		switch (e.Key)
		{
			case "ArrowDown":
				_activeIndex = Math.Min(_activeIndex + 1, _filteredItems.Count - 1);
				shouldUpdate = true;
				break;
			case "ArrowUp":
				_activeIndex = Math.Max(_activeIndex - 1, 0);
				shouldUpdate = true;
				break;
			case "Enter":
				if (_activeIndex >= 0 && _activeIndex < _filteredItems.Count)
				{
					SelectItem(_filteredItems[_activeIndex]);
					shouldUpdate = true;
				}

				break;
		}

		if (shouldUpdate)
		{
			StateHasChanged();
		}
	}
}