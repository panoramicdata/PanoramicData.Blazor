namespace PanoramicData.Blazor;

public partial class PDComboBox<TItem> : IAsyncDisposable
{
	// Mandatory parameters
	[Parameter]
	[EditorRequired]
	public required List<TItem> Items { get; set; }

	// Optional parameters
	[Parameter]
	public required EventCallback<TItem> SelectedItemChanged { get; set; }

	[Parameter]
	public Func<TItem, string> ItemToString { get; set; } = item => item?.ToString() ?? string.Empty;

	[Parameter]
	public required Func<TItem, string> ItemToId { get; set; } = item => item?.ToString() ?? string.Empty;

	[Parameter]
	public required Func<TItem, string, bool> Filter { get; set; } = (item, searchText) => item?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false;

	[Parameter]
	public TItem? SelectedItem { get; set; }

	[Parameter]
	public string Placeholder { get; set; } = "Type to search...";

	[Parameter]
	public Func<TItem, object>? OrderBy { get; set; }

	[Parameter]
	public int MaxResults { get; set; } = 5;

	[Parameter]
	public bool IsDisabled { get; set; }

	[Parameter]
	public bool IsReadOnly { get; set; }

	[Parameter]
	public string NoResultsText { get; set; } = "No results found";

	[Parameter]
	public RenderFragment<TItem>? ItemTemplate { get; set; }

	[Parameter]
	public RenderFragment<string>? NoResultsTemplate { get; set; }

	[Parameter]
	public bool ShowSelectedItemOnTop { get; set; } = false;

	// Internal state
	private string _searchText = "";
	private string _lastSearchText = "";
	private List<TItem> _filteredItems = [];
	private bool _showDropdown;
	private CancellationTokenSource? _blurToken;
	private int _activeIndex = -1;
	private bool _suppressOnInput;
	private ElementReference _inputRef;

	public string CssClass { get; set; } = "default-combobox";

	private IJSObjectReference? _jsModule;

	private bool _disposedValue;

