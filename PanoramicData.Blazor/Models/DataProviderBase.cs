namespace PanoramicData.Blazor.Models;

public abstract class DataProviderBase<T> : IDataProviderService<T>, IKeyedCollection<string>, IFilterProviderService<T>
{
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
	public virtual Task<OperationResponse> UpdateAsync(T item, IDictionary<string, object> delta, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	#endregion

	#region IFilterProviderService<T> Members

	public virtual async Task<object[]> GetDistinctValuesAsync(DataRequest<T> request, Expression<Func<T, object>> field)
	{
		// use main data provider - take has to be applied on base query
		var response = await GetDataAsync(request, default);
		var fn = field.Compile();
		return response.Items
			.Where(x => fn(x) != null)
			.Select(x => fn(x))
			.Distinct()
			.OrderBy(x => x)
			.ToArray();
	}

	#endregion

	#region IKeyedCollection<string> Members

	private IDictionary<string, string> _keyProperties = new Dictionary<string, string>();

	public void Add(string key, string value)
	{
		if (!_keyProperties.ContainsKey(key))
		{
			_keyProperties.Add(key, value);
		}
	}

	public bool ContainsKey(string key) => _keyProperties.ContainsKey(key);

	public string Get(string key)
	{
		return _keyProperties.ContainsKey(key) ? _keyProperties[key] : String.Empty;
	}

	public string Get(string key, string defaultValue)
	{
		return _keyProperties.ContainsKey(key) ? _keyProperties[key] : defaultValue;
	}

	#endregion

	public virtual IQueryable<T> ApplyFilter(IQueryable<T> query, Filter filter)
	{
		if (!filter.IsValid)
		{
			throw new InvalidOperationException($"Filter is not valid: {filter.Key}");
		}

		return query.ApplyFilter(filter, this);
	}

	public virtual Expression<Func<T, bool>> ApplyFilter(Expression<Func<T, bool>>? existingPredicate, Filter filter)
	{
		if (!filter.IsValid)
		{
			throw new InvalidOperationException($"Filter is not valid: {filter.Key}");
		}

		Expression<Func<T, bool>> newPredicate = x => true;
		var property = ContainsKey(filter.Key)
			? Get(filter.Key, filter.Key)
			: filter.Key;

		var parameters = filter.FilterType switch
		{
			FilterTypes.In => filter.Value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.RemoveQuotes()).ToArray(),
			FilterTypes.Range => new[] { filter.Value.RemoveQuotes(), filter.Value2.RemoveQuotes() },
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
