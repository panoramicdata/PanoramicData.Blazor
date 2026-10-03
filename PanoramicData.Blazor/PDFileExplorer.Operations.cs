namespace PanoramicData.Blazor;

/// <summary>
/// Toolbar, navigation and file operations of a <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorer
{
	private string _deleteDialogMessage = string.Empty;
	private string _conflictDialogMessage = string.Empty;
	private string[] _conflictDialogList = [];
	private double[] _lastSplitSizes = [20, 60, 20];

	private async Task OnTogglePreviewPanelAsync()
	{
		if (Splitter != null)
		{
			if (PreviewPanelVisible)
			{
				_lastSplitSizes = await Splitter.GetSizesAsync().ConfigureAwait(true);
				if (_lastSplitSizes.Length > 2)
				{
					await Splitter.SetSizesAsync([_lastSplitSizes[0], _lastSplitSizes[1] + _lastSplitSizes[2], 0]).ConfigureAwait(true);
				}
			}
			else
			{
				await Splitter.SetSizesAsync(_lastSplitSizes).ConfigureAwait(true);
			}

			PreviewPanelVisible = !PreviewPanelVisible;

			await RefreshToolbarAsync();
		}
	}

	private async Task OnToolbarButtonClickAsync(KeyedEventArgs<MouseEventArgs> args)
		=> await (args.Key switch
		{
			"navigate-up" => NavigateUpAsync(),
			"open" => OpenSelectedFolderAsync(),
			"create-folder" => CreateNewFolderAsync(false),
			"delete" => DeleteFilesAsync(),
			"upload" => ShowUploadDialogAsync(),
			"refresh" => RefreshAllAsync(),
			"preview" when PreviewPanel is FilePreviewModes.OptionalOff or FilePreviewModes.OptionalOn => OnTogglePreviewPanelAsync(),
			"preview" => Task.CompletedTask,
			_ => ToolbarClick.InvokeAsync(args.Key)
		}).ConfigureAwait(true);

	private async Task OpenSelectedFolderAsync()
	{
		var selectedFolderPath = Table?.Selection[0];
		if (selectedFolderPath != null)
		{
			await NavigateFolderAsync(selectedFolderPath).ConfigureAwait(true);
		}
	}

	private async Task OnDropAsync(DropEventArgs args)
	{
		// unwrap FileExplorerItem
		args.Target = args.Target switch
		{
			null => _selectedNode?.Data,
			TreeNode<FileExplorerItem> node => node.Data,
			var other => other
		};

		// source and target are file items - and target is folder?
		// and the folder accepts new items (the tree checks this before a drop, the table does not)?
		if (args.Target is not FileExplorerItem { EntryType: FileExplorerItemType.Directory } target || !CanDropInto(target))
		{
			return;
		}

		var payload = args.Payload switch
		{
			List<FileExplorerItem> items => items,
			FileExplorerItem item => [item],
			_ => new List<FileExplorerItem>()
		};

		// check not dropping an item onto itself (or sub folder), and can move/copy all items
		if (payload.Any(x => x.Path == target.Path || target.Path.StartsWith(x.Path, StringComparison.InvariantCultureIgnoreCase))
			|| payload.Any(x => !x.CanCopyMove))
		{
			return;
		}

		// move items into folder
		await MoveCopyFilesAsync(payload, target.Path, args.Ctrl).ConfigureAwait(true);
	}

	/// <summary>
	/// Whether a drop may add items to the given folder. The ".." row is always read-only itself, so it defers to
	/// the parent folder it stands for.
	/// </summary>
	private bool CanDropInto(FileExplorerItem folder)
		=> IsParentDirectoryItem(folder)
			? _selectedNode?.ParentNode?.Data?.CanAddItems != false
			: folder.CanAddItems;

	/// <summary>
	/// Replaces the payload of a later paste with the given items.
	/// </summary>
	/// <param name="items">The items to copy or move.</param>
	/// <param name="move">True when the items are cut, to be moved by the paste rather than copied.</param>
	/// <returns>A completed task, so that this can be one of several menu or key actions.</returns>
	private Task SetCopyPayload(IEnumerable<FileExplorerItem> items, bool move)
	{
		_copyPayload.Clear();
		_copyPayload.AddRange(items);
		_moveCopyPayload = move;
		return Task.CompletedTask;
	}

	/// <summary>
	/// Copies or moves the payload into the given folder.
	/// </summary>
	private async Task PasteAsync(string targetPath)
	{
		await MoveCopyFilesAsync(_copyPayload, targetPath, !_moveCopyPayload).ConfigureAwait(true);
		if (_moveCopyPayload) // clear copy payload only if move
		{
			_copyPayload.Clear();
		}
	}

	private async Task ShowUploadDialogAsync()
	{
		if (UploadDialog != null)
		{
			await UploadDialog.ShowAsync().ConfigureAwait(true);
		}
	}

	private async Task OnException(Exception exception) => await ExceptionHandler.InvokeAsync(exception).ConfigureAwait(true);

	private bool IsValidSelection()
	{
		foreach (var path in Table!.Selection)
		{
			// disallow delete if uploading
			var item = Table.ItemsToDisplay.Single(x => x.Path == path);
			if (item?.IsUploading != false)
			{
				return false;
			}
		}

		return true;
	}

	private async Task NavigateFolderAsync(string path)
	{
		if (_selectedNode?.IsExpanded == false)
		{
			await Tree!.ToggleNodeIsExpandedAsync(_selectedNode).ConfigureAwait(true);
		}

		var node = Tree!.RootNode.Find(path);
		if (node != null)
		{
			await Tree!.SelectNode(node).ConfigureAwait(true);
		}
	}

	private async Task NavigateUpAsync()
	{
		var parentPath = _selectedNode?.Data?.ParentPath;
		if (parentPath != null)
		{
			await NavigateFolderAsync(parentPath).ConfigureAwait(true);
		}
	}

	private async Task CreateNewFolderAsync(bool createInTree = true)
	{
		if (Tree?.SelectedNode?.Data is not { } folder)
		{
			return;
		}

		// determine default name
		var newFolderName = createInTree
			? await GetUniqueTreeFolderNameAsync(Tree.SelectedNode).ConfigureAwait(true)
			: GetUniqueTableFolderName();

		// create new folder via api
		var newItem = new FileExplorerItem
		{
			EntryType = FileExplorerItemType.Directory,
			Name = newFolderName,
			Path = $"{folder.Path.TrimEnd('/')}/{newFolderName}",
			HasSubFolders = false
		};
		var result = await DataProvider.CreateAsync(newItem, CancellationToken.None).ConfigureAwait(true);
		if (!result.Success)
		{
			return;
		}

		// refresh current node, select new node and finally begin edit mode
		await Tree.RefreshNodeAsync(Tree.SelectedNode).ConfigureAwait(true);
		if (createInTree)
		{
			await EditNewTreeFolderAsync(newItem.Path).ConfigureAwait(true);
		}
		else
		{
			await EditNewTableFolderAsync(newFolderName).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Returns a name for a new folder that no sub-folder of the given node has.
	/// </summary>
	private async Task<string> GetUniqueTreeFolderNameAsync(TreeNode<FileExplorerItem> node)
	{
		// current logic uses tree node sub-items to create a new folder with a unique name
		// ensure that the node is expanded so all sub-items are fetched.
		if (!node.IsExpanded)
		{
			await Tree!.ToggleNodeIsExpandedAsync(node).ConfigureAwait(true);
		}

		return node.MakeUniqueText(NewFolderName) ?? NewFolderName;
	}

	/// <summary>
	/// Returns a name for a new folder that no item in the table has.
	/// </summary>
	private string GetUniqueTableFolderName()
	{
		var newFolderName = NewFolderName;
		var count = 2;
		while (Table!.ItemsToDisplay.Any(x => string.Equals(x.Name, newFolderName, StringComparison.OrdinalIgnoreCase)))
		{
			newFolderName = $"{NewFolderName} ({count++})";
		}

		return newFolderName;
	}

	private async Task EditNewTreeFolderAsync(string path)
	{
		var newNode = Tree!.RootNode.Find(path);
		if (newNode != null)
		{
			await Tree.SelectNode(newNode).ConfigureAwait(true);
			await Tree.BeginEdit().ConfigureAwait(true);
		}
	}

	private async Task EditNewTableFolderAsync(string name)
	{
		// refresh table, select new folder row and begin edit
		await Table!.RefreshAsync().ConfigureAwait(true);
		var row = Table.ItemsToDisplay.FirstOrDefault(x => x.Name == name);
		if (row != null && Table.KeyField!(row)?.ToString() is string key)
		{
			await Table.SelectItemAsync(key).ConfigureAwait(true);
			await Table.BeginEditAsync().ConfigureAwait(true);
		}
	}

	private async Task DeleteFolderAsync()
	{
		if (_selectedNode?.Data is not { } folder || DeleteDialog == null)
		{
			return;
		}

		var deleteArgs = await RequestDeleteAsync([folder], items => $"Are you sure you wish to delete '{items[0].Name}'?").ConfigureAwait(true);
		if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Delete && deleteArgs.Items.Length > 0)
		{
			await DeleteTreeFolderAsync(deleteArgs.Items[0]).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Deletes a folder, removing the selected tree node once it is gone.
	/// </summary>
	private async Task DeleteTreeFolderAsync(FileExplorerItem folder)
	{
		try
		{
			var result = await DataProvider.DeleteAsync(folder, CancellationToken.None).ConfigureAwait(true);
			if (result.Success && Tree?.SelectedNode != null)
			{
				await Tree.RemoveNodeAsync(Tree.SelectedNode).ConfigureAwait(true);
			}
		}
		catch (Exception ex)
		{
			await OnException(ex).ConfigureAwait(true);
		}
	}

	private async Task DeleteFilesAsync()
	{
		if (Table?.Selection == null || DeleteDialog == null)
		{
			return;
		}

		var deleteArgs = await RequestDeleteAsync(Table.GetSelectedItems(), items => items.Length == 1
			? $"Are you sure you wish to delete '{items[0].Name}'?"
			: $"Are you sure you wish to delete these {items.Length} items?").ConfigureAwait(true);
		if (deleteArgs.Resolution != DeleteArgs.DeleteResolutions.Delete)
		{
			return;
		}

		foreach (var item in deleteArgs.Items)
		{
			try
			{
				await DataProvider.DeleteAsync(item, CancellationToken.None).ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				await OnException(ex).ConfigureAwait(true);
			}
		}

		// refresh tree, table and toolbar
		await RefreshTreeAsync().ConfigureAwait(true);
		await RefreshTableAsync().ConfigureAwait(true);
		await RefreshToolbarAsync().ConfigureAwait(true);
	}

	/// <summary>
	/// Lets the application decide on deleting the given items, and asks the user when it leaves that to them.
	/// </summary>
	/// <param name="items">The items to delete.</param>
	/// <param name="getPrompt">Gives the question to ask the user about the items left to delete.</param>
	/// <returns>The delete arguments, resolved to delete or cancel unless the application chose otherwise.</returns>
	private async Task<DeleteArgs> RequestDeleteAsync(FileExplorerItem[] items, Func<FileExplorerItem[], string> getPrompt)
	{
		var deleteArgs = new DeleteArgs
		{
			Items = items,
			Resolution = DeleteArgs.DeleteResolutions.Prompt
		};

		await DeleteRequest.InvokeAsync(deleteArgs).ConfigureAwait(true);

		if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Prompt)
		{
			_deleteDialogMessage = getPrompt(deleteArgs.Items);
			StateHasChanged();
			var choice = await DeleteDialog!.ShowAndWaitResultAsync().ConfigureAwait(true);
			deleteArgs.Resolution = choice == "yes" ? DeleteArgs.DeleteResolutions.Delete : DeleteArgs.DeleteResolutions.Cancel;
		}

		return deleteArgs;
	}

	private async Task MoveCopyFilesAsync(List<FileExplorerItem> payload, string targetPath, bool isCopy)
	{
		// store source folders being moved
		var pathsToRefresh = payload
			.Where(x => x.EntryType == FileExplorerItemType.Directory && !isCopy)
			.Select(x => x.ParentPath)
			.Distinct()
			.ToList();

		if (payload.Any(x => x.EntryType == FileExplorerItemType.Directory) && !pathsToRefresh.Contains(targetPath))
		{
			pathsToRefresh.Add(targetPath);
		}

		// check for conflicts - top level only
		var conflictArgs = new MoveCopyArgs
		{
			Payload = payload,
			TargetPath = targetPath,
			IsCopy = isCopy,
			ConflictResolution = ConflictResolution
		};
		await GetMoveCopyConflictsAsync(conflictArgs).ConfigureAwait(true);

		if (conflictArgs.Conflicts.Count > 0)
		{
			await ResolveMoveCopyConflictsAsync(conflictArgs, payload, targetPath).ConfigureAwait(true);
		}

		if (conflictArgs.Conflicts.Count > 0 && conflictArgs.ConflictResolution == ConflictResolutions.Cancel)
		{
			return;
		}

		// allow app to perform custom move / copy, else perform default move / copy behaviour
		if (!await IsMoveCopyDoneByApplicationAsync(conflictArgs, targetPath, isCopy).ConfigureAwait(true))
		{
			foreach (var source in conflictArgs.Payload.ToArray())
			{
				await MoveCopyItemAsync(source, conflictArgs).ConfigureAwait(true);
			}
		}

		await ShowMoveCopyResultAsync(payload, targetPath, isCopy, pathsToRefresh).ConfigureAwait(true);
	}

	/// <summary>
	/// Lets the application process move / copy conflicts, asking the user how to resolve them when it leaves that to them.
	/// </summary>
	private async Task ResolveMoveCopyConflictsAsync(MoveCopyArgs conflictArgs, List<FileExplorerItem> payload, string targetPath)
	{
		// allow application to process conflicts
		await MoveCopyConflict.InvokeAsync(conflictArgs).ConfigureAwait(true);
		if (conflictArgs.ConflictResolution != ConflictResolutions.Prompt)
		{
			return;
		}

		// if any source and target path are the same then user is copy/moving from same folder - so hide overwrite option
		var showOverwrite = !conflictArgs.Payload.Any(x => conflictArgs.Conflicts.Any(y => y.Path == x.Path));

		// check if target folder is source folder?
		conflictArgs.ConflictResolution = payload.Any(x => x.ParentPath == targetPath)
			? await PromptUserForConflictResolution([], false, AllowRenameConflicts, "The source and destination filenames are the same.").ConfigureAwait(true)
			: await PromptUserForConflictResolution([.. conflictArgs.Conflicts.Select(x => FileExplorerItem.GetNameFromPath(x.Path))], showOverwrite).ConfigureAwait(true);
	}

	/// <summary>
	/// Lets the application perform the move / copy itself, returning whether it did so instead of the default.
	/// </summary>
	private async Task<bool> IsMoveCopyDoneByApplicationAsync(MoveCopyArgs conflictArgs, string targetPath, bool isCopy)
	{
		if (!CustomMoveCopy.HasDelegate)
		{
			return false;
		}

		var customArgs = new CustomMoveCopyArgs
		{
			ConflictResolution = conflictArgs.ConflictResolution,
			Payload = conflictArgs.Payload,
			Conflicts = conflictArgs.Conflicts,
			IsCopy = isCopy,
			TargetPath = targetPath
		};
		await CustomMoveCopy.InvokeAsync(customArgs).ConfigureAwait(true);
		return customArgs.CancelDefault;
	}

	/// <summary>
	/// Moves or copies one item into the target folder, resolving any conflict as chosen.
	/// </summary>
	private async Task MoveCopyItemAsync(FileExplorerItem source, MoveCopyArgs conflictArgs)
	{
		if (conflictArgs.ConflictResolution == ConflictResolutions.Rename)
		{
			// get a unique name
			var uniquePath = $"{conflictArgs.TargetPath.TrimEnd('/')}/{GetUniqueName(source, conflictArgs.TargetItems)}";
			await DataProvider.UpdateAsync(source, GetMoveCopyDelta(uniquePath, conflictArgs.IsCopy), CancellationToken.None).ConfigureAwait(true);
			return;
		}

		var newPath = $"{conflictArgs.TargetPath}/{source.Name}";
		var overwrite = conflictArgs.ConflictResolution == ConflictResolutions.Overwrite;
		var isConflict = conflictArgs.Conflicts.Any(x => x.Name == source.Name);

		// delete conflicting target file?
		if (overwrite && isConflict)
		{
			// check source and destination are not same file
			if (newPath == source.Path)
			{
				await ExceptionHandler.InvokeAsync(new InvalidOperationException("Operation Failed: Source and Destination are the same")).ConfigureAwait(true);
				return;
			}

			var target = new FileExplorerItem { EntryType = source.EntryType, Path = newPath };
			await DataProvider.DeleteAsync(target, CancellationToken.None).ConfigureAwait(true);
		}

		// move or copy entry if no conflict or overwrite chosen
		if (overwrite || !isConflict)
		{
			await DataProvider.UpdateAsync(source, GetMoveCopyDelta(newPath, conflictArgs.IsCopy), CancellationToken.None).ConfigureAwait(true);
		}
	}

	private static Dictionary<string, object?> GetMoveCopyDelta(string newPath, bool isCopy) => new()
	{
		{ "Path", newPath },
		{ "Copy", isCopy }
	};

	/// <summary>
	/// Shows the outcome of a move / copy: the table is refreshed, or the target folder selected, and the affected
	/// tree nodes refreshed.
	/// </summary>
	private async Task ShowMoveCopyResultAsync(List<FileExplorerItem> payload, string targetPath, bool isCopy, List<string> pathsToRefresh)
	{
		await RefreshTableOrSelectTargetAsync(payload, targetPath, isCopy).ConfigureAwait(true);

		// refresh affected tree nodes
		foreach (var path in pathsToRefresh)
		{
			var node = Tree!.Search((x) => x?.Data?.Path == path);
			if (node != null)
			{
				await Tree.RefreshNodeAsync(node).ConfigureAwait(true);
			}
		}
	}

	private async Task RefreshTableOrSelectTargetAsync(List<FileExplorerItem> payload, string targetPath, bool isCopy)
	{
		// RM-12291 - API: moving a folder between a Sharepoint filesystem and a ReportMagic filesystem(when target folder is expanded) shows an API error(but succeeds)
		// determine whether to refresh the table or select the target node
		var selectedItem = Tree?.SelectedNode?.Data;
		if (isCopy || (selectedItem != null && !payload.Contains(selectedItem)))
		{
			await Table!.RefreshAsync().ConfigureAwait(true);
		}
		else
		{
			// switch to target path
			await SelectTreeFolderAsync(targetPath).ConfigureAwait(true);
		}
	}

	private async Task SelectTreeFolderAsync(string path)
	{
		var node = Tree?.Search(x => x.Data?.Path == path);
		if (Tree != null && node != null)
		{
			await Tree.SelectNode(node).ConfigureAwait(true);
		}
	}

	private static string GetUniqueName(FileExplorerItem source, List<FileExplorerItem> targetItems)
	{
		var count = 1;
		var newName = PostFixFilename(FileExplorerItem.GetNameFromPath(source.Path), " Copy");

		while (targetItems.Any(x => FileExplorerItem.GetNameFromPath(x.Path) == newName))
		{
			newName = PostFixFilename(FileExplorerItem.GetNameFromPath(source.Path), $" Copy {count++}");
		}

		return newName;
	}

	private static string PostFixFilename(string filename, string postfix)
	{
		if (string.IsNullOrWhiteSpace(filename))
		{
			return postfix.Trim();
		}

		var idx = filename.LastIndexOf('.');
		if (idx == -1)
		{
			return $"{filename}{postfix}";
		}

		return $"{filename[..idx]}{postfix}{filename[idx..]}";
	}

	/// <summary>
	/// Populates the move copy arguments with conflicting items.
	/// </summary>
	private async Task GetMoveCopyConflictsAsync(MoveCopyArgs args)
	{
		var conflicts = new List<FileExplorerItem>();
		var names = args.Payload.Select(x => FileExplorerItem.GetNameFromPath(x.Path)).ToArray();
		var request = new DataRequest<FileExplorerItem> { SearchText = args.TargetPath };
		var response = await DataProvider.GetDataAsync(request, CancellationToken.None).ConfigureAwait(true);
		args.TargetItems = [.. response.Items];

		foreach (var item in response.Items)
		{
			if (names.Any(x => string.Equals(item.Name, x, StringComparison.OrdinalIgnoreCase)))
			{
				conflicts.Add(item);
			}
		}

		args.Conflicts = conflicts;
	}

	private async Task<ConflictResolutions> PromptUserForConflictResolution(IEnumerable<string> names, bool showOverwrite, bool showRename = true, string? message = null)
	{
		var namesSummary = names.Take(5).ToList();
		if (names.Count() > 5)
		{
			namesSummary.Add($"+ {names.Count() - 5} other items");
		}

		_conflictDialogMessage = message ?? $"{names.Count()} conflicts found : -";
		_conflictDialogList = [.. namesSummary];
		var buttons = ConflictDialog!.Buttons;
		buttons.First(x => x.Key == "Overwrite").IsVisible = showOverwrite;
		buttons.First(x => x.Key == "Rename").IsVisible = showRename;
		// shift first button right
		foreach (var btn in buttons)
		{
			btn.ShiftRight = buttons.FirstOrDefault(x => x.IsVisible) == btn;
		}

		StateHasChanged();

		if (ConflictDialog != null)
		{
			var result = await ConflictDialog.ShowAndWaitResultAsync().ConfigureAwait(true);
			return result switch
			{
				"Skip" => ConflictResolutions.Skip,
				"Cancel" => ConflictResolutions.Cancel,
				"Overwrite" => ConflictResolutions.Overwrite,
				"Rename" => ConflictResolutions.Rename,
				_ => ConflictResolutions.Skip
			};
		}

		return ConflictResolutions.Skip;
	}
}
