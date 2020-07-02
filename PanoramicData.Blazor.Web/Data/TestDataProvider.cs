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
						StringField = _loremIpsum.Substring(0, _random.Next(0, _loremIpsum.Length)),
						StringField2 = _loremIpsum.Substring(0, _random.Next(0, _loremIpsum.Length))
					});
				}
			}
		}

		public async Task<DataResponse<TestRow>> GetDataAsync(DataRequest<TestRow> request, CancellationToken cancellationToken)
		{
			var items = new List<TestRow>();
			await Task.Run(() =>
			{
				var query = _testData
					.AsQueryable<TestRow>();
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
				if(request.Take > 0)
				{
					query = query.Skip(request.Skip).Take(request.Take);
				}
				items = query.ToList();
			}).ConfigureAwait(false);
			return new DataResponse<TestRow>(items, 55);
		}
	}
}
