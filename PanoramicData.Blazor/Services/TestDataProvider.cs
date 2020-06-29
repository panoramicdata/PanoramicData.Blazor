using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PanoramicData.Blazor.Services
{
	public class TestDataProvider : IDataProviderService<TestRow>
	{
		private static string _loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce at leo eu risus faucibus facilisis quis in tortor. Phasellus gravida libero sit amet ullamcorper rhoncus. Ut at viverra lectus. Vestibulum mi eros, egestas vel nulla at, lacinia ornare mauris. Morbi a pulvinar lacus. Praesent ut convallis magna. Etiam est sem, feugiat a leo in, viverra scelerisque lectus. Vivamus dictum luctus eros non ultrices. Curabitur enim enim, porta eu lorem ut, varius venenatis sem. Etiam ullamcorper mollis congue. Vivamus faucibus augue lectus, a tincidunt velit commodo faucibus. Nunc congue lacus eu urna aliquet feugiat. Sed iaculis tristique varius. Cras rhoncus euismod molestie.Quisque bibendum venenatis lectus sit amet interdum. Nulla condimentum justo sit amet ultricies ornare.Sed lectus nulla, vulputate sed aliquam id, pharetra sit amet augue. Etiam vitae enim tempor, laoreet lectus a, placerat tellus. Integer vitae dapibus nunc. Proin aliquet hendrerit fermentum. Fusce sit amet elit arcu.Sed dignissim purus sed mi consequat mollis nec placerat urna. Quisque bibendum eu massa vel gravida. Nulla lobortis dapibus vestibulum. Fusce fermentum sit amet lectus eget dapibus.Nulla non dolor sagittis, varius purus ac, lacinia metus. Nulla sit amet vulputate mauris, ut suscipit purus. Nullam egestas felis a accumsan convallis. Ut vitae tincidunt ligula. Aliquam sollicitudin elementum purus a fringilla. Proin suscipit aliquam accumsan. Donec velit justo, mattis a aliquet nec, finibus tempus leo.Phasellus nunc justo, varius sed leo vitae, commodo sagittis eros.Nam et mauris sapien. Nam eleifend dui vitae tortor vehicula, eget imperdiet urna tincidunt. Nullam sem mauris, suscipit sit amet congue a, elementum at velit.";
		private static Random _random = new Random(Environment.TickCount);
		private static readonly List<TestRow> _testData = new List<TestRow>();

		public TestDataProvider()
		{
			// generate random rows
			foreach (var id in Enumerable.Range(1, 55))
			{
				_testData.Add(new TestRow
				{
					IntField = id,
					BooleanField = _random.Next(0, 2) == 1,
					DateField = DateTimeOffset.Now.AddDays(_random.Next(0, 7)),
					StringField = _loremIpsum.Substring(0, _random.Next(0, _loremIpsum.Length))
				});
			}
		}

		public async Task<DataResponse<TestRow>> GetDataAsync(DataRequest<TestRow> request, CancellationToken cancellationToken)
		{
			var items = new List<TestRow>();
			await Task.Run(() =>
			{
				items = _testData
					.Skip(request.Skip)
					.Take(request.Take)
					//.OrderBy(request.SortFieldExpression)
					.ToList();
			}).ConfigureAwait(false);
			return new DataResponse<TestRow>(items, 55);
		}
	}
}
