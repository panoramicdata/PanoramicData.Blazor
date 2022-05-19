using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Models
{
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

		public virtual IQueryable<T> ApplyFilter(IQueryable<T> query, Filter filter) => query.ApplyFilter(filter, this);

		public virtual IQueryable<T> ApplyFilters(IQueryable<T> query, IEnumerable<Filter> filters, params string[] exclude)
		{
			var output = query;
			foreach (var filter in filters)
			{
				if (!exclude.Contains(filter.Key))
				{
					output = ApplyFilter(output, filter);
				}
			}
			return output;
		}
	}
}
