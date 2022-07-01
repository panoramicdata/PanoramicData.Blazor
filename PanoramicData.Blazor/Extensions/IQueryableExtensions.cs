namespace PanoramicData.Blazor.Extensions;

public static class IQueryableExtensions
{
	public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, Filter filter, IDictionary<string, string>? keyProperyMappings = null)
	{
		try
		{
			// must have key
			if (string.IsNullOrWhiteSpace(filter.Key))
			{
				return query;
			}

			// determine property name to use in query
			if (string.IsNullOrEmpty(filter.PropertyName))
			{
				if (keyProperyMappings != null && keyProperyMappings.ContainsKey(filter.Key))
				{
					filter.PropertyName = keyProperyMappings[filter.Key];
				}
				else
				{
					// search entity properties for matching key attribute
					// Note - currently this does NOT perform a nested serach
					var entityProperties = typeof(T).GetProperties();
					var propertyInfo = entityProperties.SingleOrDefault(x => x.GetFilterKey() == filter.Key || x.GetDisplayShortName() == filter.Key);
					if (propertyInfo != null)
					{
						filter.PropertyName = propertyInfo.Name;
					}
					else
					{
						// fallback is to simply use key
						filter.PropertyName = filter.Key.UpperFirstChar();
					}
				}
			}

			// apply query only if property name is known
			if (!string.IsNullOrWhiteSpace(filter.PropertyName))
			{
				var parameters = filter.FilterType switch
				{
					FilterTypes.In => filter.Value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries),
					FilterTypes.Range => new[] { filter.Value, filter.Value2 },
					_ => new object[] { filter.Value }
				};
				var predicate = filter.FilterType switch
				{
					FilterTypes.Contains => $"{filter.PropertyName} != null and ({filter.PropertyName}).Contains(@0)",
					FilterTypes.DoesNotContain => $"{filter.PropertyName} != null and !({filter.PropertyName}).Contains(@0)",
					FilterTypes.DoesNotEqual => $"{filter.PropertyName} != @0",
					FilterTypes.EndsWith => $"{filter.PropertyName} != null and ({filter.PropertyName}).EndsWith(@0)",
					FilterTypes.Equals => $"{filter.PropertyName} == @0",
					FilterTypes.StartsWith => $"{filter.PropertyName} != null and ({filter.PropertyName}).StartsWith(@0)",
					FilterTypes.In => string.Join(" || ", parameters.Select((x, i) => $"{filter.PropertyName} == @{i}").ToArray()),
					FilterTypes.GreaterThan => $"{filter.PropertyName} > @0",
					FilterTypes.GreaterThanOrEqual => $"{filter.PropertyName} >= @0",
					FilterTypes.LessThanOrEqual => $"{filter.PropertyName} <= @0",
					FilterTypes.LessThan => $"{filter.PropertyName} < @0",
					FilterTypes.Range => $"{filter.PropertyName} >= @0 and {filter.PropertyName} <= @1",
					FilterTypes.IsNull => $"{filter.PropertyName} == null",
					FilterTypes.IsNotNull => $"{filter.PropertyName} != null",
					FilterTypes.IsEmpty => $"{filter.PropertyName} == \"\"",
					FilterTypes.IsNotEmpty => $"{filter.PropertyName} != \"\"",
					_ => ""
				};
				return string.IsNullOrWhiteSpace(predicate) ? query : query.Where(predicate, parameters);
			}
		}
		catch
		{
			// invalid property
		}
		return query;
	}

	public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<Filter> filters, IDictionary<string, string>? keyProperties = null)
	{
		foreach (var filter in filters)
		{
			query = query.ApplyFilter(filter, keyProperties);
		}
		return query;
	}
}
