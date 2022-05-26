namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// An IDataProviderService implementation provides data query operations on an underlying data source.
/// </summary>
/// <typeparam name="TItem">Data type that defines the data set to be operated on.</typeparam>
public interface IDataProviderService<TItem>
{
	/// <summary>
	/// Sends details of a query to be performed on the underlying data set and returns the results.
	/// </summary>
	/// <param name="request">Details of the query to be performed.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new DataResponse instance containing the result of the query.</returns>
	Task<DataResponse<TItem>> GetDataAsync(DataRequest<TItem> request, CancellationToken cancellationToken);

	/// <summary>
	/// Requests that the item is deleted.
	/// </summary>
	/// <param name="item">The item to be deleted.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	Task<OperationResponse> DeleteAsync(TItem item, CancellationToken cancellationToken);

	///// <summary>
	///// Requests the given item is updated by applying the given delta.
	///// </summary>
	///// <param name="item">The original item to be updated.</param>
	///// <param name="delta">An anonymous object with new property values.</param>
	///// <param name="cancellationToken">A cancellation token for the async operation.</param>
	///// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	//Task<OperationResponse> UpdateAsync(TItem item, object delta, CancellationToken cancellationToken);

	/// <summary>
	/// Requests the given item is updated by applying the given delta.
	/// </summary>
	/// <param name="item">The original item to be updated.</param>
	/// <param name="delta">A dictionary with new property values.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	Task<OperationResponse> UpdateAsync(TItem item, IDictionary<string, object> delta, CancellationToken cancellationToken);

	/// <summary>
	/// Requests the given item is created.
	/// </summary>
	/// <param name="item">New item details.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	Task<OperationResponse> CreateAsync(TItem item, CancellationToken cancellationToken);
}
