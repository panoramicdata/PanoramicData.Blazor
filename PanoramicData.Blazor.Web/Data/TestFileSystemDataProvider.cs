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

				// if search text given then take that as the parent path value
				// and only return direct child items
				if(request.SearchText != null)
					query = query.Where(x => x.ParentPath == request.SearchText);

				total = query.Count();

				// realize query
				items = query.ToList();

			}).ConfigureAwait(false);
			return new DataResponse<FileSystemEntry>(items, total);
		}
	}
}
