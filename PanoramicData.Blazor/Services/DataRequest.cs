using System;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Services
{
	/// <summary>
	/// The DataRequest class is used to provide details of a query to be executed in order to retrieve a sub set of data from a given source.
	/// </summary>
	/// <typeparam name="TItem">Data type of the items to be retrieved.</typeparam>
	public class DataRequest<TItem>
	{
		/// <summary>
		/// Gets or sets the number for rows to skip.
		/// </summary>
		/// <remarks>
		/// The sort expression must be provided for this to work.
		/// </remarks>
		public int? Skip { get; set; }

		/// <summary>
		/// The maximum number of rows to return.
		/// </summary>
		public int? Take { get; set; }

		/// <summary>
		/// Gets or sets a Linq expression that defines one or more fields to sort the query results, before returning.
		/// </summary>
		public Expression<Func<TItem, object>>? SortFieldExpression { get; set; }

		/// <summary>
		/// Gets or sets the direction of the sort.
		/// </summary>
		public SortDirection? SortDirection { get; set; }

		/// <summary>
		/// Gets or sets an indicator to the underlying data provider that it should ignore/bypass any caching mechanism
		/// and re-evaluate the query and return fresh data.
		/// </summary>
		public bool ForceUpdate { get; set; }

		/// <summary>
		/// Gets or sets the a text string that defines search criteria to be apply to the query.
		/// </summary>
		public string? SearchText { get; set; }
	}
}
