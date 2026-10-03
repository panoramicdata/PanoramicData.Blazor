namespace PanoramicData.Blazor;

/// <summary>
/// Handling of the file table of a <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorer
{
	private string _pasteTarget = string.Empty;

	private void OnTableItemsLoaded(List<FileExplorerItem> items)
	{
		// insert special .. folder?
		if (GetParentFolderItem() is { } parentFolder)
		{
			items.Insert(0, parentFolder);
		}

		// remove any paths to exclude
		items.RemoveAll(x => ExcludedPaths.Contains(x.Path));

		// arrange folders together?
		if (GroupFolders)
		{
			var folders = items.Where(x => x.EntryType == FileExplorerItemType.Directory).ToList();
			var files = items.Where(x => x.EntryType == FileExplorerItemType.File).ToList();
			items.Clear();
			items.AddRange(folders);
			items.AddRange(files);
		}

		// filter files displayed
		items.RemoveAll(x => x.EntryType == FileExplorerItemType.File && (!ShowFiles || !x.IsNameMatch(FilenamePattern)));
	}

	/// <summary>
	/// Returns the special ".." item leading up from the selected folder, when it is wanted and there is a parent.
	/// </summary>
	private FileExplorerItem? GetParentFolderItem()
	{
		var parentPath = _selectedNode?.ParentNode?.Data?.Path;
		if (!ShowParentFolder || parentPath is null || _selectedNode!.Data?.Path == "/")
		{
			return null;
		}

		return new FileExplorerItem
		{
			Name = "..",
			Path = parentPath,
			EntryType = FileExplorerItemType.Directory,
			CanCopyMove = false,
			IsReadOnly = true
		};
	}

	private async Task OnTableDoubleClickAsync(FileExplorerItem item)
	{
		if (!Table!.IsEditing)
		{
			if (item.EntryType == FileExplorerItemType.Directory)
			{
				await NavigateFolderAsync(item.Path).ConfigureAwait(true);
			}
			else
			{
				await ItemDoubleClick.InvokeAsync(item).ConfigureAwait(true);
			}
		}
	}

	private async Task OnTableBeforeEdit(TableBeforeEditEventArgs<FileExplorerItem> args)
	{
		if (args.Item != null)
		{
			var renameArgs = new RenameArgs { Item = args.Item };
			await BeforeRename.InvokeAsync(renameArgs).ConfigureAwait(true);
			if (renameArgs.Cancel || !AllowRename || args.Item.Name == ".." || args.Item.IsUploading || args.Item.IsReadOnly || !args.Item.CanRename)
			{
				args.Cancel = true;
			}
			else
			{
				// only want to select the filename portion of the text
				args.SelectionEnd = Path.GetFileNameWithoutExtension(args.Item.Name).Length;
			}
		}
	}

	private async Task OnTableKeyDownAsync(KeyboardEventArgs args)
	{
		if (Table?.IsEditing == true)
		{
			return;
		}

		await ((args.Code, args.CtrlKey) switch
		{
			("Delete", _) => DeleteFilesAsync(),
			("KeyC" or "KeyX", true) => SetCopyPayload(Table!.GetSelectedItems(), args.Code == "KeyX"),
			("KeyV", true) => PasteAsync(GetSelectedFolderOrCurrentPath()),
			_ => Task.CompletedTask
		}).ConfigureAwait(true);
	}

	/// <summary>
	/// The path of the single selected folder, or else of the current folder.
	/// </summary>
	private string GetSelectedFolderOrCurrentPath()
	{
		var selection = Table!.GetSelectedItems();
		return selection.Length == 1 && selection[0].EntryType == FileExplorerItemType.Directory ? selection[0].Path : FolderPath;
	}

	private async Task OnTableAfterEditAsync(TableAfterEditEventArgs<FileExplorerItem> args)
	{
		if (!args.NewValues.TryGetValue("Name", out object? value))
		{
			return;
		}

		var newName = value?.ToString();
		if (newName == args.Item.Name)
		{
			args.Cancel = true;
			return;
		}

		// cancel if new name is empty or hidden
		if (string.IsNullOrWhiteSpace(newName) || newName.StartsWith('.'))
		{
			args.Cancel = true;
			var message = string.IsNullOrWhiteSpace(newName) ? "A value is required" : "Names may not begin with a period (.)";
			await ExceptionHandler.InvokeAsync(new PDFileExplorerException(message)).ConfigureAwait(true);
			return;
		}

		await RenameTableItemAsync(args, newName).ConfigureAwait(true);
	}

	private async Task RenameTableItemAsync(TableAfterEditEventArgs<FileExplorerItem> args, string newName)
	{
		var previousPath = args.Item.Path;
		var newPath = $"{args.Item.ParentPath}/{newName}";
		if (newPath.StartsWith("//", StringComparison.Ordinal))
		{
			newPath = newPath[1..];
		}

		if (IsNameTaken(args.Item, newName, newPath))
		{
			args.Cancel = true;
			await OnException(new PDFileExplorerException($"An item named '{newName}' already exists")).ConfigureAwait(true);
			return;
		}

		// inform data provider
		var delta = new Dictionary<string, object?>
		{
			{  "Path", newPath }
		};
		var result = await DataProvider.UpdateAsync(args.Item, delta, CancellationToken.None).ConfigureAwait(true);
		if (!result.Success)
		{
			args.Cancel = true;
		}
		else if (args.Item.EntryType == FileExplorerItemType.Directory)
		{
			// if folder renamed then update nodes
			await DirectoryRenameAsync(previousPath, newPath).ConfigureAwait(true);
		}
		else
		{
			args.Item.Name = newName;
			args.Item.Path = newPath;
		}

		// replace selection with new path
		Table!.Selection.Clear();
		Table.Selection.Add(newPath);
	}

	/// <summary>
	/// Whether another item already has the new name or path, ignoring case for renaming same file changing case.
	/// </summary>
	private bool IsNameTaken(FileExplorerItem item, string newName, string newPath)
		=> !string.Equals(item.Name, newName, StringComparison.OrdinalIgnoreCase)
			&& Table!.ItemsToDisplay.Any(x => x.Path == newPath || string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Shows each separator of a context menu only where it separates visible items.
	/// </summary>
	private void UpdateSeparators(IEnumerable<MenuItem> items)
	{
		_menuSep3.IsVisible = _menuSep2.IsVisible = _menuSep1.IsVisible = true;
		_menuSep3.IsVisible = ShowSeparator(_menuSep3, items);
		_menuSep2.IsVisible = ShowSeparator(_menuSep2, items);
		_menuSep1.IsVisible = ShowSeparator(_menuSep1, items);
	}

	private static bool ShowSeparator(MenuItem separator, IEnumerable<MenuItem> items)
	{
		var visibleItems = items.Where(x => x.IsVisible).ToList();
		if (visibleItems.Count == 0 || separator == visibleItems[0] || separator == visibleItems[^1])
		{
			return false;
		}

		var idx = visibleItems.IndexOf(separator);
		return idx <= 0 || !visibleItems[idx - 1].IsSeparator;
	}

	private async Task OnTableContextMenuUpdateStateAsync(MenuItemsEventArgs args)
	{
		var selectedItems = Table!.GetSelectedItems() ?? [];
		var validSelection = IsValidSelection();

		UpdateTableFolderMenuItems(selectedItems);
		UpdateTableSelectionMenuItems(selectedItems, validSelection);
		_menuPaste.IsVisible = UpdatePasteTarget(args.SourceElement, selectedItems, validSelection);
		UpdateSeparators(TableContextItems);

		// allow application to alter table context menu state
		args.Context = selectedItems;
		await UpdateTableContextState.InvokeAsync(args).ConfigureAwait(true);
	}

	private void UpdateTableFolderMenuItems(FileExplorerItem[] selectedItems)
	{
		var canAddToFolder = selectedItems.Length == 0 && _selectedNode?.Data?.CanAddItems == true;
		_menuOpen.IsVisible = selectedItems.Length == 1 && selectedItems[0].EntryType == FileExplorerItemType.Directory;
		_menuNewFolder.IsVisible = canAddToFolder;
		_menuUploadFiles.IsVisible = canAddToFolder;
	}

	private void UpdateTableSelectionMenuItems(FileExplorerItem[] selectedItems, bool validSelection)
	{
		var hasSelection = validSelection && selectedItems.Length > 0;
		var isChangeable = hasSelection && !selectedItems.Any(IsParentDirectoryItem);
		_menuDownload.IsVisible = hasSelection && selectedItems.All(x => x.EntryType == FileExplorerItemType.File);
		_menuRename.IsVisible = isChangeable && selectedItems.Length == 1 && selectedItems[0].CanRename;
		_menuDelete.IsVisible = isChangeable && selectedItems.All(x => x.CanDelete);
		_menuCopy.IsVisible = isChangeable && selectedItems.All(x => x.CanCopyMove);
		_menuCut.IsVisible = _menuDelete.IsVisible;
	}

	/// <summary>
	/// Determines whether the payload can be pasted, and where to, recording the target for the Paste menu item.
	/// </summary>
	/// <returns>True when the payload can be pasted.</returns>
	private bool UpdatePasteTarget(ElementInfo? sourceElement, FileExplorerItem[] selectedItems, bool validSelection)
	{
		if (!validSelection || _copyPayload.Count == 0)
		{
			_pasteTarget = string.Empty;
			return false;
		}

		if (GetPasteTarget(sourceElement, selectedItems) is not { } pasteTarget)
		{
			return false;
		}

		_pasteTarget = pasteTarget;
		return true;
	}

	/// <summary>
	/// Returns the folder a paste from the context menu goes into, or null when the menu was opened on a file.
	/// </summary>
	private string? GetPasteTarget(ElementInfo? sourceElement, FileExplorerItem[] selectedItems)
	{
		// did user right click in selected row? - can only paste if selected item is a folder
		if (IsInSelectedRow(sourceElement))
		{
			return selectedItems[0].EntryType == FileExplorerItemType.Directory ? selectedItems[0].Path : null;
		}

		// if user right clicked in whitespace then use current folder
		if (sourceElement?.Tag is "TD" or "DIV")
		{
			return FolderPath;
		}

		// find the row clicked on by using id, else use current folder
		return sourceElement?.Find("TR") is ElementInfo parentTrElement
			&& Table!.ItemsToDisplay.Find(x => x.Path == parentTrElement.Id) is { EntryType: FileExplorerItemType.Directory } row
				? row.Path
				: FolderPath;
	}

	private bool IsInSelectedRow(ElementInfo? sourceElement)
		=> sourceElement?.HasAncestor("TR", "selected") == true
			|| (sourceElement?.Find("TR") is ElementInfo trEl && Table!.Selection.Contains(trEl.Id));

	private async Task OnTableContextMenuItemClickAsync(MenuItem menuItem)
	{
		if (Table is null)
		{
			throw new InvalidOperationException("_table is null");
		}

		// notify application and allow cancel
		var args = new MenuItemEventArgs(Table, menuItem);
		await TableContextMenuClick.InvokeAsync(args).ConfigureAwait(true);
		var selection = Table.GetSelectedItems();

		if (args.Cancel)
		{
			return;
		}

		await (menuItem.Text switch
		{
			"Open" when selection.Length == 1 => NavigateFolderAsync(selection[0].Path),
			"Delete" => DeleteFilesAsync(),
			"Rename" => Table.BeginEditAsync(),
			"Download" => TableDownloadRequest.InvokeAsync(new TableSelectionEventArgs<FileExplorerItem>(Table.GetSelectedItems())),
			"Copy" or "Cut" => SetCopyPayload(Table.GetSelectedItems(), menuItem.Text == "Cut"),
			"Paste" => PasteAsync(_pasteTarget),
			"New Folder" => CreateNewFolderAsync(false),
			"Upload Files" => ShowUploadDialogAsync(),
			_ => Task.CompletedTask
		}).ConfigureAwait(true);
	}

	private async Task OnTableSelectionChangedAsync()
	{
		await RefreshToolbarAsync().ConfigureAwait(true);
		var selection = Table?.GetSelectedItems() ?? [];

		await SelectionChanged.InvokeAsync(selection).ConfigureAwait(true);
		_previewItem = selection.Length == 1 ? selection[0] : null;
	}

	private async Task DirectoryRenameAsync(string oldPath, string newPath)
	{
		if (Tree != null && Table != null)
		{
			// synchronize existing node paths for tree and table
			Tree.RootNode.Walk((x) =>
			{
				if (x.Data != null)
				{
					if (x.Data.Path == oldPath)
					{
						x.Data.Name = FileExplorerItem.GetNameFromPath(newPath);
					}

					x.Key = x.Key.ReplacePathPrefix(oldPath, newPath);
					x.Data.Path = x.Data.Path.ReplacePathPrefix(oldPath, newPath);
				}

				return true;
			});

			Table.ItemsToDisplay.ToList().ForEach(x => x.Path = x.Path.ReplacePathPrefix(oldPath, newPath));
			
			// re-apply sort to tree and table
			if (Tree.SelectedNode != null)
			{
				await OnTreeSelectionChangeAsync(Tree.SelectedNode).ConfigureAwait(true);
				await Tree.RefreshNodeAsync(Tree.SelectedNode).ConfigureAwait(true);
			}

			await Table.SortAsync(Table.SortCriteria);
		}
	}
}
