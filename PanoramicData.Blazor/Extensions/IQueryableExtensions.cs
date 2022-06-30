using PanoramicData.Blazor.Attributes;

namespace PanoramicData.Blazor.Extensions;

public static class IQueryableExtensions
{
	public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, Filter filter, IKeyedCollection<string>? keyProperties = null)
	{
		try
		{
			// lookup property name for given filter key
			var properties = typeof(T).GetProperties();
			var propertyName = properties.SingleOrDefault(p => FilterKeyAttribute.Get(p) == filter.Key)?.Name
					?? (keyProperties is null
						? filter.Key
						: keyProperties.Get(filter.Key, filter.Key));

			if (!string.IsNullOrWhiteSpace(propertyName))
			{
				var parameters = filter.FilterType switch
				{
					FilterTypes.In => filter.Value.Split(new[] { "|" }, System.StringSplitOptions.RemoveEmptyEntries),
					FilterTypes.Range => new[] { filter.Value, filter.Value2 },
					_ => new object[] { filter.Value }
				};
				var predicate = filter.FilterType switch
				{
					FilterTypes.Contains => $"({propertyName}).Contains(@0)",
					FilterTypes.DoesNotContain => $"!({propertyName}).Contains(@0)",
					FilterTypes.DoesNotEqual => $"{propertyName} != @0",
					FilterTypes.EndsWith => $"({propertyName}).EndsWith(@0)",
					FilterTypes.Equals => $"{propertyName} == @0",
					FilterTypes.StartsWith => $"({propertyName}).StartsWith(@0)",
					FilterTypes.In => string.Join(" || ", parameters.Select((x, i) => $"{propertyName} == @{i}").ToArray()),
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

	//public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<Filter> filters, IKeyedCollection<string>? keyProperties = null)
	//{

	//}
}
