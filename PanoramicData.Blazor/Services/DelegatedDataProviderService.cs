namespace PanoramicData.Blazor.Services;

/// <summary>
/// <see cref="IDataProviderService{TItem}"/> implementation that delegates operations to assigned function handlers.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public class DelegatedDataProviderService<TItem> : IDataProviderService<TItem>
{
	/// <summary>
	/// Gets or sets the delegate used to create items.
	/// </summary>
	public Func<TItem, CancellationToken, Task<OperationResponse>>? CreateAsync { get; set; }
	/// <summary>
	/// Gets or sets the delegate used to delete items.
	/// </summary>
	public Func<TItem, CancellationToken, Task<OperationResponse>>? DeleteAsync { get; set; }
	/// <summary>
	/// Gets or sets the delegate used to update items.
	/// </summary>
	public Func<TItem, IDictionary<string, object?>, CancellationToken, Task<OperationResponse>>? UpdateAsync { get; set; }
	/// <summary>
	/// Gets or sets the delegate used to retrieve data.
	/// </summary>
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

	async Task<OperationResponse> IDataProviderService<TItem>.UpdateAsync(TItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
		if (UpdateAsync != null)
		{
			return await UpdateAsync(item, delta, cancellationToken);
		}

		throw new NotImplementedException();
	}

	#endregion
}
