using System.Security.Cryptography;

namespace PanoramicData.Blazor.Demo.Data;

public class TestFileSystemDataProvider : IDataProviderService<FileExplorerItem>
{
	private readonly DirectoryEntry _root = CreateRoot();

	private static DirectoryEntry CreateRoot()
	{
		var root = new DirectoryEntry(
			CreateLibrary(),
			CreateUsers(),
			CreateCDrive(),
			CreateDDrive(),
			CreateSharepoint());
		root.Alias = "/";
		return root;
	}

	private static DirectoryEntry CreateLibrary()
		=> new("Library", true, false, false,
			new DirectoryEntry("Templates", true, false, false,
				new DirectoryEntry("web_template.html", FileExplorerItemType.File, 13000, true, false, false),
				new DirectoryEntry("excel_template.xlsx", FileExplorerItemType.File, 7500, true, false, false),
				new DirectoryEntry("word_template.docx", FileExplorerItemType.File, 10000, true, false, false)
			)
		);

	private static DirectoryEntry CreateUsers()
	{
		var alice = new DirectoryEntry("1", false, false, false,
			new DirectoryEntry("summary.xlsx", FileExplorerItemType.File, 5012, false),
			new DirectoryEntry("instruction.docx", FileExplorerItemType.File, 4320, false),
			new DirectoryEntry("example.md", FileExplorerItemType.File, 2647, false),
			new DirectoryEntry("lorem_ipsum.txt", FileExplorerItemType.File, 1424, false),
			new DirectoryEntry("simple_example.html", FileExplorerItemType.File, 21155, false),
			new DirectoryEntry("web_shortcut.url", FileExplorerItemType.File, 55, false)
		);
		alice.Alias = "Alice";
		var bob = new DirectoryEntry("2", false, false, false,
			new DirectoryEntry("notes.docx", FileExplorerItemType.File, 2000, false)
		);
		bob.Alias = "Bob";
		return new DirectoryEntry("Users", true, false, false, alice, bob);
	}

	private static DirectoryEntry CreateCDrive()
	{
		var cDrive = new DirectoryEntry("CDrive",
			new DirectoryEntry("ProgramData",
				new DirectoryEntry("Acme",
					new DirectoryEntry("UserGuide.pdf", FileExplorerItemType.File, 10304500),
					new DirectoryEntry("Readme.txt", FileExplorerItemType.File, 65833)
				),
				new DirectoryEntry("stats.txt", FileExplorerItemType.File, 60766)
			),
			new DirectoryEntry("Temp",
				VisibleFile("1gigabyte.tmp", 1096000000),
				VisibleFile("1kilobyte.tmp", 1024),
				VisibleFile("2kilobytes.tmp", 2048),
				VisibleFile("4bytes.tmp", 4),
				VisibleFile("empty.tmp", 0)
			),
			new DirectoryEntry("Cache",
				new DirectoryEntry("document.docx", FileExplorerItemType.File, 4096),
				new DirectoryEntry("spreadsheet.xlsx", FileExplorerItemType.File, 2048)
			)
		);
		cDrive.CanCopyMove = false;
		return cDrive;
	}

	private static DirectoryEntry VisibleFile(string name, int size)
	{
		var file = new DirectoryEntry(name, FileExplorerItemType.File, size);
		file.IsHidden = false;
		return file;
	}

	private static DirectoryEntry CreateDDrive()
	{
		var dDrive = new DirectoryEntry("DDrive",
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
			new DirectoryEntry("Folders", [.. Enumerable.Range(1, 29).Select(i => new DirectoryEntry($"Folder{i:00}"))]),
			new DirectoryEntry("Readme.txt", FileExplorerItemType.File, 3500)
		);
		dDrive.CanCopyMove = false;
		return dDrive;
	}

