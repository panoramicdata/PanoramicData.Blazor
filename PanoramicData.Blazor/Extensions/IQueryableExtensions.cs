namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for <see cref="System.Linq.IQueryable{T}"/> that apply <see cref="PanoramicData.Blazor.Models.Filter"/> predicates using Dynamic LINQ.
/// </summary>
public static class IQueryableExtensions
{
	private static readonly string[] _valueSeparators = ["|"];

	/// <summary>
	/// Applies a single <see cref="PanoramicData.Blazor.Models.Filter"/> to the query.
	/// The property to filter on is resolved from the filter key using
	/// <see cref="PanoramicData.Blazor.Attributes.FilterKeyAttribute"/>, <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.ShortName"/>,
	/// or by camel-casing the key. Returns the unmodified query when the key is blank or the property cannot be found.
	/// </summary>
	/// <typeparam name="T">The element type of the queryable source.</typeparam>
	/// <param name="query">The source queryable to filter.</param>
	/// <param name="filter">The filter definition to apply.</param>
	/// <returns>A new queryable with the filter predicate applied, or the original query when the filter cannot be built.</returns>
	public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, Filter filter)
		=> query.ApplyFilter(filter, null);

	/// <summary>
	/// Applies a single <see cref="PanoramicData.Blazor.Models.Filter"/> to the query.
	/// The property to filter on is resolved from the filter key using optional key-to-property mappings,
	/// <see cref="PanoramicData.Blazor.Attributes.FilterKeyAttribute"/>, <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.ShortName"/>,
	/// or by camel-casing the key. Returns the unmodified query when the key is blank or the property cannot be found.
	/// </summary>
	/// <typeparam name="T">The element type of the queryable source.</typeparam>
	/// <param name="query">The source queryable to filter.</param>
	/// <param name="filter">The filter definition to apply.</param>
	/// <param name="keyPropertyMappings">Optional dictionary mapping filter keys to explicit property names.</param>
	/// <returns>A new queryable with the filter predicate applied, or the original query when the filter cannot be built.</returns>
	public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, Filter filter, IDictionary<string, string>? keyPropertyMappings)
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
				filter.PropertyName = ResolvePropertyName<T>(filter.Key, keyPropertyMappings);
			}

			// apply query only if property name is known
			if (!string.IsNullOrWhiteSpace(filter.PropertyName))
			{
				var parameters = GetParameters<T>(filter);
				var predicate = GetPredicate(filter.FilterType, filter.PropertyName, parameters.Length);
				return string.IsNullOrWhiteSpace(predicate) ? query : query.Where(predicate, parameters);
			}
		}
		catch
		{
			// invalid property
		}

		return query;
	}

	/// <summary>
	/// Applies a sequence of <see cref="PanoramicData.Blazor.Models.Filter"/> objects to the query, chaining them with AND semantics.
	/// </summary>
	/// <typeparam name="T">The element type of the queryable source.</typeparam>
	/// <param name="query">The source queryable to filter.</param>
	/// <param name="filters">The filters to apply in order.</param>
	/// <returns>A new queryable with all filters applied.</returns>
	public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<Filter> filters)
		=> query.ApplyFilters(filters, null);

	/// <summary>
	/// Applies a sequence of <see cref="PanoramicData.Blazor.Models.Filter"/> objects to the query, chaining them with AND semantics.
	/// </summary>
	/// <typeparam name="T">The element type of the queryable source.</typeparam>
	/// <param name="query">The source queryable to filter.</param>
	/// <param name="filters">The filters to apply in order.</param>
	/// <param name="keyProperties">Optional dictionary mapping filter keys to explicit property names.</param>
	/// <returns>A new queryable with all filters applied.</returns>
	public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IEnumerable<Filter> filters, IDictionary<string, string>? keyProperties)
	{
		foreach (var filter in filters)
		{
			query = query.ApplyFilter(filter, keyProperties);
		}

		return query;
	}

	internal static string ResolvePropertyName<T>(string key, IDictionary<string, string>? keyPropertyMappings)
	{
		if (keyPropertyMappings != null && keyPropertyMappings.TryGetValue(key, out var value))
		{
			return value;
		}

		// search entity properties for matching key attribute
		// Note - currently this does NOT perform a nested search
		var propertyInfo = typeof(T).GetProperties().SingleOrDefault(x => x.GetFilterKey() == key || x.GetDisplayShortName() == key);

		// fallback is to simply use key
		return propertyInfo?.Name ?? key.UpperFirstChar();
	}

	private static object[] GetParameters<T>(Filter filter)
	{
		object[] parameters = filter.FilterType switch
		{
			FilterTypes.In or FilterTypes.NotIn => [.. filter.Value.Split(_valueSeparators, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())],
			FilterTypes.Range => [filter.Value.RemoveQuotes(), filter.Value2.RemoveQuotes()],
			FilterTypes.IsEmpty or FilterTypes.IsNotEmpty => [string.Empty],
			_ => [filter.Value.RemoveQuotes()]
		};

		// for enum properties, translate display names back to member names for dynamic LINQ
		if (filter.FilterType is FilterTypes.Equals or FilterTypes.In or FilterTypes.NotIn or FilterTypes.DoesNotEqual)
		{
			var propType = typeof(T).GetProperty(filter.PropertyName)?.PropertyType;

			// unwrap Nullable<TEnum> if needed
			var enumType = propType is null ? null : Nullable.GetUnderlyingType(propType) ?? propType;
			if (enumType?.IsEnum == true)
			{
				parameters = [.. parameters.Select(p => p is string s ? Filter.GetMemberName(enumType, s) : p)];
			}
		}

		return parameters;
	}

	private static string GetPredicate(FilterTypes filterType, string propertyName, int parameterCount)
	{
		// if using nested properties - surround by null propagating function
		if (propertyName.Contains('.'))
		{
			propertyName = $"np({propertyName})";
		}

		return filterType switch
		{
			FilterTypes.Contains => $"{propertyName} != null and ({propertyName}).Contains(@0)",
			FilterTypes.DoesNotContain => $"{propertyName} != null and !({propertyName}).Contains(@0)",
			FilterTypes.DoesNotEqual => $"{propertyName} != @0",
			FilterTypes.EndsWith => $"{propertyName} != null and ({propertyName}).EndsWith(@0)",
			FilterTypes.Equals => $"{propertyName} == @0",
			FilterTypes.StartsWith => $"{propertyName} != null and ({propertyName}).StartsWith(@0)",
			FilterTypes.In => string.Join(" || ", Enumerable.Range(0, parameterCount).Select(i => $"{propertyName} == @{i}")),
			FilterTypes.NotIn => string.Join(" && ", Enumerable.Range(0, parameterCount).Select(i => $"{propertyName} != @{i}")),
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
	}
}
