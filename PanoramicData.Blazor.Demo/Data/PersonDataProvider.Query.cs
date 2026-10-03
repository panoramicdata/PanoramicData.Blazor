namespace PanoramicData.Blazor.Demo.Data;

/// <summary>
/// Searching, filtering, sorting and paging of the demo people.
/// </summary>
public partial class PersonDataProvider
{
	public bool SlowSearch { get; set; }

	public override async Task<DataResponse<Person>> GetDataAsync(DataRequest<Person> request, CancellationToken cancellationToken)
	{
		var total = _people.Count;
		var items = new List<Person>();

		if (SlowSearch)
		{
			await SimulateSlowSearchAsync(cancellationToken).ConfigureAwait(false);
		}

		await Task.Run(() =>
		{
			// apply search criteria and get a total count of matching items
			var query = ApplySearch(_people.AsQueryable(), request.SearchText);

			total = query.Count();

			// apply sort and paging, then realize query
			items = [.. ApplySortAndPaging(query, request)];

		}, cancellationToken).ConfigureAwait(false);
		return new DataResponse<Person>(items, total);
	}

	private async Task SimulateSlowSearchAsync(CancellationToken cancellationToken)
	{
		try
		{
			if (AddDelay)
			{
				await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
			}
		}
		catch (TaskCanceledException)
		{
			// Nothing to do...
		}
	}

	private IQueryable<Person> ApplySearch(IQueryable<Person> query, string? searchText)
	{
		if (string.IsNullOrWhiteSpace(searchText))
		{
			return query;
		}

		var filters = Filter.ParseMany(searchText, KeyPropertyMappings).ToArray();
		if (filters.Length == 0)
		{
			// basic filtering
			return query.Where(x => (x.FirstName != null && x.FirstName.Contains(searchText)) || x.LastName.Contains(searchText));
		}

		// column filtering
		// example: 'last:Smith*' -> will search LastName property for values starting with Smith
		// note: As derived from DataProviderBase all columns
		// have their Id mapped to the Field name so we need to prevent user
		// from being able to type a search term that will query the Password field
		return ApplyFilters(query, filters, "password");
	}

	private static IQueryable<Person> ApplySortAndPaging(IQueryable<Person> query, DataRequest<Person> request)
	{
		// apply sort
		if (request.SortFieldExpression != null)
		{
			query = request.SortDirection == SortDirection.Descending
				? query.OrderByDescending(request.SortFieldExpression)
				: query.OrderBy(request.SortFieldExpression);
		}

		// apply paging
		if (request.Skip.HasValue)
		{
			query = query.Skip(request.Skip.Value);
		}

		if (request.Take.HasValue)
		{
			query = query.Take(request.Take.Value);
		}

		return query;
	}

	public override IQueryable<Person> ApplyFilter(IQueryable<Person> query, Filter filter)
	{
		if (filter.Key == "age")
		{
			// example of applying a custom / calculated filter on the dataset
			var birthYear = DateTime.Now.Date.Year - int.Parse(filter.Value);
			return query.Where(x => x.Dob!.Value.Year == birthYear);
		}
		else
		{
			return base.ApplyFilter(query, filter);
		}
	}
}
