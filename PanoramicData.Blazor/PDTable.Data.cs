namespace PanoramicData.Blazor;

public partial class PDTable<TItem>
{
	/// <summary>
	/// Refresh the grid by performing a re-query.
	/// </summary>
	public Task RefreshAsync() => RefreshAsync(null);

	/// <summary>
	/// Refresh the grid by performing a re-query.
	/// </summary>
	/// <param name="searchText">Optional override for the search text.</param>
	public Task RefreshAsync(string? searchText)
		=> GetDataAsync(searchText);

	/// <summary>
	/// Instructs the component to show the specified page.
	/// </summary>
	/// <param name="pageCriteria">Details of the page to be displayed.</param>
	public async Task PageAsync(PageCriteria pageCriteria)
	{
		SetPageCriteria(pageCriteria);
		await GetDataAsync().ConfigureAwait(true);
		await PageChanged.InvokeAsync(pageCriteria).ConfigureAwait(true);
	}

	/// <summary>
	/// Sort the displayed items.
	/// </summary>
	/// <param name="sortCriteria">Details of the sort operation to be performed.</param>
	public Task SortAsync(SortCriteria sortCriteria)
	{
		var column = Columns.SingleOrDefault(c => string.Equals(c.PropertyInfo?.Name, sortCriteria.Key, StringComparison.OrdinalIgnoreCase));
		if (column != null)
		{
			return SortByAsync(column, sortCriteria.Direction);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Requests data from the data provider using the current settings.
	/// </summary>
	protected async Task GetDataAsync()
		=> await GetDataAsync(null);

	/// <summary>
	/// Identifies the view a fetch is for: its search text, sort and page. Two fetches with the same key
	/// differ only in when they ran, so the second is a refresh.
	/// </summary>
	private string GetViewKey(string? searchText, PDColumn<TItem>? sortColumn)
		=> $"{searchText}\u001f{sortColumn?.Id}\u001f{sortColumn?.SortDirection}\u001f{PageCriteria?.Page}\u001f{PageCriteria?.PageSize}";

	/// <summary>
	/// After a refresh, drops selected keys whose rows are no longer present, raising
	/// <see cref="SelectionChanged"/> only when that actually changed the selection.
	/// </summary>
	/// <remarks>
	/// Skipped when <see cref="RetainSelectionOnPage"/> is set: that selection deliberately spans pages, so
	/// a key missing from the current page is not evidence that its row has gone.
	/// </remarks>
	private async Task PruneSelectionAsync()
	{
		if (RetainSelectionOnPage || KeyField is null || Selection.Count == 0)
		{
			return;
		}

		var presentKeys = ItemsToDisplay
			.Select(x => KeyField(x)?.ToString() ?? string.Empty)
			.ToHashSet(StringComparer.Ordinal);

		if (Selection.RemoveAll(key => !presentKeys.Contains(key)) > 0)
		{
			await SelectionChanged.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Requests data from the data provider using the current settings.
	/// </summary>
	/// <param name="searchText">Optional override for the search text.</param>
	protected async Task GetDataAsync(string? searchText)
	{
		try
		{
			// Provide a means to cancel the refresh
			_cancellationTokenSource = new CancellationTokenSource();

			if (ShowOverlay)
			{
				BlockOverlayService.Show();
			}

			await BeforeFetch.InvokeAsync();

			var sortColumn = Columns.Find(IsSortColumn);
			var request = BuildDataRequest(searchText, sortColumn);

			// A fetch for the same search, sort and page as the last one is a refresh, not a new view:
			// it keeps the selection rather than clearing it (issue #151). Clearing it on every refresh
			// deselected the user's row each time an auto-refreshing page re-queried.
			var viewKey = GetViewKey(request.SearchText, sortColumn);
			var isRefresh = RetainSelectionOnRefresh && viewKey == _lastViewKey;

			// Clear selection
			if (!isRefresh && !RetainSelectionOnPage)
			{
				await ClearSelectionAsync().ConfigureAwait(true);
			}

			// Perform query data
			var response = await DataProvider
				.GetDataAsync(request, _cancellationTokenSource.Token)
				.ConfigureAwait(true);

			await ApplyDataResponseAsync(response, viewKey, isRefresh).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
		}
		finally
		{
			await EndFetchAsync().ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Builds the data request for the current sort, page and search text.
	/// </summary>
	private DataRequest<TItem> BuildDataRequest(string? searchText, PDColumn<TItem>? sortColumn)
	{
		var request = new DataRequest<TItem>
		{
			Skip = 0,
			ForceUpdate = false,
			SortFieldExpression = sortColumn?.Field,
			SortDirection = sortColumn?.SortDirection,
			SearchText = searchText ?? SearchText
		};

		// Paging
		if (PageCriteria != null)
		{
			request.Take = (int)PageCriteria.PageSize;
			request.Skip = (int)PageCriteria.PreviousItems;
		}

		return request;
	}

	/// <summary>
	/// Displays the items of a data response, then updates the selection and pager state.
	/// </summary>
	private async Task ApplyDataResponseAsync(DataResponse<TItem> response, string viewKey, bool isRefresh)
	{
		// Allow calling application to filter/add items etc.
		var items = new List<TItem>(response.Items);
		ItemsLoaded?.Invoke(items); // must use an action here and not an EventCallaback as that leads to infinite loop and 100% CPU
		ItemsToDisplay = items;
		_lastViewKey = viewKey;

		if (isRefresh)
		{
			await PruneSelectionAsync().ConfigureAwait(true);
		}

		// Update pager state
		if (PageCriteria != null)
		{
			PageCriteria.TotalCount = (uint)(response.TotalCount ?? 0);
		}
	}

	/// <summary>
	/// Releases the fetch's cancellation source, hides the overlay and raises <see cref="AfterFetch"/>.
	/// </summary>
	private async Task EndFetchAsync()
	{
		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = null;

		if (ShowOverlay)
		{
			BlockOverlayService.Hide();
		}

		await AfterFetch.InvokeAsync();
	}

	/// <summary>
	/// Sort the data by the specified column.
	/// </summary>
	/// <param name="column">The column to sort by.</param>
	/// <remarks>To disable sorting for any given column, set its Sortable property set to false.</remarks>

	protected async Task SortByAsync(PDColumn<TItem> column)
	{
		await SortByAsync(column, null);
	}

	/// <summary>
	/// Sorts the data by the specified column and optional direction override.
	/// </summary>
	/// <param name="column">The column to sort.</param>
	/// <param name="direction">Optional direction override.</param>
	protected async Task SortByAsync(PDColumn<TItem> column, SortDirection? direction)
	{
		if (column.Sortable && !string.IsNullOrWhiteSpace(column.Id))
		{
			ApplySortDirection(column, direction);

			if (SortCriteria != null)
			{
				SortCriteria.Key = column.Id;
				SortCriteria.Direction = column.SortDirection;
			}

			await GetDataAsync().ConfigureAwait(true);
			await SortChanged.InvokeAsync(new SortCriteria { Key = column.Id, Direction = direction ?? column.SortDirection }).ConfigureAwait(true);

			await ScrollColumnIntoViewAsync(column).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Sets the sort direction of the column being sorted on: the requested direction if given, otherwise
	/// the reverse of its current direction if it is already the sort column, otherwise its default.
	/// </summary>
	private void ApplySortDirection(PDColumn<TItem> column, SortDirection? direction)
	{
		// If direction specified then sort as requested
		if (direction.HasValue)
		{
			column.SortDirection = direction.Value;
		}
		// If column already sorted then reverse direction
		else if (IsSortColumn(column))
		{
			column.SortDirection = column.SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
		}
		else
		{
			var previousCol = Columns.Find(IsSortColumn);
			if (previousCol != null)
			{
				previousCol.SortDirection = SortDirection.None;
			}

			column.SortDirection = column.DefaultSortDirection != SortDirection.None
				? column.DefaultSortDirection
				: GetInitialSortDirection(column);
		}
	}

	/// <summary>
	/// Gets the direction a column without a default direction is first sorted in: ascending for text,
	/// descending for anything else.
	/// </summary>
	private static SortDirection GetInitialSortDirection(PDColumn<TItem> column)
	{
		var defaultSortDirection = SortDirection.Ascending;
		if (column.Field != null)
		{
			var info = column.Field.GetPropertyMemberInfo();
			if (info?.GetMemberUnderlyingType()?.FullName != typeof(string).FullName)
			{
				defaultSortDirection = SortDirection.Descending;
			}
		}

		return defaultSortDirection;
	}

	/// <summary>
	/// Ensures the column sorted on is still in view.
	/// </summary>
	private async Task ScrollColumnIntoViewAsync(PDColumn<TItem> column)
	{
		if (_commonModule != null)
		{
			// Using query selector that allows column names to be non-unique - i.e default col ids are col-1, col-2 etc.
			try
			{
				await _commonModule.InvokeVoidAsync("scrollIntoViewEx", $"#{Id} #{column.Id}", "smooth", "nearest", "center");
			}
			catch (ObjectDisposedException)
			{
				// Silently handle disposal during async operation
			}
		}
	}

	private async void PageCriteria_PageSizeChanged(object? sender, EventArgs e)
	{
		await RefreshAsync(SearchText).ConfigureAwait(true);
		if (PageCriteria != null)
		{
			await PageSizeChanged.InvokeAsync(PageCriteria).ConfigureAwait(true);
		}

		StateHasChanged();
	}

	private async void PageCriteria_PageChanged(object? sender, EventArgs e)
	{
		if (PageCriteria != null)
		{
			await RefreshAsync(SearchText).ConfigureAwait(true);
			await PageChanged.InvokeAsync(PageCriteria).ConfigureAwait(true);
			StateHasChanged();
		}
	}
}
