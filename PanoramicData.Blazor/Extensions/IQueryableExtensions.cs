namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for <see cref="System.Linq.IQueryable{T}"/> that apply <see cref="PanoramicData.Blazor.Models.Filter"/> predicates using Dynamic LINQ.
/// </summary>
public static class IQueryableExtensions
{
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
				var parameters = FilterPredicateBuilder.GetParameters(filter, typeof(T));
				var predicate = FilterPredicateBuilder.GetPredicate(filter.FilterType, filter.PropertyName, parameters.Length);
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
}