	private static DirectoryEntry CreateSharepoint()
		=> new("Sharepoint", true, false, false,
			new DirectoryEntry("Public", true, false, false,
				new DirectoryEntry("web_template.html", FileExplorerItemType.File, 13000, true, false, false),
				new DirectoryEntry("excel_template.xlsx", FileExplorerItemType.File, 7500, true, false, false),
				new DirectoryEntry("word_template.docx", FileExplorerItemType.File, 10000, true, false, false)
			)
		);

	public TestFileSystemDataProvider()
	{
		var itemNode = _root.Where(x => x.Path() == "/CDrive/Temp").FirstOrDefault();
		if (itemNode != null)
		{
			for (var i = 0; i < 50; i++)
			{
				var childItem = new DirectoryEntry($"datafile-{i + 1:00}.dat", FileExplorerItemType.File, RandomNumberGenerator.GetInt32(100000))
				{
					Parent = itemNode
				};
				itemNode.Items.Add(childItem);
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
			try
			{
				AddFileItem(item);
				result.Success = true;
			}
			catch (Exception ex)
			{
				result.ErrorMessage = ex.Message;
			}
		}, cancellationToken).ConfigureAwait(true);
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
		}, cancellationToken).ConfigureAwait(false);
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
				? items.AsQueryable().OrderBy(request.SortFieldExpression)
				: items.AsQueryable().OrderByDescending(request.SortFieldExpression);
			items = [.. sortedItems];

			// move Library folder to the top of the list - if displayed
			if (items.SingleOrDefault(i => i.Path == "/Library") is FileExplorerItem libraryFolder)
			{
				items.RemoveAt(items.IndexOf(libraryFolder));
				items.Insert(0, libraryFolder);
			}

			// move Users folder to the top of the list (after library) - if displayed
			if (items.SingleOrDefault(i => i.Path == "/Users") is FileExplorerItem usersFolder)
			{
				items.RemoveAt(items.IndexOf(usersFolder));
				items.Insert(1, usersFolder);
			}
		}

		// add in some random latency
		var delayMs = RandomNumberGenerator.GetInt32(50, 800);
		await Task.Delay(delayMs, cancellationToken).ConfigureAwait(true);

		return new DataResponse<FileExplorerItem>(items, total);
	}

	public static string GetIconClass(FileExplorerItem item)
	{
		if (item.EntryType == FileExplorerItemType.Directory)
		{
			return "fas fa-fw fa-folder";
		}

		return item.FileExtension.ToLowerInvariant() switch
		{
			"doc" or "docx" => "fas fa-fw fa-file-word",
			"xls" or "xlsx" => "fas fa-fw fa-file-excel",
			"zip" or "gzip" => "fas fa-fw fa-file-archive",
			"txt" or "log" => "far fa-fw fa-file-alt",
			"csv" => "fas fa-fw fa-file-csv",
			"wav" or "mp3" => "fas fa-fw fa-file-audio",
			"png" or "ico" or "gif" or "bmp" or "jpg" or "jpeg" => "fas fa-fw fa-file-image",
			"htm" or "html" or "rmscript" => "fas fa-fw fa-file-code",
			"pdf" => "fas fa-fw fa-file-pdf",
			_ => "far fa-fw fa-file",
		};
	}

	/// <summary>
	/// Requests the given item is updated by applying the given delta.
	/// </summary>
	/// <param name="item">The original item to be updated.</param>
	/// <param name="delta">A dictionary with new property values.</param>
	/// <param name="cancellationToken">A cancellation token for the async operation.</param>
	/// <returns>A new OperationResponse instance that contains the results of the operation.</returns>
	public async Task<OperationResponse> UpdateAsync(FileExplorerItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
	{
		var result = new OperationResponse();

		await Task.Run(() =>
		{
			var errorMessage = ApplyPathUpdate(item, delta);
			if (errorMessage is null)
			{
				result.Success = true;
			}
			else
			{
				result.ErrorMessage = errorMessage;
			}
		}, cancellationToken).ConfigureAwait(true);
		return result;
	}

	private DirectoryEntry? FindNode(string path) => _root.Where(x => x.Path() == path).FirstOrDefault();

	/// <summary>
	/// Moves, renames or copies an item to the path given in the delta.
	/// </summary>
	/// <returns>null on success, otherwise an error message.</returns>
	private string? ApplyPathUpdate(FileExplorerItem item, IDictionary<string, object?> delta)
	{
		if (!delta.TryGetValue("Path", out object? value))
		{
			return "Only Path property update supported";
		}

		var tempItem = CreatePathItem(value?.ToString());
		var targetNode = FindNode(tempItem.Path);
		var targetParentNode = GetTargetParentNode(targetNode, tempItem);
		if (targetParentNode is null)
		{
			return "Invalid Path: Parent item not found";
		}

		var itemNode = FindNode(item.Path);
		if (itemNode is null)
		{
			return "Item not found";
		}

		// if copy then create a deep clone of the copied item
		if (IsCopy(delta))
		{
			itemNode = itemNode.Clone();
			itemNode.DateModified = DateTimeOffset.UtcNow;
		}

		// target path does not exist - move or rename, otherwise move into the existing target
		return targetNode is null
			? MoveOrRename(itemNode, targetParentNode, tempItem.Name)
			: MoveInto(itemNode, targetNode);
	}

	private static FileExplorerItem CreatePathItem(string? path)
	{
		var tempPath = path ?? string.Empty;
		return new FileExplorerItem { Path = tempPath, Name = FileExplorerItem.GetNameFromPath(tempPath) };
	}

	private DirectoryEntry? GetTargetParentNode(DirectoryEntry? targetNode, FileExplorerItem tempItem)
		=> targetNode is null ? FindNode(tempItem.ParentPath) : targetNode.Parent;

	private static bool IsCopy(IDictionary<string, object?> delta)
		=> delta.TryGetValue("Copy", out var copy) && string.Equals(copy?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

	private static string? MoveOrRename(DirectoryEntry itemNode, DirectoryEntry targetParentNode, string newName)
	{
		// simulate rename/move error
		if (newName.Contains(".."))
		{
			return "Failed to move: Invalid name";
		}

		itemNode.Parent?.Items.Remove(itemNode);

		targetParentNode.Items.Add(itemNode);
		itemNode.Parent = targetParentNode;
		itemNode.Name = newName;
		return null;
	}

	private static string? MoveInto(DirectoryEntry itemNode, DirectoryEntry targetNode)
	{
		if (targetNode.Type == FileExplorerItemType.File)
		{
			// conflict - file already exists
			return "Item already exists";
		}

		// target is folder - so move item into
		itemNode.Parent?.Items.Remove(itemNode);

		targetNode.Items.Add(itemNode);
		itemNode.Parent = targetNode;
		return null;
	}

	/// <summary>
	///A new file has been added to the virtual file system model.
	/// </summary>
	/// <param name="file">Details of the new file.</param>
	public void AddFileItem(FileExplorerItem file)
	{
		var currentDir = EnsureParentFolders(file);

		// finally add file / folder
		var isDirectory = file.EntryType == FileExplorerItemType.Directory;
		currentDir.Items.Add(new DirectoryEntry
		{
			Type = isDirectory ? FileExplorerItemType.Directory : FileExplorerItemType.File,
			Name = file.Name,
			Size = isDirectory ? 0 : file.FileSize,
			CanCopyMove = file.CanCopyMove,
			DateCreated = file.DateCreated ?? DateTimeOffset.UtcNow,
			DateModified = file.DateModified ?? DateTimeOffset.UtcNow,
			IsHidden = file.IsHidden,
			IsReadOnly = file.IsReadOnly,
			IsSystem = file.IsSystem,
			Parent = currentDir
		});
	}

	private DirectoryEntry EnsureParentFolders(FileExplorerItem file)
	{
		var currentDir = _root;

		// file could be at any virtual sub folder - so descend from root creating folders as necessary
		foreach (var folderName in file.ParentPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
		{
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
				throw new InvalidOperationException($"Invalid file path: {file.Path}");
			}
		}

		return currentDir;
	}
}
