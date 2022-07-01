namespace PanoramicData.Blazor.Interfaces;

public interface IFilterProviderService<TItem>
{
	IDictionary<string, string> KeyPropertyMappings { get; }

	Task<object[]> GetDistinctValuesAsync(DataRequest<TItem> request, Expression<Func<TItem, object>> field);
}
