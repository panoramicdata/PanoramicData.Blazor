namespace PanoramicData.Blazor.Extensions;

public static class IQueryableExtensions
{
	public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, Filter filter, IDictionary<string, string>? keyPropertyMappings = null)
	{
		// uses dynamic LINQ to build queries : https://dynamic-linq.net/advanced-null-propagation

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
				if (keyPropertyMappings != null && keyPropertyMappings.TryGetValue(filter.Key, out string? value))
				{
					filter.PropertyName = value;
				}
				else
				{
					// search entity properties for matching key attribute
					// Note - currently this does NOT perform a nested search
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
				string[] separatorArray = ["|"];
				object[] parameters = filter.FilterType switch
				{
					FilterTypes.In => [.. filter.Value.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())],
					FilterTypes.NotIn => [.. filter.Value.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())],
					FilterTypes.Range => [filter.Value.RemoveQuotes(), filter.Value2.RemoveQuotes()],
					FilterTypes.IsEmpty => [string.Empty],
					FilterTypes.IsNotEmpty => [string.Empty],
					_ => [filter.Value.RemoveQuotes()]
				};

				// build dynamic predicate
				var propertyName = filter.PropertyName;

				// if using nested properties - surround by null propagating function
				if (propertyName.Contains('.'))
				{
					propertyName = $"np({propertyName})";
				}

				var predicate = filter.FilterType switch
				{
					FilterTypes.Contains => $"{propertyName} != null and ({propertyName}).Contains(@0)",
					FilterTypes.DoesNotContain => $"{propertyName} != null and !({propertyName}).Contains(@0)",
					FilterTypes.DoesNotEqual => $"{propertyName} != @0",
					FilterTypes.EndsWith => $"{propertyName} != null and ({propertyName}).EndsWith(@0)",
					FilterTypes.Equals => $"{propertyName} == @0",
					FilterTypes.StartsWith => $"{propertyName} != null and ({propertyName}).StartsWith(@0)",
					FilterTypes.In => string.Join(" || ", parameters.Select((x, i) => $"{propertyName} == @{i}").ToArray()),
					FilterTypes.NotIn => string.Join(" && ", parameters.Select((x, i) => $"{propertyName} != @{i}").ToArray()),
					FilterTypes.GreaterThan => $"{propertyName} > @0",
					FilterTypes.GreaterThanOrEqual => $"{propertyName} >= @0",
					FilterTypes.LessThanOrEqual => $"{propertyName} <= @0",
					FilterTypes.LessThan => $"{propertyName} < @0",
					FilterTypes.Range => $"{propertyName} >= @0 and {propertyName} <= @1",
					FilterTypes.IsNull => $"{propertyName} == null",
					FilterTypes.IsNotNull => $"{propertyName} != null",
					FilterTypes.IsEmpty => $"{propertyName} == \"\"",
					FilterTypes.IsNotEmpty => $"{propertyName} != \"\"",
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
