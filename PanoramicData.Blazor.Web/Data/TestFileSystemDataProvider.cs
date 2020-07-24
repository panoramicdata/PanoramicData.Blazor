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

		/// <summary>
		/// Gets or sets the root folder. All operations can only be performed under this folder.
		/// </summary>
		public string RootFolder { get; set; } = @"My Computer";

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
			_testData.Add(new FileExplorerItem { Path = @"C:\Temp\a53fde.tmp", ParentPath = @"C:\Temp", EntryType = FileExplorerItemType.File, IsHidden = true });
			_testData.Add(new FileExplorerItem { Path = @"C:\Temp\b76jba.tmp", ParentPath = @"C:\Temp", EntryType = FileExplorerItemType.File, IsHidden = true });
			_testData.Add(new FileExplorerItem { Path = @"C:\Temp\p21wsa.tmp", ParentPath = @"C:\Temp", EntryType = FileExplorerItemType.File, IsHidden = true });
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

		/// <summary>
		/// Sends details of a query to be performed on the underlying data set and returns the results.
		/// </summary>
		/// <param name="request">Details of the query to be performed.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new DataResponse instance containing the result of the query.</returns>
		public async Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
		{
			var total = _testData.Count;
			var items = new List<FileExplorerItem>();
			await Task.Run(() =>
			{
				var query = _testData
					.AsQueryable<FileExplorerItem>();

				// if search text given then take that as the parent path value
				// if null then return all items (load all example)
				// if empty string then return root item (load on demand example)
				if (request.SearchText != null)
				{
					if (request.SearchText == string.Empty)
					{
						query = query.Where(x => x.Path == RootFolder);
					}
					else
					{
						query = query.Where(x => x.ParentPath == request.SearchText);
					}
				}

				total = query.Count();

				// realize query
				items = query.ToList();

				// remove parent path from all root items
				items.ForEach(x => x.ParentPath = x.Path == RootFolder ? string.Empty : x.ParentPath);

			}).ConfigureAwait(false);
			return new DataResponse<FileExplorerItem>(items, total);
		}

		/// <summary>
		/// Requests that the item is deleted.
		/// </summary>
		/// <param name="item">The item to be deleted.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
		public async Task<OperationResponse> DeleteAsync(FileExplorerItem item, CancellationToken cancellationToken)
		{
			var result = new OperationResponse();
			await Task.Run(() =>
			{
				if(_testData.RemoveAll(x => item.EntryType == FileExplorerItemType.Directory
					? x.Path.StartsWith(item.Path)
					: x.Path == item.Path) > 0)
				{
					result.Success = true;
				}
				else
				{
					result.ErrorMessage = "Path not found";
				}
			}).ConfigureAwait(false);
			return result;
		}
	}
}
