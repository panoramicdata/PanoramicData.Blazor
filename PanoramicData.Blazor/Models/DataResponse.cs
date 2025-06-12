namespace PanoramicData.Blazor.Models;

/// <summary>
/// The DataResponse class contains data returned by an IDataProvider implementation in response to a DataRequest.
/// </summary>
/// <typeparam name="TItem">Data type of the returned data items.</typeparam>
/// <remarks>
/// Initializes a new instance of the DataResponse class.
/// </remarks>
/// <param name="items">Data rows to be returned.</param>
/// <param name="totalCount">Total number of rows available.</param>
public class DataResponse<TItem>(List<TItem> items, int? totalCount)
{
	/// <summary>
	/// Gets the total number of rows available for the given request.
	/// </summary>
	public int? TotalCount { get; } = totalCount;

	/// <summary>
	/// Gets the number of rows returned in this response.
	/// </summary>
	public int Count { get; } = items.Count;

	/// <summary>
	/// Gets or sets the data rows returned in this response.
	/// </summary>
	public IEnumerable<TItem> Items { get; set; } = items;
}
