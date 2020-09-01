using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Data
{
	public class PersonDataProvider : IDataProviderService<Person>
	{
		private static string _loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce at leo eu risus faucibus facilisis quis in tortor. Phasellus gravida libero sit amet ullamcorper rhoncus. Ut at viverra lectus. Vestibulum mi eros, egestas vel nulla at, lacinia ornare mauris. Morbi a pulvinar lacus. Praesent ut convallis magna. Etiam est sem, feugiat a leo in, viverra scelerisque lectus. Vivamus dictum luctus eros non ultrices. Curabitur enim enim, porta eu lorem ut, varius venenatis sem.";
		private static string[] _firstNames = new string[] { "Alice", "Bob", "Carol", "David", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy", "Mike" };
		private static string[] _lastNames = new string[] { "Smith", "Cooper", "Watkins", "Jenkins", "Tailor", "Williams", "Jones", "Smithson", "Carter", "Miller", "Baker" };
		private static Random _random = new Random(Environment.TickCount);
		private static readonly List<Person> _people = new List<Person>();

		public PersonDataProvider()
		{
			// generate random rows
			if (_people.Count() == 0)
			{
				foreach (var id in Enumerable.Range(1, 55))
				{
					_people.Add(new Person
					{
						Id = id,
						AllowLogin = _random.Next(0, 2) == 1,
						DateCreated = DateTimeOffset.Now.AddDays(_random.Next(0, 7)),
						Department = (Departments)_random.Next(0, 4),
						FirstName = _firstNames[_random.Next(_firstNames.Length)],
						LastName = _lastNames[_random.Next(_lastNames.Length)],
						Comments = _loremIpsum.Substring(0, _random.Next(0, _loremIpsum.Length))
					});
				}
			}
		}

		public async Task<DataResponse<Person>> GetDataAsync(DataRequest<Person> request, CancellationToken cancellationToken)
		{
			var total = _people.Count;
			var items = new List<Person>();
			await Task.Run(() =>
			{
				var query = _people
					.AsQueryable<Person>();

				// apply search criteria and get a total count of matching items
				if(!string.IsNullOrWhiteSpace(request.SearchText))
				{
					query = query.Where(x => x.FirstName.Contains(request.SearchText) ||x.LastName.Contains(request.SearchText));
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
			return new DataResponse<Person>(items, total);
		}

		/// <summary>
		/// Requests that the item is deleted.
		/// </summary>
		/// <param name="item">The item to be deleted.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
		public async Task<OperationResponse> DeleteAsync(Person item, CancellationToken cancellationToken)
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
		public async Task<OperationResponse> UpdateAsync(Person item, object delta, CancellationToken cancellationToken)
		{
			return new OperationResponse { ErrorMessage = "Operation not supported" };
		}

		/// <summary>
		/// Requests the given item is created.
		/// </summary>
		/// <param name="item">New item details.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
		public async Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
		{
			return new OperationResponse { ErrorMessage = "Operation not supported" };
		}
	}
}
