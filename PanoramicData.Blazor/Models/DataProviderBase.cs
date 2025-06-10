namespace PanoramicData.Blazor.Models;

public abstract class DataProviderBase<T> : IDataProviderService<T>, IFilterProviderService<T>
{
	private readonly Dictionary<string, string> _keyMappings = [];

	#region IDataProviderService<T> Members

	public virtual Task<OperationResponse> CreateAsync(T item, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	public virtual Task<OperationResponse> DeleteAsync(T item, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	public virtual Task<DataResponse<T>> GetDataAsync(DataRequest<T> request, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	public virtual Task<OperationResponse> UpdateAsync(T item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		=> throw new NotImplementedException();

	#endregion

	#region IFilterProviderService<T> Members

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

	public IDictionary<string, string> KeyPropertyMappings => _keyMappings;

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
	private static bool NonNullExpressionResult(Func<T, object>? expression, T target)
	{
		if (target is null)
		{
			return false;
		}

		if (expression is null)
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

	public virtual IQueryable<T> ApplyFilter(IQueryable<T> query, Filter filter)
	{
		if (!filter.IsValid)
		{
			throw new InvalidOperationException($"Filter is not valid: {filter.Key}");
		}

		return query.ApplyFilter(filter, _keyMappings);
	}

	public virtual Expression<Func<T, bool>> ApplyFilter(Expression<Func<T, bool>>? existingPredicate, Filter filter)
		=> ApplyFilter(existingPredicate, filter, null);

	public virtual Expression<Func<T, bool>> ApplyFilter(Expression<Func<T, bool>>? existingPredicate, Filter filter, IDictionary<string, string>? keyPropertyMappings)
	{
		if (!filter.IsValid)
		{
			throw new InvalidOperationException($"Filter is not valid: {filter.Key}");
		}

		Expression<Func<T, bool>> newPredicate = x => true;

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

		var property = filter.PropertyName;

		object[] parameters = filter.FilterType switch
		{
			FilterTypes.In => [.. filter.Value.Split(["|"], StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())],
			FilterTypes.NotIn => [.. filter.Value.Split(["|"], StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes())],
			FilterTypes.Range => [filter.Value.RemoveQuotes(), filter.Value2.RemoveQuotes()],
			FilterTypes.IsEmpty => [string.Empty],
			FilterTypes.IsNotEmpty => [string.Empty],
			_ => new object[] { filter.Value.RemoveQuotes() }
		};

		switch (filter.FilterType)
		{
			case FilterTypes.Contains:
				// TODO: allow for case insensitivity
				//var lowerParams = parameters.Where(x => x != null).Select(x => x.ToString()!.ToLowerInvariant()).ToArray();
				//newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"({property}.ToLower()).Contains(@0)", lowerParams);
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"({property}).Contains(@0)", parameters);
				break;

			case FilterTypes.DoesNotContain:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"!{property}.Contains(@0)", parameters);
				break;

			case FilterTypes.IsNotEmpty:
			case FilterTypes.DoesNotEqual:
				{
					if (Filter.IsDateTime(filter.Value, out var from, out var format, out var datePrecision))
					{
						from = ZeroOutDateParts(from, datePrecision);
						var equalsToUTC = Filter.Format(GetDateRangeEnd(from, datePrecision), true);
						var equalsFromUTC = Filter.Format(from, true);
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0 || {property} < @1", equalsToUTC, equalsFromUTC);
					}
					else
					{
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} != @0", parameters);
					}
					break;
				}
			case FilterTypes.EndsWith:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property}.EndsWith(@0)", parameters);
				break;

			case FilterTypes.GreaterThan:
				{
					if (Filter.IsDateTime(filter.Value, out var gtDateTime, out var format, out var datePrecision))
					{
						gtDateTime = GetDateRangeEnd(gtDateTime, datePrecision);
						var addedASecond = Filter.Format(gtDateTime, true);
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0", addedASecond);
					}
					else
					{
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} > @0", parameters);
					}
					break;
				}
			case FilterTypes.GreaterThanOrEqual:
				{
					if (Filter.IsDateTime(filter.Value, out var gteqDateTime, out var formatFound, out var datePrecision))
					{
						var addedASecond = Filter.Format(gteqDateTime, true);
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0", addedASecond);
					}
					else
					{
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0", parameters);
					}
					break;
				}
			case FilterTypes.In:
				{
					var allDateTimes = Array.TrueForAll(parameters, p => Filter.IsDateTime(p.ToString(), out _, out var _, out var _));
					if (allDateTimes)
					{
						var dateTimeParameters = parameters.Select(p =>
						{
							Filter.IsDateTime(p.ToString(), out var dt, out var formatFound, out var datePrecision);
							dt = ZeroOutDateParts(dt, datePrecision);
							var dtTo = GetDateRangeEnd(dt, datePrecision);
							return new { Start = Filter.Format(dt, true), End = Filter.Format(dtTo, true) };
						}).ToArray();

						var query = string.Join(" || ", dateTimeParameters.Select((p, i) => $"(it.{property} >= @{i * 2} && it.{property} < @{i * 2 + 1})").ToArray());
						var dateTimeValues = dateTimeParameters.SelectMany(p => new object[] { p.Start, p.End }).ToArray();
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, query, dateTimeValues);
					}
					else
					{
						var query = string.Join(" || ", parameters.Select((x, i) => $"it.{property} == @{i}").ToArray());
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, query, parameters);
					}
				}
				break;

