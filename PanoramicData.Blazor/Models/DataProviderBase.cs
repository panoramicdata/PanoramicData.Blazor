using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Models
{
	public abstract class DataProviderBase<T> : IDataProviderService<T>, IKeyedCollection<string>
	{
		#region IDataProviderService<T> Members

		public virtual Task<OperationResponse> CreateAsync(T item, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public virtual Task<OperationResponse> DeleteAsync(T item, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public virtual Task<DataResponse<T>> GetDataAsync(DataRequest<T> request, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public virtual Task<OperationResponse> UpdateAsync(T item, IDictionary<string, object> delta, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
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
			foreach(var filter in filters)
			{
				if (!exclude.Contains(filter.Key))
				{
					output = ApplyFilter(query, filter);
				}
			}
			return output;
		}
	}
}
