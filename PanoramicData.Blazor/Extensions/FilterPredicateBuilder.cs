namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Builds the Dynamic LINQ predicate and parameters that apply a <see cref="Filter"/> to a queryable.
/// </summary>
internal static class FilterPredicateBuilder
{
	private static readonly string[] _valueSeparators = ["|"];

	/// <summary>
	/// Gets the Dynamic LINQ parameter values (@0, @1, ...) for a filter on a property of the given item type.
	/// </summary>
	internal static object[] GetParameters(Filter filter, Type itemType)
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
			// unwrap Nullable<TEnum> if needed
			if (itemType.GetProperty(filter.PropertyName)?.PropertyType is { } propType
				&& (Nullable.GetUnderlyingType(propType) ?? propType) is { IsEnum: true } enumType)
			{
				parameters = [.. parameters.Select(p => p is string s ? Filter.GetMemberName(enumType, s) : p)];
			}
		}

		return parameters;
	}

	/// <summary>
	/// Gets the Dynamic LINQ predicate for a filter type on the given property.
	/// </summary>
	internal static string GetPredicate(FilterTypes filterType, string propertyName, int parameterCount)
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
