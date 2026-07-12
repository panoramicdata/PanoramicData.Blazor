namespace PanoramicData.Blazor.Services;

/// <summary>
/// The EmptyDataProviderService class implements an IDataProviderService instance that
/// contains no data and all operation do nothing and return success.
/// </summary>
/// <remarks>The primary use of the service is as a default instance for components, to be replaced
/// by calling apps.</remarks>
public class EmptyDataProviderService<TItem> : IDataProviderService<TItem>
{
	/// <inheritdoc />
	public Task<OperationResponse> CreateAsync(TItem item, CancellationToken cancellationToken)
		=> Task.FromResult(new OperationResponse { Success = true });

	/// <inheritdoc />
	public Task<OperationResponse> DeleteAsync(TItem item, CancellationToken cancellationToken)
		=> Task.FromResult(new OperationResponse { Success = true });

	/// <inheritdoc />
	public Task<DataResponse<TItem>> GetDataAsync(DataRequest<TItem> request, CancellationToken cancellationToken)
		=> Task.FromResult(new DataResponse<TItem>([], 0));

	/// <inheritdoc />
	public Task<OperationResponse> UpdateAsync(TItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		=> Task.FromResult(new OperationResponse { Success = true });
}
