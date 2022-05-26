namespace PanoramicData.Blazor.Models;

public class DelegatedDataProvider<TItem> : IDataProviderService<TItem>
{
	public Func<TItem, CancellationToken, Task<OperationResponse>>? CreateAsync { get; set; }
	public Func<TItem, CancellationToken, Task<OperationResponse>>? DeleteAsync { get; set; }
	public Func<TItem, IDictionary<string, object>, CancellationToken, Task<OperationResponse>>? UpdateAsync { get; set; }
	public Func<DataRequest<TItem>, CancellationToken, Task<DataResponse<TItem>>>? GetDataAsync { get; set; }

	#region IDataProviderService<TItem> members

	async Task<OperationResponse> IDataProviderService<TItem>.CreateAsync(TItem item, CancellationToken cancellationToken)
	{
		if (CreateAsync != null)
		{
			return await CreateAsync(item, cancellationToken);
		}
		throw new NotImplementedException();
	}

	async Task<OperationResponse> IDataProviderService<TItem>.DeleteAsync(TItem item, CancellationToken cancellationToken)
	{
		if (DeleteAsync != null)
		{
			return await DeleteAsync(item, cancellationToken);
		}
		throw new NotImplementedException();
	}

	async Task<DataResponse<TItem>> IDataProviderService<TItem>.GetDataAsync(DataRequest<TItem> request, CancellationToken cancellationToken)
	{
		if (GetDataAsync != null)
		{
			return await GetDataAsync(request, cancellationToken);
		}
		throw new NotImplementedException();
	}

	async Task<OperationResponse> IDataProviderService<TItem>.UpdateAsync(TItem item, IDictionary<string, object> delta, CancellationToken cancellationToken)
	{
		if (UpdateAsync != null)
		{
			return await UpdateAsync(item, delta, cancellationToken);
		}
		throw new NotImplementedException();
	}

	#endregion
}
