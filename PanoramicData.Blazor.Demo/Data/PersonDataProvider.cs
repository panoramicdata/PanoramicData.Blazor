namespace PanoramicData.Blazor.Demo.Data;

public class PersonDataProvider : DataProviderBase<Person>
{
	private static readonly string _loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce at leo eu risus faucibus facilisis quis in tortor. Phasellus gravida libero sit amet ullamcorper rhoncus. Ut at viverra lectus. Vestibulum mi eros, egestas vel nulla at, lacinia ornare mauris. Morbi a pulvinar lacus. Praesent ut convallis magna. Etiam est sem, feugiat a leo in, viverra scelerisque lectus. Vivamus dictum luctus eros non ultrices. Curabitur enim enim, porta eu lorem ut, varius venenatis sem.";
	private static readonly string[] _firstNames = ["Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy", "Mike"];
	private static readonly string[] _lastNames = ["Smith", "Cooper", "Watkins", "Jenkins", "Van Holden", "Williams", "Jones", "Smithson", "Carter", "Miller", "Baker"];
	private static readonly Random _random = new(System.Environment.TickCount);
	private static readonly List<Person> _people = [];
	public static readonly string[] Locations = ["Paris", "Rome", "Milan", "New York", "Peckham", "Sydney"];

	public PersonDataProvider() : this(255){}

	public PersonDataProvider(int count)
	{
		// generate random rows
		if (_people.Count == 0)
		{
			foreach (var id in Enumerable.Range(1, count))
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
					AllowLogin = _random.Next(0, 2) == 1,
					DateCreated = DateTimeOffset.Now.AddDays(_random.Next(-365, 0)),
					DateModified = _random.Next(10) < 3 ? null : DateTimeOffset.Now.AddDays(_random.Next(-30, 0)),
					Department = (Departments)_random.Next(0, 4),
					FirstName = _random.Next(10) < 2 ? null : _firstNames[_random.Next(_firstNames.Length)],
					LastName = _lastNames[_random.Next(_lastNames.Length)],
					Location = _random.Next(Locations.Length),
					Dob = DateTime.Today.AddYears(-_random.Next(20, 50)),
					Comments = _loremIpsum[.._random.Next(0, _loremIpsum.Length)],
					Password = "Password",
					IsFirstAider = _random.Next(0, 4) switch { 0 => true, 1 => false, _ => null },
					Dependents = _random.Next(0, 2) switch { 0 => (int?)null, _ => _random.Next(1, 4) },
				};
				var managers = new List<Person?>() { boss1, boss2, null };
				person.Manager = managers[_random.Next(0, 3)]!;
				person.Email = _random.Next(10) < 2
					? string.Empty
					: $"{person.FirstName?.ToLowerInvariant() ?? _firstNames[_random.Next(_firstNames.Length)].ToLowerInvariant()}.{person.LastName.ToLowerInvariant()}@acme.com";
				_people.Add(person);
			}
		}
	}

	public bool SlowSearch { get; set; }

	public override async Task<DataResponse<Person>> GetDataAsync(DataRequest<Person> request, CancellationToken cancellationToken)
	{
		var total = _people.Count;
		var items = new List<Person>();

		if (SlowSearch)
		{
			try
			{
				await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
			}
			catch (TaskCanceledException)
			{
				var a = 1;
			}
		}

		await Task.Run(() =>
		{
			var query = _people.AsQueryable();

			// apply search criteria and get a total count of matching items
			if (!string.IsNullOrWhiteSpace(request.SearchText))
			{
				var filters = Filter.ParseMany(request.SearchText, KeyPropertyMappings).ToArray();
				if (filters.Length == 0)
				{
					// basic filtering
					query = query.Where(x => (x.FirstName != null && x.FirstName.Contains(request.SearchText)) || x.LastName.Contains(request.SearchText));
				}
				else
				{
					// column filtering
					// example: 'last:Smith*' -> will search LastName property for values starting with Smith
					// note: As derived from DataProviderBase all columns
					// have their Id mapped to the Field name so we need to prevent user
					// from being able to type a search term that will query the Password field
					query = ApplyFilters(query, filters, "password");
				}
			}

			total = query.Count();

			// apply sort
			if (request.SortFieldExpression != null)
			{
				if (request.SortDirection != null && request.SortDirection == SortDirection.Descending)
				{
					query = query.OrderByDescending(request.SortFieldExpression);
				}
				else
				{
					query = query.OrderBy(request.SortFieldExpression);
				}
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

			// realize query
			items = [.. query];

		}, cancellationToken).ConfigureAwait(false);
		return new DataResponse<Person>(items, total);
	}

	//public override async Task<string[]> GetDistinctValuesAsync(DataRequest<Person> request, Expression<Func<Person, object>> field)
	//{
	//	var values = new List<string>();
	//	var c = field.Compile();
	//	await Task.Run(() =>
	//	{

	//		var values = _people.AsQueryable().Select(x => c.Invoke(x).ToString()).Distinct().Take(take).ToArray();
	//		values.AddRange(values);

	//	}).ConfigureAwait(true);
	//	return values.ToArray();
	//}

	/// <summary>
	/// Requests that the item is deleted.
	/// </summary>
	/// <param name="item">The item to be deleted.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override Task<OperationResponse> DeleteAsync(Person item, CancellationToken cancellationToken)
		=> Task.Run(() =>
			{
				var existingPerson = _people.Find(x => x.Id == item.Id);
				if (existingPerson == null)
				{
					return new OperationResponse { ErrorMessage = $"Person not found (id {item.Id})" };
				}

				_people.Remove(existingPerson);
				return new OperationResponse { Success = true };
			});

	/// <summary>
	/// Requests the given item is updated by applying the given delta.
	/// </summary>
	/// <param name="item">The original item to be updated.</param>
	/// <param name="delta">A dictionary with new property values.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override Task<OperationResponse> UpdateAsync(Person item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		=> Task.Run(() =>
			{

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
			});

	/// <summary>
	/// Requests the given item is created.
	/// </summary>
	/// <param name="item">New item details.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public override Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
		=> Task.Run(() =>
			{
				item.Id = _people.Max(x => x.Id) + 1;
				item.DateModified = item.DateCreated = DateTime.Now;
				_people.Add(item);
				return new OperationResponse { Success = true };
			});
}