			case FilterTypes.NotIn:
				{
					var allDateTimes = Array.TrueForAll(parameters, p => Filter.IsDateTime(p.ToString(), out _, out var _, out var _));
					if (allDateTimes)
					{
						var dateTimeParameters = parameters.Select(p =>
						{
							Filter.IsDateTime(p.ToString(), out var dt, out var format, out var datePrecision);
							dt = ZeroOutDateParts(dt, datePrecision);
							var dtTo = GetDateRangeEnd(dt, datePrecision);
							return new { Start = Filter.Format(dt, true), End = Filter.Format(dtTo, true) };
						}).ToArray();

						var query = string.Join(" && ", dateTimeParameters.Select((p, i) => $"!(it.{property} >= @{i * 2} && it.{property} < @{i * 2 + 1})").ToArray());
						var dateTimeValues = dateTimeParameters.SelectMany(p => new object[] { p.Start, p.End }).ToArray();
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, query, dateTimeValues);
					}
					else
					{
						var query = string.Join(" && ", parameters.Select((x, i) => $"it.{property} != @{i}").ToArray());
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, query, parameters);
					}
				}
				break;

			case FilterTypes.LessThan:
				{
					if (Filter.IsDateTime(filter.Value, out var ltDateTime, out var format, out var datePrecision))
					{
						ltDateTime = ZeroOutDateParts(ltDateTime, datePrecision);
						var addedASecond = Filter.Format(ltDateTime, true);
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} < @0", addedASecond);
					}
					else
					{
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} < @0", parameters);
					}
					break;
				}
			case FilterTypes.LessThanOrEqual:
				{
					if (Filter.IsDateTime(filter.Value, out var lteqDateTime, out var formatFound, out var datePrecision))
					{
						lteqDateTime = ZeroOutDateParts(lteqDateTime, datePrecision);
						lteqDateTime = GetDateRangeEnd(lteqDateTime, datePrecision);
						var addedASecondUTC = Filter.Format(lteqDateTime, true);
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} < @0", addedASecondUTC);
					}
					else
					{
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} <= @0", parameters);
					}
					break;
				}
			case FilterTypes.Range:
				if (Filter.IsDateTime(filter.Value, out var rangeFrom, out var fromFormat, out var fromDatePrecision) &&
						Filter.IsDateTime(filter.Value2, out var rangeTo, out var toFormat, out var toDatePrecision))
				{
					if (rangeFrom > rangeTo)
					{
						(rangeTo, rangeFrom) = (rangeFrom, rangeTo);
					}

					rangeFrom = ZeroOutDateParts(rangeFrom, fromDatePrecision);
					rangeTo = GetDateRangeEnd(rangeTo, toDatePrecision);
					var equalsToUTC = Filter.Format(rangeTo, true);
					var equalsFromUTC = Filter.Format(rangeFrom, true);

					newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0 && {property} < @1", equalsFromUTC, equalsToUTC);
				}
				else
				{
					newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0 && {property} <= @1", parameters);
				}
				break;

			case FilterTypes.StartsWith:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property}.StartsWith(@0)", parameters);
				break;

			case FilterTypes.IsNull:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} == null");
				break;

			case FilterTypes.IsNotNull:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} != null");
				break;

			case FilterTypes.IsEmpty:
			case FilterTypes.Equals:
				{
					if (Filter.IsDateTime(filter.Value, out var equalsFrom, out var formatFound, out var datePrecision))
					{
						equalsFrom = ZeroOutDateParts(equalsFrom, datePrecision);
						var equalsTo = GetDateRangeEnd(equalsFrom, datePrecision);
						var equalsToUTC = Filter.Format(equalsTo, true);
						var equalsFromUTC = Filter.Format(equalsFrom, true);
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0 && {property} < @1", equalsFromUTC, equalsToUTC);
					}
					else
					{
						newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} == @0", parameters);
					}
					break;
				}
			default:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} == @0", parameters);
				break;
		}

		return existingPredicate is null ? newPredicate : PredicateBuilderService.And(existingPredicate, newPredicate);
	}

	public virtual IQueryable<T> ApplyFilters(IQueryable<T> query, IEnumerable<Filter> filters, params string[] exclude)
	{
		var output = query;
		foreach (var filter in filters.Where(x => x.IsValid && !exclude.Contains(x.Key)))
		{
			output = ApplyFilter(output, filter);
		}

		return output;
	}

	public virtual Expression<Func<T, bool>> ApplyFilters(IEnumerable<Filter> filters, params string[] exclude)
	{
		Expression<Func<T, bool>>? predicate = null;
		foreach (var filter in filters.Where(x => x.IsValid && !exclude.Contains(x.Key)))
		{
			predicate = ApplyFilter(predicate, filter);
		}

		if (predicate is null)
		{
			return x => true;
		}

		return predicate ?? PredicateBuilderService.True<T>();
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
