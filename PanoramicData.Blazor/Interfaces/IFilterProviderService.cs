namespace PanoramicData.Blazor.Interfaces;

public interface IFilterProviderService<TItem>
{
	Task<object[]> GetDistinctValuesAsync(DataRequest<TItem> request, Expression<Func<TItem, object>> field);
}
