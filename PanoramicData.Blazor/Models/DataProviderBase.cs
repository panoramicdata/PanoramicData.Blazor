namespace PanoramicData.Blazor.Models;

/// <summary>
/// Abstract base class providing a default implementation of <see cref="IDataProviderService{T}"/> and
/// <see cref="IFilterProviderService{T}"/> that subclasses can override to supply data from any source.
/// </summary>
/// <typeparam name="T">The entity type provided by this service.</typeparam>
public abstract class DataProviderBase<T> : IDataProviderService<T>, IFilterProviderService<T>
{
	private readonly Dictionary<string, string> _keyMappings = [];

	#region IDataProviderService<T> Members

	/// <inheritdoc />
	public virtual Task<OperationResponse> CreateAsync(T item, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public virtual Task<OperationResponse> DeleteAsync(T item, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public virtual Task<DataResponse<T>> GetDataAsync(DataRequest<T> request, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public virtual Task<OperationResponse> UpdateAsync(T item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	#endregion

	#region IFilterProviderService<T> Members

	/// <summary>
	/// Applies each key/value pair in <paramref name="delta"/> to the corresponding property of <paramref name="item"/> via reflection.
	/// </summary>
	/// <param name="item">The entity to update.</param>
	/// <param name="delta">A dictionary of property names to new values.</param>
	protected virtual void ApplyDelta(T item, IDictionary<string, object?> delta)
	{
		var itemType = typeof(T);
		foreach (var kvp in delta)
		{
			try
			{
				var propInfo = itemType.GetProperty(kvp.Key);
				propInfo?.SetValue(item, kvp.Value);
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Error applying delta to {kvp.Key}: {ex.Message}", ex);
			}
		}
	}

	/// <summary>Gets the dictionary that maps filter key names to entity property names used when building LINQ predicates.</summary>
	public IDictionary<string, string> KeyPropertyMappings => _keyMappings;

	/// <summary>
	/// Returns distinct, ordered, non-null values for the specified field by delegating to <see cref="GetDataAsync"/>.
	/// </summary>
	/// <inheritdoc />
	public virtual async Task<object[]> GetDistinctValuesAsync(DataRequest<T> request, Expression<Func<T, object>> field)
	{
		// use main data provider - take has to be applied on base query
		var response = await GetDataAsync(request, default);
		var fn = field.Compile();
		return [.. response.Items
			.Where(x => NonNullExpressionResult(fn, x))
			.Select(x => fn(x))
			.Distinct()
			.OrderBy(x => x)];
	}

	/// <summary>
	/// Perform expression evaluation followed by a null check, with handling of exceptions
	/// </summary>
	/// <param name="expression">The expression to be evaluated</param>
	/// <param name="target">The target against which to execute the expression</param>
	/// <returns>Boolean true if the evaluation of an expression results in a non-null result, otherwise false</returns>
	private static bool NonNullExpressionResult(Func<T, object> expression, T target)
	{
		if (target is null)
		{
			return false;
		}

		try
		{
			return expression(target) != null;
		}
		catch
		{
			return false;
		}
	}

	#endregion

	/// <summary>
	/// Applies a single <see cref="Filter"/> to an <see cref="IQueryable{T}"/> using the configured key/property mappings.
	/// </summary>
	/// <param name="query">The query to filter.</param>
	/// <param name="filter">The filter to apply.</param>
	/// <returns>The filtered query.</returns>
	public virtual IQueryable<T> ApplyFilter(IQueryable<T> query, Filter filter)
	{
		if (!filter.IsValid)
		{
			throw new InvalidOperationException($"Filter is not valid: {filter.Key}");
		}

		return query.ApplyFilter(filter, _keyMappings);
	}

	/// <summary>
	/// Builds or combines a LINQ predicate expression for the given <see cref="Filter"/> using the configured key/property mappings.
	/// </summary>
	/// <param name="existingPredicate">An optional predicate to AND with the new condition.</param>
	/// <param name="filter">The filter to translate into a predicate.</param>
	/// <returns>The combined predicate expression.</returns>
	public virtual Expression<Func<T, bool>> ApplyFilter(Expression<Func<T, bool>>? existingPredicate, Filter filter)
		=> ApplyFilter(existingPredicate, filter, null);

	/// <summary>
	/// Builds or combines a LINQ predicate expression for the given <see cref="Filter"/> using an explicit key/property mapping dictionary.
	/// </summary>
	/// <param name="existingPredicate">An optional predicate to AND with the new condition.</param>
	/// <param name="filter">The filter to translate into a predicate.</param>
	/// <param name="keyPropertyMappings">Optional key-to-property mappings that override the instance-level mappings.</param>
	/// <returns>The combined predicate expression.</returns>
	public virtual Expression<Func<T, bool>> ApplyFilter(Expression<Func<T, bool>>? existingPredicate, Filter filter, IDictionary<string, string>? keyPropertyMappings)
	{
		if (!filter.IsValid)
		{
			throw new InvalidOperationException($"Filter is not valid: {filter.Key}");
		}

		// determine property name to use in query
		if (string.IsNullOrEmpty(filter.PropertyName))
		{
			filter.PropertyName = IQueryableExtensions.ResolvePropertyName<T>(filter.Key, keyPropertyMappings);
		}

		var newPredicate = BuildPredicate(new PredicateContext(filter));
		return existingPredicate is null ? newPredicate : PredicateBuilderService.And(existingPredicate, newPredicate);
	}

	/// <summary>
	/// Applies multiple filters to a query, skipping any that are invalid or whose keys are in <paramref name="exclude"/>.
	/// </summary>
	/// <param name="query">The base query to filter.</param>
	/// <param name="filters">The set of filters to apply.</param>
	/// <param name="exclude">Keys of filters to skip.</param>
	/// <returns>The filtered query.</returns>
	public virtual IQueryable<T> ApplyFilters(IQueryable<T> query, IEnumerable<Filter> filters, params string[] exclude)
	{
		var output = query;
		foreach (var filter in filters.Where(x => x.IsValid && !exclude.Contains(x.Key)))
		{
			output = ApplyFilter(output, filter);
		}

		return output;
	}

	/// <summary>
	/// Builds a combined LINQ predicate from multiple filters, skipping any that are invalid or whose keys are in <paramref name="exclude"/>.
	/// Returns an always-true predicate when no valid filters are provided.
	/// </summary>
	/// <param name="filters">The set of filters to translate.</param>
	/// <param name="exclude">Keys of filters to skip.</param>
	/// <returns>A combined predicate expression, or a predicate that always returns true.</returns>
	public virtual Expression<Func<T, bool>> ApplyFilters(IEnumerable<Filter> filters, params string[] exclude)
	{
		Expression<Func<T, bool>>? predicate = null;
		foreach (var filter in filters.Where(x => x.IsValid && !exclude.Contains(x.Key)))
		{
			predicate = ApplyFilter(predicate, filter);
		}

		return predicate ?? (x => true);
	}

	private static Expression<Func<T, bool>> BuildPredicate(PredicateContext context)
	{
		var property = context.Property;
		var parameters = context.Parameters;
		return context.Filter.FilterType switch
		{
			// TODO: allow for case insensitivity
			FilterTypes.Contains => Parse($"({property}).Contains(@0)", parameters),
			FilterTypes.DoesNotContain => Parse($"!{property}.Contains(@0)", parameters),
			FilterTypes.IsNotEmpty or FilterTypes.DoesNotEqual => BuildDoesNotEqual(context),
			FilterTypes.EndsWith => Parse($"{property}.EndsWith(@0)", parameters),
			FilterTypes.GreaterThan => BuildGreaterThan(context),
			FilterTypes.GreaterThanOrEqual => BuildGreaterThanOrEqual(context),
			FilterTypes.In => BuildMembership(context, false),
			FilterTypes.NotIn => BuildMembership(context, true),
			FilterTypes.LessThan => BuildLessThan(context),
			FilterTypes.LessThanOrEqual => BuildLessThanOrEqual(context),
			FilterTypes.Range => BuildRange(context),
			FilterTypes.StartsWith => Parse($"{property}.StartsWith(@0)", parameters),
			FilterTypes.IsNull => Parse($"{property} == null"),
			FilterTypes.IsNotNull => Parse($"{property} != null"),
			FilterTypes.IsEmpty or FilterTypes.Equals => BuildEquals(context),
			_ => Parse($"{property} == @0", parameters)
		};
	}

	private static Expression<Func<T, bool>> BuildDoesNotEqual(PredicateContext context)
	{
		if (context.ParseDate(context.Filter.Value) is not { } date)
		{
			return Parse($"{context.Property} != @0", context.Parameters);
		}

		var from = ZeroOutDateParts(date.Date, date.Precision);
		var to = GetDateRangeEnd(from, date.Precision);
		return Parse($"{context.Property} >= @0 || {context.Property} < @1", context.ToParameter(to), context.ToParameter(from));
	}

	private static Expression<Func<T, bool>> BuildEquals(PredicateContext context)
	{
		if (context.ParseDate(context.Filter.Value) is not { } date)
		{
			return Parse($"{context.Property} == @0", context.Parameters);
		}

		var from = ZeroOutDateParts(date.Date, date.Precision);
		var to = GetDateRangeEnd(from, date.Precision);
		return Parse($"{context.Property} >= @0 && {context.Property} < @1", context.ToParameter(from), context.ToParameter(to));
	}

	private static Expression<Func<T, bool>> BuildGreaterThan(PredicateContext context)
		=> context.ParseDate(context.Filter.Value) is { } date
			? Parse($"{context.Property} >= @0", context.ToParameter(GetDateRangeEnd(date.Date, date.Precision)))
			: Parse($"{context.Property} > @0", context.Parameters);

	private static Expression<Func<T, bool>> BuildGreaterThanOrEqual(PredicateContext context)
		=> context.ParseDate(context.Filter.Value) is { } date
			? Parse($"{context.Property} >= @0", context.ToParameter(date.Date))
			: Parse($"{context.Property} >= @0", context.Parameters);

	private static Expression<Func<T, bool>> BuildLessThan(PredicateContext context)
		=> context.ParseDate(context.Filter.Value) is { } date
			? Parse($"{context.Property} < @0", context.ToParameter(ZeroOutDateParts(date.Date, date.Precision)))
			: Parse($"{context.Property} < @0", context.Parameters);

	private static Expression<Func<T, bool>> BuildLessThanOrEqual(PredicateContext context)
		=> context.ParseDate(context.Filter.Value) is { } date
			? Parse($"{context.Property} < @0", context.ToParameter(GetDateRangeEnd(date.Date, date.Precision)))
			: Parse($"{context.Property} <= @0", context.Parameters);

	private static Expression<Func<T, bool>> BuildRange(PredicateContext context)
	{
		if (context.ParseDate(context.Filter.Value) is not { } from || context.ParseDate(context.Filter.Value2) is not { } to)
		{
			return Parse($"{context.Property} >= @0 && {context.Property} <= @1", context.Parameters);
		}

		// the values are swapped when given in the wrong order, but each keeps the precision it was written with
		var rangeFrom = from.Date;
		var rangeTo = to.Date;
		if (rangeFrom > rangeTo)
		{
			(rangeTo, rangeFrom) = (rangeFrom, rangeTo);
		}

		rangeFrom = ZeroOutDateParts(rangeFrom, from.Precision);
		rangeTo = GetDateRangeEnd(rangeTo, to.Precision);
		return Parse($"{context.Property} >= @0 && {context.Property} < @1", context.ToParameter(rangeFrom), context.ToParameter(rangeTo));
	}

	private static Expression<Func<T, bool>> BuildMembership(PredicateContext context, bool negate)
	{
		var joiner = negate ? " && " : " || ";
		var dates = Array.ConvertAll(context.Parameters, p => context.ParseDate(p.ToString()));
		if (!Array.TrueForAll(dates, d => d.HasValue))
		{
			var comparison = negate ? "!=" : "==";
			return Parse(string.Join(joiner, context.Parameters.Select((_, i) => $"it.{context.Property} {comparison} @{i}")), context.Parameters);
		}

		var boundaries = dates.SelectMany(d =>
		{
			var start = ZeroOutDateParts(d!.Value.Date, d.Value.Precision);
			return new object[] { context.ToParameter(start), context.ToParameter(GetDateRangeEnd(start, d.Value.Precision)) };
		}).ToArray();
		var not = negate ? "!" : string.Empty;
		var query = string.Join(joiner, dates.Select((_, i) => $"{not}(it.{context.Property} >= @{i * 2} && it.{context.Property} < @{i * 2 + 1})"));
		return Parse(query, boundaries);
	}

	private static Expression<Func<T, bool>> Parse(string expression, params object[] values)
		=> DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, expression, values);

	/// <summary>
	/// What a predicate is built from: the filter, the property it compares, and that property's type, which decides
	/// how a date value becomes a query parameter (#193) and whether a bare year or year and month may be read as a
	/// date at all (#197).
	/// </summary>
	private sealed class PredicateContext
	{
		private readonly Type? _propertyType;
		private readonly bool _isDateProperty;

		public PredicateContext(Filter filter)
		{
			Filter = filter;
			Property = filter.PropertyName;
			_propertyType = GetPropertyType(Property);
			_isDateProperty = IsDateType(_propertyType);
			Parameters = filter.FilterType switch
			{
				FilterTypes.In or FilterTypes.NotIn => [.. filter.Value.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())],
				FilterTypes.Range => [filter.Value.RemoveQuotes(), filter.Value2.RemoveQuotes()],
				FilterTypes.IsEmpty or FilterTypes.IsNotEmpty => [string.Empty],
				_ => [filter.Value.RemoveQuotes()]
			};
		}

		public Filter Filter { get; }

		public string Property { get; }

		public object[] Parameters { get; }

		public (DateTime Date, DatePrecision Precision)? ParseDate(string? value)
			=> Filter.IsDateTime(value, _isDateProperty, out var date, out _, out var precision) ? (date, precision) : null;

		public string ToParameter(DateTime boundary) => DataProviderBase<T>.ToParameter(boundary, _propertyType);
	}

	/// <summary>
	/// Resolves the type of the (possibly dotted) property path a filter compares with, or null when it cannot
	/// be resolved to a single public instance property at every step.
	/// </summary>
	private static Type? GetPropertyType(string propertyPath)
	{
		var type = typeof(T);
		foreach (var name in propertyPath.Split('.'))
		{
			PropertyInfo? propertyInfo;
			try
			{
				propertyInfo = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			}
			catch (AmbiguousMatchException)
			{
				return null;
			}

			if (propertyInfo is null)
			{
				return null;
			}

			type = propertyInfo.PropertyType;
		}

		return type;
	}

	private static bool IsDateType(Type? type)
	{
		var actualType = type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
		return actualType == typeof(DateTime) || actualType == typeof(DateTimeOffset);
	}

	/// <summary>
	/// Converts a date boundary into the query parameter it is compared with. Filter values without a time zone
	/// are UTC, as <see cref="Filter.Format(object, bool)"/> treats them.
	/// </summary>
	/// <remarks>
	/// For a <see cref="DateTime"/> property the parameter is the UTC wall clock with no zone designator, which
	/// Dynamic LINQ reads back as exactly that value. It used to end in Z, which Dynamic LINQ converts to the local
	/// time of the machine running the query, moving every boundary by that machine's UTC offset (#193). A string
	/// is still used, rather than a <see cref="DateTime"/>, because Dynamic LINQ will not compare a
	/// <see cref="Nullable{DateTime}"/> property with a <see cref="DateTime"/> parameter. The value it produces is
	/// Unspecified, which a query provider that checks Kind against the column type (Npgsql does) accepts wherever
	/// it accepted the Local value produced before. Any other property, including <see cref="DateTimeOffset"/>
	/// where the Z is read correctly, gets the string it always did.
	/// </remarks>
	private static string ToParameter(DateTime boundary, Type? propertyType)
	{
		var actualType = propertyType is null ? null : Nullable.GetUnderlyingType(propertyType) ?? propertyType;
		if (actualType != typeof(DateTime))
		{
			return Filter.Format(boundary, true);
		}

		// A value parsed with a time zone arrives as local time; anything else already holds the UTC wall clock.
		var utc = boundary.Kind == DateTimeKind.Local ? boundary.ToUniversalTime() : boundary;
		return utc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss", CultureInfo.InvariantCulture);
	}
	private static DateTime ZeroOutDateParts(DateTime date, DatePrecision precision)
	{
		return precision switch
		{
			DatePrecision.Year => new DateTime(date.Year, 1, 1),
			DatePrecision.Month => new DateTime(date.Year, date.Month, 1),
			DatePrecision.Day => new DateTime(date.Year, date.Month, date.Day),
			DatePrecision.Hour => new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0, date.Kind),
			DatePrecision.Minute => new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, date.Kind),
			DatePrecision.Second => new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind),
			_ => date
		};
	}

	private static DateTime GetDateRangeEnd(DateTime date, DatePrecision precision)
	{
		date = ZeroOutDateParts(date, precision);
		return precision switch
		{
			DatePrecision.Year => date.AddYears(1),
			DatePrecision.Month => date.AddMonths(1),
			DatePrecision.Day => date.AddDays(1),
			DatePrecision.Hour => date.AddHours(1),
			DatePrecision.Minute => date.AddMinutes(1),
			DatePrecision.Second => date.AddSeconds(1),
			_ => date
		};
	}
}
