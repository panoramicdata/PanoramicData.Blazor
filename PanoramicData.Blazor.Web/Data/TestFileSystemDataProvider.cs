using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Data
{
	public class TestFileSystemDataProvider : IDataProviderService<FileSystemEntry>
	{
		private readonly List<FileSystemEntry> _testData = new List<FileSystemEntry>();

		public TestFileSystemDataProvider()
		{
			// generate test data
			_testData.Add(new FileSystemEntry { Path = "My Computer" });
			_testData.Add(new FileSystemEntry { Path = @"C:\", ParentPath = "My Computer" });
			_testData.Add(new FileSystemEntry { Path = @"C:\ProgramData" });
			_testData.Add(new FileSystemEntry { Path = @"C:\ProgramData\Acme" });
			_testData.Add(new FileSystemEntry { Path = @"C:\ProgramData\Acme\Readme.txt", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"C:\ProgramData\Acme\UserGuide.pdf", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"C:\ProgramData\stats.txt", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"C:\Temp" });
			_testData.Add(new FileSystemEntry { Path = @"C:\Temp\a53fde.tmp", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"C:\Temp\b76jba.tmp", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"C:\Temp\p21wsa.tmp", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"C:\Users" });
			_testData.Add(new FileSystemEntry { Path = @"D:\", ParentPath = "My Computer" });
			_testData.Add(new FileSystemEntry { Path = @"D:\Data" });
			_testData.Add(new FileSystemEntry { Path = @"D:\Data\Backup" });
			_testData.Add(new FileSystemEntry { Path = @"D:\Data\Backup\20200131_mydb.bak", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"D:\Data\Backup\20200229_mydb.bak", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"D:\Data\Backup\20200331_mydb.bak", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"D:\Data\Backup\20200430_mydb.bak", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"D:\Data\WeeklyStats.json", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"D:\Data\MonthlyStats.json", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"D:\Logs" });
			_testData.Add(new FileSystemEntry { Path = @"D:\Logs\20200430_agent.log", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"D:\Logs\20200501_agent.log", EntryType = FileSystemEntryTypes.File });
			_testData.Add(new FileSystemEntry { Path = @"D:\Logs\20200502_agent.log", EntryType = FileSystemEntryTypes.File });
		}

		public async Task<DataResponse<FileSystemEntry>> GetDataAsync(DataRequest<FileSystemEntry> request, CancellationToken cancellationToken)
		{
			var total = _testData.Count;
			var items = new List<FileSystemEntry>();
			await Task.Run(() =>
			{
				var query = _testData
					.AsQueryable<FileSystemEntry>();

				// apply search criteria and get a total count of matching items
				if(!string.IsNullOrWhiteSpace(request.SearchText))
				{
					query = query.Where(x => x.Name.Contains(request.SearchText));
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
				//if(request.Take > 0)
				//{
				//	query = query.Skip(request.Skip).Take(request.Take);
				//}

				// realize query
				items = query.ToList();

			}).ConfigureAwait(false);
			return new DataResponse<FileSystemEntry>(items, total);
		}
	}
}
