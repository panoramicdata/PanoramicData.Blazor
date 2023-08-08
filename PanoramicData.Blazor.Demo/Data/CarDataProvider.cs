namespace PanoramicData.Blazor.Demo.Data;

public class CarDataProvider : DataProviderBase<Car>
{
	private List<Car> _cars = new();

	public CarDataProvider()
	{
		_cars = new()
		{
			new Car { Make = "Ford", Model = "Model T", FromYear = 1908, ToYear = 1927 },
			new Car { Make = "Ford", Model = "GT40", FromYear = 1964, ToYear = 1969 },
			new Car { Make = "Ford", Model = "Mustang", FromYear = 1965 },
			new Car { Make = "Ford", Model = "Ranger", FromYear = 2011 },
			new Car { Make = "Audi", Model = "R8", FromYear = 2006 },
			new Car { Make = "Audi", Model = "Quattro", FromYear = 1980, ToYear = 1991 },
			new Car { Make = "Audi", Model = "TT", FromYear = 1998, ToYear = 2023 },
			new Car { Make = "Vauxhall", Model = "Calibra", FromYear = 1989, ToYear = 1997 },
			new Car { Make = "Vauxhall", Model = "Chevette", FromYear = 1975, ToYear = 1984 },
			new Car { Make = "Vauxhall", Model = "Astra", FromYear = 1980 },
			new Car { Make = "BMW", Model = "M3", FromYear = 1986 },
			new Car { Make = "BMW", Model = "330i", FromYear = 2004, ToYear = 2011 },
			new Car { Make = "Jaguar", Model = "XJ-S", FromYear = 1975, ToYear = 1996 },
			new Car { Make = "Jaguar", Model = "Mark 2", FromYear = 1959, ToYear = 1967 },
			new Car { Make = "Jaguar", Model = "E Type", FromYear = 1961, ToYear = 1974 },
			new Car { Make = "Jaguar", Model = "XJ220", FromYear = 1992, ToYear = 1994 },
			new Car { Make = "Dodge", Model = "Viper", FromYear = 1991, ToYear = 2017 },
			new Car { Make = "Porsche", Model = "911", FromYear = 1964 },
			new Car { Make = "Porsche", Model = "944", FromYear = 1982, ToYear = 1991 },
			new Car { Make = "Ferrari", Model = "Testarossa", FromYear = 1984, ToYear = 1996 },
			new Car { Make = "Ferrari", Model = "F40", FromYear = 1987, ToYear = 1992 },
			new Car { Make = "Ferrari", Model = "360", FromYear = 1999, ToYear = 2004 },
			new Car { Make = "Lamborghini", Model = "Countach", FromYear = 1974, ToYear = 1990 },
			new Car { Make = "Lamborghini", Model = "Gallardo", FromYear = 2003, ToYear = 2013 },
			new Car { Make = "Lamborghini", Model = "Diablo", FromYear = 1990, ToYear = 2001 },
			new Car { Make = "McLaren", Model = "F1", FromYear = 1992, ToYear = 1998 }
		};
	}

	public override Task<DataResponse<Car>> GetDataAsync(DataRequest<Car> request, CancellationToken cancellationToken)
	{
		var query = _cars.AsQueryable();

		// apply sort
		if (request.SortFieldExpression != null)
		{
			query = request.SortDirection == SortDirection.Ascending
				? query.OrderBy(request.SortFieldExpression)
				: query.OrderByDescending(request.SortFieldExpression);
		}

		// apply paging
		if (request?.Skip > 0)
		{
			query = query.Skip(request.Skip.Value);
		}
		if (request?.Take > 0)
		{
			query = query.Take(request.Take.Value);
		}

		// perform query
		var items = query.ToList();
		var response = new DataResponse<Car>(items, _cars.Count);

		return Task.FromResult(response);
	}
}
