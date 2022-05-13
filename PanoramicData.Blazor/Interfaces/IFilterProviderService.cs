using PanoramicData.Blazor.Models;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Interfaces
{
	public interface IFilterProviderService<TItem>
	{
		Task<string[]> GetDistinctValuesAsync(DataRequest<TItem> request, Expression<Func<TItem, object>> field);
	}
}
