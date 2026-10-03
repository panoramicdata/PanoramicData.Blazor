using System.Security.Cryptography;

namespace PanoramicData.Blazor.Demo.Data;

public class PersonDataProvider : DataProviderBase<Person>
{
	private static readonly string _loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce at leo eu risus faucibus facilisis quis in tortor. Phasellus gravida libero sit amet ullamcorper rhoncus. Ut at viverra lectus. Vestibulum mi eros, egestas vel nulla at, lacinia ornare mauris. Morbi a pulvinar lacus. Praesent ut convallis magna. Etiam est sem, feugiat a leo in, viverra scelerisque lectus. Vivamus dictum luctus eros non ultrices. Curabitur enim enim, porta eu lorem ut, varius venenatis sem.";
	private static readonly string[] _firstNames = ["Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy", "Mike"];
	private static readonly string[] _lastNames = ["Smith", "Cooper", "Watkins", "Jenkins", "Van Holden", "Williams", "Jones", "Smithson", "Carter", "Miller", "Baker"];
	private static readonly List<Person> _people = [];
	public static readonly string[] Locations = ["Paris", "Rome", "Milan", "New York", "Peckham", "Sydney"];

	public PersonDataProvider() : this(255) { }

	/// <summary>
	/// Add a small delay to simulate network latency.
	/// </summary>
	public bool AddDelay { get; set; }

	public static List<Person> GetAllPersons()
	{
		return _people;
	}

	public PersonDataProvider(int count)
	{
		// generate random rows
		if (_people.Count == 0)
		{
			foreach (var id in Enumerable.Range(1, count))
			{
				_people.Add(CreateRandomPerson(id));
			}
		}
	}

	private static Person CreateRandomPerson(int id)
	{
		var boss1 = new Person
		{
			FirstName = "Peter",
			LastName = "Simmons"
		};
		var boss2 = new Person
		{
			FirstName = "Lucy",
			LastName = "Waterman"
		};
		var person = new Person
		{
			Id = id,
			AllowLogin = RandomNumberGenerator.GetInt32(0, 2) == 1,
			DateCreated = DateTimeOffset.Now.AddDays(RandomNumberGenerator.GetInt32(-365, 0)),
			DateModified = RandomNumberGenerator.GetInt32(10) < 3 ? null : DateTimeOffset.Now.AddDays(RandomNumberGenerator.GetInt32(-30, 0)),
			Department = (Departments)RandomNumberGenerator.GetInt32(0, 4),
			FirstName = RandomNumberGenerator.GetInt32(10) < 2 ? null : _firstNames[RandomNumberGenerator.GetInt32(_firstNames.Length)],
			LastName = _lastNames[RandomNumberGenerator.GetInt32(_lastNames.Length)],
			Location = RandomNumberGenerator.GetInt32(Locations.Length),
			Dob = DateTime.Today.AddYears(-RandomNumberGenerator.GetInt32(20, 50)),
			Comments = _loremIpsum[..RandomNumberGenerator.GetInt32(0, _loremIpsum.Length)],
			Password = "Password",
			IsFirstAider = RandomNumberGenerator.GetInt32(0, 4) switch { 0 => true, 1 => false, _ => null },
			Dependents = RandomNumberGenerator.GetInt32(0, 2) == 0 ? null : RandomNumberGenerator.GetInt32(1, 4),
		};
		List<Person?> managers = [boss1, boss2, null];
		person.Manager = managers[RandomNumberGenerator.GetInt32(0, 3)]!;
		person.Email = RandomNumberGenerator.GetInt32(10) < 2
			? string.Empty
			: $"{person.FirstName?.ToLowerInvariant() ?? _firstNames[RandomNumberGenerator.GetInt32(_firstNames.Length)].ToLowerInvariant()}.{person.LastName.ToLowerInvariant()}@acme.com";
		return person;
	}

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

	/// <summary>
	/// Requests that the item is deleted.
	/// </summary>
	/// <param name="item">The item to be deleted.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override async Task<OperationResponse> DeleteAsync(Person item, CancellationToken cancellationToken)
	{
		if (AddDelay)
		{
			await Task.Delay(1000, cancellationToken);
		}

		var existingPerson = _people.Find(x => x.Id == item.Id);
		if (existingPerson == null)
		{
			return new OperationResponse { ErrorMessage = $"Person not found (id {item.Id})" };
		}

		_people.Remove(existingPerson);
		return new OperationResponse { Success = true };
	}

	/// <summary>
	/// Requests the given item is updated by applying the given delta.
	/// </summary>
	/// <param name="item">The original item to be updated.</param>
	/// <param name="delta">A dictionary with new property values.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override async Task<OperationResponse> UpdateAsync(Person item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
		if (AddDelay)
		{
			await Task.Delay(1000, cancellationToken);
		}


		var existingPerson = _people.Find(x => x.Id == item.Id);
		if (existingPerson == null)
		{
			return new OperationResponse { ErrorMessage = $"Person not found (id {item.Id})" };
		}

		foreach (var kvp in delta)
		{
			var prop = item.GetType().GetProperty(kvp.Key);
			if (prop == null)
			{
				return new OperationResponse { ErrorMessage = $"Person does not contain a property named {kvp.Key}" };
			}
			else
			{
				try
				{
					var value = kvp.Value.Cast(prop.PropertyType);
					prop.SetValue(existingPerson, value);
				}
				catch (Exception ex)
				{
					return new OperationResponse { ErrorMessage = $"Failed to update property {kvp.Key} to {kvp.Value}: {ex.Message}" };
				}
			}
		}

		existingPerson.DateModified = DateTime.Now;
		return new OperationResponse { Success = true };
	}

	/// <summary>
	/// Requests the given item is created.
	/// </summary>
	/// <param name="item">New item details.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override async Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
	{
		if (AddDelay)
		{
			await Task.Delay(1000, cancellationToken);
		}

		item.Id = _people.Max(x => x.Id) + 1;
		item.DateModified = item.DateCreated = DateTime.Now;
		_people.Add(item);
		return new OperationResponse { Success = true };
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
