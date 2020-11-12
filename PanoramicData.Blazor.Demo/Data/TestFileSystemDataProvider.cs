using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor.Demo.Data
{
	public class TestFileSystemDataProvider : IDataProviderService<FileExplorerItem>
	{
		private readonly List<FileExplorerItem> _testData = new List<FileExplorerItem>();
		private readonly Random _random = new Random(Environment.TickCount);

		public TestFileSystemDataProvider()
		{
			// generate test data - Paths can not edit with trailing slashes
			_testData.Add(new FileExplorerItem { Path = @"/", CanCopyMove = false });
			_testData.Add(new FileExplorerItem { Path = @"/C:", CanCopyMove = false });
			_testData.Add(new FileExplorerItem { Path = @"/C:/ProgramData" });
			_testData.Add(new FileExplorerItem { Path = @"/C:/ProgramData/Acme", HasSubFolders = false });
			_testData.Add(new FileExplorerItem { Path = @"/C:/ProgramData/Acme/Readme.txt", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 65833 });
			_testData.Add(new FileExplorerItem { Path = @"/C:/ProgramData/Acme/UserGuide.pdf", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 10304500 });
			_testData.Add(new FileExplorerItem { Path = @"/C:/ProgramData/stats.txt", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 60766 });
			_testData.Add(new FileExplorerItem { Path = @"/C:/Temp", HasSubFolders = false });
			_testData.Add(new FileExplorerItem { Path = @"/C:/Temp/a53fde.tmp", EntryType = FileExplorerItemType.File, IsHidden = true, DateModified = DateTimeOffset.UtcNow, FileSize = 1024 });
			_testData.Add(new FileExplorerItem { Path = @"/C:/Temp/b76jba.tmp", EntryType = FileExplorerItemType.File, IsHidden = true, DateModified = DateTimeOffset.UtcNow, FileSize = 2048 });
			_testData.Add(new FileExplorerItem { Path = @"/C:/Temp/p21wsa.tmp", EntryType = FileExplorerItemType.File, IsHidden = true, DateModified = DateTimeOffset.UtcNow, FileSize = 4096 });
			_testData.Add(new FileExplorerItem { Path = @"/C:/Users", HasSubFolders = false });
			_testData.Add(new FileExplorerItem { Path = @"/D:", CanCopyMove = false });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Logs", HasSubFolders = false });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Logs/20200502_agent.log", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 600700 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Logs/20200430_agent.log", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 156654000 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Logs/20200501_agent.log", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 250001000 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Data" });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Data/Backup", HasSubFolders = false });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Data/Backup/20200131_mydb.bak", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 234871123 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Data/Backup/20200229_mydb.bak", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 224342237 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Data/Backup/20200331_mydb.bak", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 25672653 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Data/Backup/20200430_mydb.bak", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 8566455 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Data/WeeklyStats.json", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 23500 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Data/MonthlyStats.json", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 104999 });
			_testData.Add(new FileExplorerItem { Path = @"/D:/Readme.txt", EntryType = FileExplorerItemType.File, DateModified = DateTimeOffset.UtcNow, FileSize = 3500 });
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

			var query = _testData
				.AsQueryable<FileExplorerItem>();

			// apply sort
			if (request.SortFieldExpression != null)
			{
				query = request.SortDirection == SortDirection.Ascending
					? query.OrderBy(request.SortFieldExpression)
					: query.OrderByDescending(request.SortFieldExpression);
			}

			// if search text given then take that as the parent path value
			// if null then return all items (load all example)
			// if empty string then return root item (load on demand example)
			if (request.SearchText is null)
			{
				total = query.Count();
				items = query.ToList();
			}
			else if (string.IsNullOrWhiteSpace(request.SearchText))
			{
				total = 1;
				items.Add(new FileExplorerItem { Path = "/" });
			}
			else
			{
				query = query.Where(x => x.ParentPath == request.SearchText);
				total = query.Count();
				items = query.ToList();
			}

			// add in some random latency
			var delayMs = _random.Next(50, 800);
			await Task.Delay(delayMs).ConfigureAwait(true);

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

		/// <summary>
		/// Requests the given item is updated by applying the given delta.
		/// </summary>
		/// <param name="item">The original item to be updated.</param>
		/// <param name="delta">A dictionary with new property values.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
		public async Task<OperationResponse> UpdateAsync(FileExplorerItem item, IDictionary<string, object> delta, CancellationToken cancellationToken)
		{
			var result = new OperationResponse();
			await Task.Run(() =>
			{
				// find original item
				var existingItem = _testData.FirstOrDefault(x => x.Path == item.Path);
				if(existingItem == null)
				{
					result.ErrorMessage = "Item not found";
				}
				else
				{
					// only path updates supported
					if(!delta.ContainsKey("Path"))
					{
						result.ErrorMessage = "Only Path property update supported";
					}
					else
					{
						var newPath = delta["Path"]?.ToString();
						if(string.IsNullOrWhiteSpace(newPath))
						{
							result.ErrorMessage = "Invalid value for Path property";
						}
						else
						{
							// check for move/copy - is the target a directory?
							var targetItem = _testData.FirstOrDefault(x => x.Path == newPath);
							if(targetItem?.EntryType == FileExplorerItemType.Directory)
							{
								// check for conflict
								targetItem = _testData.FirstOrDefault(x => x.Path == $"{newPath}/{existingItem.Name}");
								if(targetItem != null)
								{
									result.ErrorMessage = "Conflict";
									return;
								}

								// move / copy
								var isCopy = delta.ContainsKey("Copy") && string.Equals(delta["Copy"].ToString(), "true", StringComparison.OrdinalIgnoreCase);
								MoveOrCopyItem(existingItem, newPath, isCopy);
							}
							else
							{
								// rename
								var previousPath = existingItem.Path;
								existingItem.Path = newPath;
								if (existingItem.EntryType == FileExplorerItemType.Directory)
								{
									// update child paths
									_testData.ForEach(x =>
									{
										x.Path = x.Path.ReplacePathPrefix(previousPath, newPath);
									});
								}
							}
							result.Success = true;
						}
					}
				}
			}).ConfigureAwait(false);
			return result;
		}

		private void MoveOrCopyItem(FileExplorerItem item, string parentPath, bool isCopy)
		{
			if (item.EntryType == FileExplorerItemType.File)
			{
				var existingItem = _testData.FirstOrDefault(x => x.Path == $"{parentPath}/{item.Name}");
				if (existingItem != null)
				{
					existingItem.DateModified = DateTime.UtcNow; // update to indicate written
					if (!isCopy)
					{
						_testData.Remove(item);
					}
				}
				else if (isCopy)
				{
					_testData.Add(new FileExplorerItem
					{
						DateCreated = DateTime.UtcNow,
						DateModified = DateTime.UtcNow, // update to indicate written
						EntryType = FileExplorerItemType.File,
						FileSize = item.FileSize,
						IsHidden = item.IsHidden,
						IsSystem = item.IsSystem,
						IsReadOnly = item.IsReadOnly,
						Path = $"{parentPath}/{item.Name}"
					});
				}
				else
				{
					item.Path = $"{parentPath}/{item.Name}";
				}
			}
			else
			{
				// first move / copy folder entry
				var originalPath = item.Path;
				if (isCopy)
				{
					_testData.Add(new FileExplorerItem
					{
						DateCreated = DateTime.UtcNow,
						DateModified = item.DateModified,
						EntryType = FileExplorerItemType.Directory,
						Path = $"{parentPath}/{item.Name}"
					});
				}
				else
				{
					item.Path = $"{parentPath}/{item.Name}";
				}

				// move / copy child items
				var subItems = _testData.Where(x => x.ParentPath == originalPath);
				foreach(var subItem in subItems)
				{
					MoveOrCopyItem(subItem, item.Path, isCopy);
				}
			}
		}

		/// <summary>
		/// Requests the given item is created.
		/// </summary>
		/// <param name="item">New item details.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
		public async Task<OperationResponse> CreateAsync(FileExplorerItem item, CancellationToken cancellationToken)
		{
			var result = new OperationResponse();
			await Task.Run(() =>
			{
				item.DateCreated = item.DateModified = DateTimeOffset.UtcNow;
				_testData.Add(item);
				result.Success = true;
			});
			return result;
		}
	}
}
