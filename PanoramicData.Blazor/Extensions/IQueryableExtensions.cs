namespace PanoramicData.Blazor.Extensions;

public static class IQueryableExtensions
{
	public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, Filter filter, IKeyedCollection<string>? keyProperties = null)
	{
		try
		{
			var property = keyProperties is null
				? filter.Key
				: keyProperties.Get(filter.Key, filter.Key);
			if (!string.IsNullOrWhiteSpace(property))
			{
				var parameters = filter.FilterType switch
				{
					FilterTypes.In => filter.Value.Split(new[] { "|" }, System.StringSplitOptions.RemoveEmptyEntries),
					FilterTypes.Range => new[] { filter.Value, filter.Value2 },
					_ => new object[] { filter.Value }
				};
				var predicate = filter.FilterType switch
				{
					FilterTypes.Contains => $"({property}).Contains(@0)",
					FilterTypes.DoesNotContain => $"!({property}).Contains(@0)",
					FilterTypes.DoesNotEqual => $"{property} != @0",
					FilterTypes.EndsWith => $"({property}).EndsWith(@0)",
					FilterTypes.Equals => $"{property} == @0",
					FilterTypes.StartsWith => $"({property}).StartsWith(@0)",
					FilterTypes.In => string.Join(" || ", parameters.Select((x, i) => $"{property} == @{i}").ToArray()),
					FilterTypes.GreaterThan => $"{property} > @0",
					FilterTypes.GreaterThanOrEqual => $"{property} >= @0",
					FilterTypes.LessThanOrEqual => $"{property} <= @0",
					FilterTypes.LessThan => $"{property} < @0",
					FilterTypes.Range => $"{property} >= @0 and {property} <= @1",
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
}
