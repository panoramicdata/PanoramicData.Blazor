namespace PanoramicData.Blazor;

public partial class PDTable<TItem>
{
	private async Task OnFilterChanged()
	{
		var sb = new StringBuilder();
		foreach (var col in ActualColumnsToDisplay.Where(x => x.Filterable && x.Filter.IsValid))
		{
			sb.Append(' ').Append(col.Filter.ToString());
		}

		SearchText = sb.ToString().Trim();
		await SearchTextChanged.InvokeAsync(SearchText).ConfigureAwait(true);
		_lastSearchText = SearchText;

		await RefreshAsync(SearchText).ConfigureAwait(true);
	}

	private async Task<string[]> OnFetchFilterValuesAsync(PDColumn<TItem> column, Filter filter)
	{
		if (column.Field is null)
		{
			return [];
		}

		// Allow app to specify suggested values
		if (column.FilterSuggestedValues.Any())
		{
			return [.. column.FilterSuggestedValues.Select(x => Filter.Format(x, filter.UnspecifiedDateTimesAreUtc))];
		}

		var request = BuildFilterValuesRequest(column, filter);
		var objectValues = await FetchDistinctValuesAsync(column, column.Field, request).ConfigureAwait(true);
		return FormatFilterValues(column, objectValues);
	}

	/// <summary>
	/// Builds the request for a column's filter values, based on the current filters and sort.
	/// </summary>
	private DataRequest<TItem> BuildFilterValuesRequest(PDColumn<TItem> column, Filter filter)
	{
		var sortColumn = Columns.Find(IsSortColumn);
		return new DataRequest<TItem>
		{
			Take = 1000,
			ForceUpdate = false,
			SortFieldExpression = sortColumn?.Field,
			SortDirection = sortColumn?.SortDirection,
			SearchText = BuildFilterSearchText(column, filter)
		};
	}

	/// <summary>
	/// Limits a column's distinct values to the first N non-empty ones and formats them as text.
	/// </summary>
	private string[] FormatFilterValues(PDColumn<TItem> column, object[] objectValues)
	{
		// Limit to first N
		var limitedValues = objectValues
			.Where(x => x != null && x.ToString() != string.Empty)
			.Take(column.FilterMaxValues ?? FilterMaxValues);

		// Cast to string
		return [.. limitedValues.Select(x => string.IsNullOrEmpty(column.Format)
				? x.ToString() ?? string.Empty
				: string.Format(CultureInfo.CurrentCulture, "{0:" + column.Format + "}", x)).Distinct()];
	}

	/// <summary>
	/// Builds the search text from the filters of all displayed columns, using the given (not yet applied)
	/// filter for the column whose values are being fetched.
	/// </summary>
	private string BuildFilterSearchText(PDColumn<TItem> column, Filter filter)
	{
		var searchText = new StringBuilder();
		foreach (var col in ActualColumnsToDisplay.Where(c => c.Filterable))
		{
			var colFilter = col == column ? filter : col.Filter;
			if (colFilter.IsValid)
			{
				_ = searchText.Append(colFilter.ToString()).Append(' ');
			}
		}

		return searchText.ToString().Trim();
	}

	/// <summary>
	/// Fetches the distinct values of a column, from the filter provider when the data provider is one,
	/// otherwise from the main data provider (take has to be applied on the base query).
	/// </summary>
	private async Task<object[]> FetchDistinctValuesAsync(PDColumn<TItem> column, Expression<Func<TItem, object>> field, DataRequest<TItem> request)
	{
		// Use more efficient service provider?
		if (DataProvider is IFilterProviderService<TItem> filterService)
		{
			return await filterService.GetDistinctValuesAsync(request, field).ConfigureAwait(true);
		}

		// Use main data provider - take has to be applied on base query
		var response = await DataProvider.GetDataAsync(request, default);
		return [.. response.Items
			.Where(x => column.GetValue(x) != null)
			.Select(x => column.GetValue(x)!.ToString() ?? string.Empty)
			.Distinct()
			.OrderBy(x => x)];
	}
}
