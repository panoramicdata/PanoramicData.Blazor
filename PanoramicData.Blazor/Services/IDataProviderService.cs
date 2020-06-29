using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Services
{
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
	}
}
