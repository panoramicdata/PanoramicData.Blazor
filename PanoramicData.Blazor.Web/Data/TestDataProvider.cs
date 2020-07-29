using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Data
{
	public class TestDataProvider : IDataProviderService<TestRow>
	{
		private static string _loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce at leo eu risus faucibus facilisis quis in tortor. Phasellus gravida libero sit amet ullamcorper rhoncus. Ut at viverra lectus. Vestibulum mi eros, egestas vel nulla at, lacinia ornare mauris. Morbi a pulvinar lacus. Praesent ut convallis magna. Etiam est sem, feugiat a leo in, viverra scelerisque lectus. Vivamus dictum luctus eros non ultrices. Curabitur enim enim, porta eu lorem ut, varius venenatis sem.";
		private static string[] _names = new string[] { "Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy", "Mike" };
		private static Random _random = new Random(Environment.TickCount);
		private static readonly List<TestRow> _testData = new List<TestRow>();

		public TestDataProvider()
		{
			// generate random rows
			if (_testData.Count() == 0)
			{
				foreach (var id in Enumerable.Range(1, 55))
				{
					_testData.Add(new TestRow
					{
						IntField = id,
						BooleanField = _random.Next(0, 2) == 1,
						DateField = DateTimeOffset.Now.AddDays(_random.Next(0, 7)),
						NameField = _names[_random.Next(_names.Length)],
						StringField = _loremIpsum.Substring(0, _random.Next(0, _loremIpsum.Length))
					});
				}
			}
		}

		public async Task<DataResponse<TestRow>> GetDataAsync(DataRequest<TestRow> request, CancellationToken cancellationToken)
		{
			var total = _testData.Count;
			var items = new List<TestRow>();
			await Task.Run(() =>
			{
				var query = _testData
					.AsQueryable<TestRow>();

				// apply search criteria and get a total count of matching items
				if(!string.IsNullOrWhiteSpace(request.SearchText))
				{
					query = query.Where(x => x.NameField.Contains(request.SearchText));
				}
				total = query.Count();

				// apply sort
				if (request.SortFieldExpression != null)
				{
					if (request.SortDirection != null && request.SortDirection == SortDirection.Descending)
					{
						query = query.OrderByDescending(request.SortFieldExpression);
					}
					else
					{
						query = query.OrderBy(request.SortFieldExpression);
					}
				}

				// apply paging
				if(request.Take > 0)
				{
					query = query.Skip(request.Skip).Take(request.Take);
				}

				// realize query
				items = query.ToList();

			}).ConfigureAwait(false);
			return new DataResponse<TestRow>(items, total);
		}

		/// <summary>
		/// Requests that the item is deleted.
		/// </summary>
		/// <param name="item">The item to be deleted.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
		public async Task<OperationResponse> DeleteAsync(TestRow item, CancellationToken cancellationToken)
		{
			return new OperationResponse { ErrorMessage = "Operation not supported" };
		}

		/// <summary>
		/// Requests the given item is updated by applying the given delta.
		/// </summary>
		/// <param name="item">The original item to be updated.</param>
		/// <param name="delta">An anonymous object with new property values.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
		public async Task<OperationResponse> UpdateAsync(TestRow item, object delta, CancellationToken cancellationToken)
		{
			return new OperationResponse { ErrorMessage = "Operation not supported" };
		}

		/// <summary>
		/// Requests the given item is created.
		/// </summary>
		/// <param name="item">New item details.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
		public async Task<OperationResponse> CreateAsync(TestRow item, CancellationToken cancellationToken)
		{
			return new OperationResponse { ErrorMessage = "Operation not supported" };
		}
	}
}
