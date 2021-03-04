using PanoramicData.Blazor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.Demo.Data
{
	public class TestFileSystemDataProvider : IDataProviderService<FileExplorerItem>
	{
		private readonly Random _random = new Random(Environment.TickCount);
		private DirectoryEntry _root = new DirectoryEntry(
			new DirectoryEntry("CDrive",
				new DirectoryEntry("ProgramData",
					new DirectoryEntry("Acme",
						new DirectoryEntry("UserGuide.pdf", FileExplorerItemType.File, 10304500),
						new DirectoryEntry("Readme.txt", FileExplorerItemType.File, 65833)
					),
					new DirectoryEntry("stats.txt", FileExplorerItemType.File, 60766)
				),
				new DirectoryEntry("Temp",
					new DirectoryEntry("p21wsa.tmp", FileExplorerItemType.File, 4096) { IsHidden = true },
					new DirectoryEntry("a53fde.tmp", FileExplorerItemType.File, 1024) { IsHidden = true },
					new DirectoryEntry("b76jba.tmp", FileExplorerItemType.File, 2048) { IsHidden = true }
				),
				new DirectoryEntry("Users")
			)
			{ CanCopyMove = false },
			new DirectoryEntry("DDrive",
				new DirectoryEntry("Logs",
					new DirectoryEntry("20200502_agent.log", FileExplorerItemType.File, 600700),
					new DirectoryEntry("20200430_agent.log", FileExplorerItemType.File, 156654000),
					new DirectoryEntry("20200501_agent.log", FileExplorerItemType.File, 250001000)
				),
				new DirectoryEntry("Data",
					new DirectoryEntry("Backup",
						new DirectoryEntry("20200430_mydb.bak", FileExplorerItemType.File, 8566455),
						new DirectoryEntry("20200131_mydb.bak", FileExplorerItemType.File, 234871123),
						new DirectoryEntry("20200229_mydb.bak", FileExplorerItemType.File, 224342237),
						new DirectoryEntry("20200331_mydb.bak", FileExplorerItemType.File, 25672653),
						new DirectoryEntry("ReportBackup.zip", FileExplorerItemType.File, 127343)
					),
					new DirectoryEntry("WeeklyStats.json", FileExplorerItemType.File, 23500),
					new DirectoryEntry("MonthlyStats.json", FileExplorerItemType.File, 104999)
				),
				new DirectoryEntry("Readme.txt", FileExplorerItemType.File, 3500)
			)
			{ CanCopyMove = false }
		);

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
				try
				{
					AddFile(item);
					result.Success = true;
				}
				catch (Exception ex)
				{
					result.ErrorMessage = ex.Message;
				}
			}).ConfigureAwait(true);
			return result;
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
				var itemNode = _root.Where(x => x.Path() == item.Path).FirstOrDefault();
				if (itemNode == null)
				{
					result.ErrorMessage = "Path not found";
				}
				else
				{
					if (itemNode.Parent != null)
					{
						itemNode.Parent.Items.Remove(itemNode);
						itemNode.Parent = null;
						result.Success = true;
					}
				}
			}).ConfigureAwait(false);
			return result;
		}

		/// <summary>
		/// Sends details of a query to be performed on the underlying data set and returns the results.
		/// </summary>
		/// <param name="request">Details of the query to be performed.</param>
		/// <param name="cancellationToken">A cancellation token for the async operation.</param>
		/// <returns>A new DataResponse instance containing the result of the query.</returns>
		public async Task<DataResponse<FileExplorerItem>> GetDataAsync(DataRequest<FileExplorerItem> request, CancellationToken cancellationToken)
		{
			var total = _root.Count();
			var items = new List<FileExplorerItem>();
			// if search text given then take that as the parent path value
			// if null then return all items (load all example)
			// if empty string then return root item (load on demand example)
			if (request.SearchText is null)
			{
				_root.ForEach(x => items.Add(x.ToFileExploreritem()));
			}
			else if (string.IsNullOrWhiteSpace(request.SearchText))
			{
				total = 1;
				items.Add(_root.ToFileExploreritem());
			}
			else
			{
				items.AddRange(_root.Where(x => x.Parent?.Path() == request.SearchText).Select(x => x.ToFileExploreritem()));
				total = items.Count;
			}

			// apply sort
			if (request.SortFieldExpression != null)
			{
				var sortedItems = request.SortDirection == SortDirection.Ascending
					? items.AsQueryable<FileExplorerItem>().OrderBy(request.SortFieldExpression)
					: items.AsQueryable<FileExplorerItem>().OrderByDescending(request.SortFieldExpression);
				items = sortedItems.ToList();
			}

			// add in some random latency
			var delayMs = _random.Next(50, 800);
			await Task.Delay(delayMs).ConfigureAwait(true);

			return new DataResponse<FileExplorerItem>(items, total);
		}

		public static string GetIconClass(FileExplorerItem item)
		{
			if (item.EntryType == FileExplorerItemType.Directory)
			{
				return "far fa-fw fa-folder";
			}
			switch (item.FileExtension.ToLower())
			{
				case "doc":
				case "docx":
					return "far fa-fw fa-file-word";

				case "xls":
				case "xlsx":
					return "far fa-fw fa-file-excel";

				case "zip":
				case "gzip":
					return "far fa-fw fa-file-archive";

				case "txt":
				case "log":
					return "far fa-fw fa-file-alt";

				case "csv":
					return "far fa-fw fa-file-csv";

				case "wav":
				case "mp3":
					return "far fa-fw fa-file-audio";

				case "png":
				case "ico":
				case "gif":
				case "bmp":
				case "jpg":
				case "jpeg":
					return "far fa-fw fa-file-image";

				case "htm":
				case "html":
				case "rmscript":
					return "far fa-fw fa-file-code";

				case "pdf":
					return "far fa-fw fa-file-pdf";

				default:
					return "far fa-fw fa-file";
			}
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
				if (!delta.ContainsKey("Path"))
				{
					result.ErrorMessage = "Only Path property update supported";
					return;
				}
				var tempItem = new FileExplorerItem { Path = delta["Path"]?.ToString() };
				var targetNode = _root.Where(x => x.Path() == tempItem.Path).FirstOrDefault();
				var targetParentNode = targetNode is null ? _root.Where(x => x.Path() == tempItem.ParentPath).FirstOrDefault() : targetNode.Parent;
				if (targetParentNode is null)
				{
					result.ErrorMessage = "Invalid Path: Parent item not found";
					return;
				}

				var itemNode = _root.Where(x => x.Path() == item.Path).FirstOrDefault();
				if (itemNode is null)
				{
					result.ErrorMessage = "Item not found";
					return;
				}

				// if copy then create a deep clone of the copied item
				var isCopy = delta.ContainsKey("Copy") && string.Equals(delta["Copy"].ToString(), "true", StringComparison.OrdinalIgnoreCase);
				if (isCopy)
				{
					itemNode = itemNode.Clone();
					itemNode.DateModified = DateTimeOffset.UtcNow;
				}

				// target path does not exist - move or rename
				if (targetNode == null)
				{
					if (itemNode.Parent != null)
					{
						itemNode.Parent.Items.Remove(itemNode);
					}
					targetParentNode.Items.Add(itemNode);
					itemNode.Parent = targetParentNode;
					itemNode.Name = tempItem.Name;
				}
				else
				{
					// target path exists
					if (targetNode.Type == FileExplorerItemType.File)
					{
						// conflict - file already exists
						result.ErrorMessage = "Item already exists";
						return;
					}

					// target is folder - so move item into
					if (itemNode.Parent != null)
					{
						itemNode.Parent.Items.Remove(itemNode);
					}
					targetNode.Items.Add(itemNode);
					itemNode.Parent = targetNode;
				}

				result.Success = true;

			}).ConfigureAwait(false);
			return result;
		}

		/// <summary>
		///A new file has been added to the virtual file system model.
		/// </summary>
		/// <param name="file">Details of the new file.</param>
		public void AddFile(FileExplorerItem file)
		{
			var currentDir = _root;

			// file could be at any virtual sub folder - so descend from root creating folders as necessary
			var folderPaths = file.ParentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (folderPaths.Length > 0)
			{
				var queue = new Queue<string>(folderPaths);
				while (queue.Count > 0)
				{
					var folderName = queue.Dequeue();
					var existingItem = currentDir.Items.Find(x => x.Name.Equals(folderName, StringComparison.InvariantCultureIgnoreCase));
					if (existingItem is null)
					{
						var newDir = new DirectoryEntry
						{
							Type = FileExplorerItemType.Directory,
							Name = folderName,
							Parent = currentDir
						};
						currentDir.Items.Add(newDir);
						currentDir = newDir;
					}
					else if (existingItem.Type == FileExplorerItemType.Directory)
					{
						currentDir = existingItem;
					}
					else
					{
						throw new Exception($"Invalid file path: {file.Path}");
					}
				}
			}

			// finally add file
			currentDir.Items.Add(new DirectoryEntry
			{
				Type = FileExplorerItemType.File,
				Name = file.Name,
				Size = file.FileSize,
				CanCopyMove = file.CanCopyMove,
				DateCreated = file.DateCreated.HasValue ? file.DateCreated.Value : DateTimeOffset.UtcNow,
				DateModified = file.DateModified.HasValue ? file.DateModified.Value : DateTimeOffset.UtcNow,
				IsHidden = file.IsHidden,
				IsReadOnly = file.IsReadOnly,
				IsSystem = file.IsSystem,
				Parent = currentDir
			});
		}
	}
}
