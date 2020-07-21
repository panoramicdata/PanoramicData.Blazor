using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Web.Data
{
	public class TestFileSystemDataProvider : IDataProviderService<FileExplorerItem>
	{
		private readonly List<FileExplorerItem> _testData = new List<FileExplorerItem>();

		public TestFileSystemDataProvider()
		{
			// generate test data
			_testData.Add(new FileExplorerItem { Path = "My Computer" });
			_testData.Add(new FileExplorerItem { Path = @"C:\", ParentPath = "My Computer" });
			_testData.Add(new FileExplorerItem { Path = @"C:\ProgramData", ParentPath = @"C:\" });
			_testData.Add(new FileExplorerItem { Path = @"C:\ProgramData\Acme", ParentPath = @"C:\ProgramData" });
			_testData.Add(new FileExplorerItem { Path = @"C:\ProgramData\Acme\Readme.txt", ParentPath = @"C:\ProgramData\Acme", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"C:\ProgramData\Acme\UserGuide.pdf", ParentPath = @"C:\ProgramData\Acme", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"C:\ProgramData\stats.txt", ParentPath = @"C:\ProgramData", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"C:\Temp", ParentPath = @"C:\" });
			_testData.Add(new FileExplorerItem { Path = @"C:\Temp\a53fde.tmp", ParentPath = @"C:\Temp", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"C:\Temp\b76jba.tmp", ParentPath = @"C:\Temp", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"C:\Temp\p21wsa.tmp", ParentPath = @"C:\Temp", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"C:\Users", ParentPath = @"C:\" });
			_testData.Add(new FileExplorerItem { Path = @"D:\", ParentPath = "My Computer" });
			_testData.Add(new FileExplorerItem { Path = @"D:\Data", ParentPath = @"D:\" });
			_testData.Add(new FileExplorerItem { Path = @"D:\Data\Backup", ParentPath = @"D:\Data" });
			_testData.Add(new FileExplorerItem { Path = @"D:\Data\Backup\20200131_mydb.bak", ParentPath = @"D:\Data\Backup", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"D:\Data\Backup\20200229_mydb.bak", ParentPath = @"D:\Data\Backup", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"D:\Data\Backup\20200331_mydb.bak", ParentPath = @"D:\Data\Backup", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"D:\Data\Backup\20200430_mydb.bak", ParentPath = @"D:\Data\Backup", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"D:\Data\WeeklyStats.json", ParentPath = @"D:\Data", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"D:\Data\MonthlyStats.json", ParentPath = @"D:\Data", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"D:\Logs", ParentPath = @"D:\" });
			_testData.Add(new FileExplorerItem { Path = @"D:\Logs\20200430_agent.log", ParentPath = @"D:\Logs", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"D:\Logs\20200501_agent.log", ParentPath = @"D:\Logs", EntryType = FileExplorerItemType.File });
			_testData.Add(new FileExplorerItem { Path = @"D:\Logs\20200502_agent.log", ParentPath = @"D:\Logs", EntryType = FileExplorerItemType.File });
		}

		public async Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
		{
			var total = _testData.Count;
			var items = new List<FileExplorerItem>();
			await Task.Run(() =>
			{
				var query = _testData
					.AsQueryable<FileExplorerItem>();

				// if search text given then take that as the parent path value
				// and only return direct child items
				if(request.SearchText != null)
					query = query.Where(x => x.ParentPath == request.SearchText);

				total = query.Count();

				// realize query
				items = query.ToList();

			}).ConfigureAwait(false);
			return new DataResponse<FileExplorerItem>(items, total);
		}
	}
}