	[Inject] private IJSRuntime JS { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			_jsModule = await JS.InvokeAsync<IJSObjectReference>(
				"import", "./_content/PanoramicData.Blazor/PDComboBox.razor.js");
		}
	}

	protected override void OnParametersSet()
	{
		if (SelectedItem is null)
		{
			_searchText = "";
		}
		else
		{
			_searchText = ItemToString(SelectedItem);
		}
	}

	private async Task BlurInputAsync()
	{
		if (_jsModule is not null)
		{
			await _jsModule.InvokeVoidAsync("blurInput", _inputRef);
		}
	}

	private void ApplyFilter(string searchText)
	{
		_searchText = searchText.Trim();

		var query = Items.Where(item => Filter(item, _searchText));

		if (OrderBy is not null)
		{
			query = query.OrderBy(OrderBy);
		}
		else
		{
			query = query.OrderBy(ItemToString);
		}

		var filtered = query.Take(MaxResults).ToList();

		if (ShowSelectedItemOnTop && SelectedItem is not null)
		{
			var selectedId = ItemToId(SelectedItem);
			if (!filtered.Any(item => ItemToId(item) == selectedId))
			{
				filtered.Insert(0, SelectedItem);
			}
		}

		_filteredItems = filtered; 
		_showDropdown = true;

		// Set _activeIndex to the selected item if present in the filtered list
		if (SelectedItem is not null)
		{
			var selectedId = ItemToId(SelectedItem);
			_activeIndex = _filteredItems.FindIndex(item => ItemToId(item) == selectedId);
		}
		else
		{
			_activeIndex = -1;
		}
	}

	private void FilterItems(ChangeEventArgs e)
	{
		if (_suppressOnInput)
		{
			return;
		}
		_searchText = e.Value?.ToString() ?? string.Empty;
		_searchText = _searchText.Trim();
		_lastSearchText = _searchText;
		ApplyFilter(e.Value?.ToString() ?? string.Empty);
	}

	private async Task SelectItem(TItem item)
	{
		SelectedItem = item;
		_suppressOnInput = true;
		_searchText = ItemToString(item);
		_suppressOnInput = false;
		_filteredItems.Clear();
		_showDropdown = false;
		_activeIndex = -1;
		await SelectedItemChanged.InvokeAsync(item);
	}

	private async Task FocusInputAsync()
	{
		if (_jsModule is not null)
		{
			await _jsModule.InvokeVoidAsync("selectInputText", _inputRef);
		}
	}

	private async Task ToggleDropdown()
	{
		if (_showDropdown)
		{
			_showDropdown = false;
			if (SelectedItem != null)
			{
				_searchText = ItemToString(SelectedItem);
			}
			StateHasChanged();
			return;
		}

		// Restore last search text for filtering
		var searchText = _lastSearchText;
		ApplyFilter(searchText);
		StateHasChanged();

		// Focus the input after dropdown is shown
		await FocusInputAsync();
	}

	private async Task OnInputBlur(FocusEventArgs e)
	{
		await HideDropdownWithDelay();
		await Task.CompletedTask;
	}

	private async Task HideDropdownWithDelay()
	{
		_blurToken?.Cancel();
		_blurToken?.Dispose();
		_blurToken = new CancellationTokenSource();

		try
		{
			await Task.Delay(400, _blurToken.Token);
			_showDropdown = false;
			if (SelectedItem is not null)
			{
				_searchText = ItemToString(SelectedItem);
			}
			await InvokeAsync(StateHasChanged);
		}
		catch (TaskCanceledException) { }
	}

	private async Task ClearInput()
	{
		_searchText = "";
		_lastSearchText = "";
		_filteredItems.Clear();
		_activeIndex = -1;
		
		if (_showDropdown)
		{
			ApplyFilter(string.Empty);
		}
		await FocusInputAsync();
	}

	private async Task OnInputKeyDown(KeyboardEventArgs e)
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
			await BlurInputAsync();
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
				_activeIndex = Math.Min(++_activeIndex, _filteredItems.Count - 1);
				shouldUpdate = true;
				break;
			case "ArrowUp":
				_activeIndex = Math.Max(--_activeIndex, 0);
				shouldUpdate = true;
				break;
			case "Enter":
				if (_activeIndex < 0 || _activeIndex >= _filteredItems.Count)
				{
					break;
				}

				await SelectItem(_filteredItems[_activeIndex]);
				await BlurInputAsync();
				shouldUpdate = true;

				break;
		}

		if (shouldUpdate)
		{
			StateHasChanged();
		}
	}

	private async Task OnInputFocus(FocusEventArgs e)
	{
		// Cancel any pending dropdown hide
		_blurToken?.Cancel();

		// Restore last search text for editing/filtering
		if (SelectedItem is not null && !string.IsNullOrEmpty(_lastSearchText))
		{
			_searchText = _lastSearchText;
			_showDropdown = true;
			ApplyFilter(_searchText);
			StateHasChanged();
		} 
		else
		{
			_searchText = "";
			_lastSearchText = "";
			_filteredItems.Clear();
			_activeIndex = -1;

			_showDropdown = true;
			ApplyFilter(_searchText);
		}
		if (_jsModule is not null && !string.IsNullOrEmpty(_searchText))
		{
			await _jsModule.InvokeVoidAsync("selectInputText", _inputRef);
		}
	}

	#region IAsyncDisposable

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		if (!_disposedValue)
		{
			_blurToken?.Cancel();
			_blurToken?.Dispose();
			_blurToken = null;

			if (_jsModule is not null)
			{
				try
				{
					await _jsModule.DisposeAsync();
				}
				catch
				{
					// Ignore JS dispose exceptions
				}
				_jsModule = null;
			}

			_disposedValue = true;
		}
	}

	#endregion
}