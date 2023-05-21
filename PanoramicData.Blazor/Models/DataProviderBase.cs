namespace PanoramicData.Blazor.Models;

public abstract class DataProviderBase<T> : IDataProviderService<T>, IFilterProviderService<T>
{
	private readonly Dictionary<string, string> _keyMappings = new();

	#region IDataProviderService<T> Members

	[System.Diagnostics.CodeAnalysis.SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "<Pending>")]
	public virtual Task<OperationResponse> CreateAsync(T item, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "<Pending>")]
	public virtual Task<OperationResponse> DeleteAsync(T item, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "<Pending>")]
	public virtual Task<DataResponse<T>> GetDataAsync(DataRequest<T> request, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "<Pending>")]
	public virtual Task<OperationResponse> UpdateAsync(T item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	#endregion

	#region IFilterProviderService<T> Members

	protected virtual void ApplyDelta(T item, IDictionary<string, object> delta)
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
		return response.Items
			.Where(x => NonNullExpressionResult(fn, x))
			.Select(x => fn(x))
			.Distinct()
			.OrderBy(x => x)
			.ToArray();
	}

	/// <summary>
	/// Perform expression evaluation followed by a null check, with handling of exceptions
	/// </summary>
	/// <param name="expression">The expression to be evaluated</param>
	/// <param name="target">The target against which to execute the expression</param>
	/// <returns>Boolean true if the evaluation of an expression results in a non-null result, otherwise false</returns>
	private static bool NonNullExpressionResult(Func<T, object>? expression, T target)
	{
		if (target is null) return false;
		if (expression is null) return false;

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

		return query.ApplyFilter(filter, null);
	}

	public virtual Expression<Func<T, bool>> ApplyFilter(Expression<Func<T, bool>>? existingPredicate, Filter filter, IDictionary<string, string>? keyPropertyMappings = null)
	{
		if (!filter.IsValid)
		{
			throw new InvalidOperationException($"Filter is not valid: {filter.Key}");
		}

		Expression<Func<T, bool>> newPredicate = x => true;

		// determine property name to use in query
		if (string.IsNullOrEmpty(filter.PropertyName))
		{
			if (keyPropertyMappings != null && keyPropertyMappings.ContainsKey(filter.Key))
			{
				filter.PropertyName = keyPropertyMappings[filter.Key];
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

		var property = filter.PropertyName;

		object[] parameters = filter.FilterType switch
		{
			FilterTypes.In => filter.Value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes()).ToArray(),
			FilterTypes.Range => new[] { filter.Value.RemoveQuotes(), filter.Value2.RemoveQuotes() },
			FilterTypes.IsEmpty => new[] { string.Empty },
			FilterTypes.IsNotEmpty => new[] { string.Empty },
			_ => new object[] { filter.Value.RemoveQuotes() }
		};

		switch (filter.FilterType)
		{
			case FilterTypes.Contains:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"({property}).Contains(@0)", parameters);
				break;

			case FilterTypes.DoesNotContain:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"!{property}.Contains(@0)", parameters);
				break;

			case FilterTypes.IsNotEmpty:
			case FilterTypes.DoesNotEqual:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} != @0", parameters);
				break;

			case FilterTypes.EndsWith:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property}.EndsWith(@0)", parameters);
				break;

			case FilterTypes.GreaterThan:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} > @0", parameters);
				break;

			case FilterTypes.GreaterThanOrEqual:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0", parameters);
				break;

			case FilterTypes.In:
				var query = string.Join(" || ", parameters.Select((x, i) => $"it.{property} == @{i}").ToArray());
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, query, parameters);
				break;

			case FilterTypes.LessThan:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} < @0", parameters);
				break;

			case FilterTypes.LessThanOrEqual:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} <= @0", parameters);
				break;

			case FilterTypes.Range:
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} >= @0 && {property} <= @1", parameters);
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
				newPredicate = DynamicExpressionParser.ParseLambda<T, bool>(ParsingConfig.Default, false, $"{property} == @0", parameters);
				break;

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
}
