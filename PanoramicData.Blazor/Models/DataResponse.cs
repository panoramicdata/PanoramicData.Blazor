using System.Collections.Generic;
using System.Linq;

namespace PanoramicData.Blazor.Models
{
	/// <summary>
	/// The DataResponse class contains data returned by an IDataProvider implementation in response to a DataRequest.
	/// </summary>
	/// <typeparam name="TItem">Data type of the returned data items.</typeparam>
	public class DataResponse<TItem>
	{
		/// <summary>
		/// Gets the total number of rows available for the given request.
		/// </summary>
		public int? TotalCount { get; }

		/// <summary>
		/// Gets the number of rows returned in this response.
		/// </summary>
		public int Count { get; }

		/// <summary>
		/// Gets or sets the data rows returned in this response.
		/// </summary>
		public IEnumerable<TItem> Items { get; set; } = Enumerable.Empty<TItem>();

		/// <summary>
		/// Initializes a new instance of the DataResponse class.
		/// </summary>
		/// <param name="items">Data rows to be returned.</param>
		/// <param name="totalCount">Total number of rows available.</param>
		public DataResponse(List<TItem> items, int? totalCount)
		{
			Items = items;
			Count = items.Count;
			TotalCount = totalCount;
		}
	}
}
