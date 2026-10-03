namespace PanoramicData.Blazor;

/// <summary>
/// Handling of the file table of a <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorer
{
	private void OnTableItemsLoaded(List<FileExplorerItem> items)
	{
		// insert special .. folder?
		if (_selectedNode != null && ShowParentFolder && _selectedNode.ParentNode?.Data?.Path != null && _selectedNode.Data?.Path != "/")
		{
			items.Insert(0, new FileExplorerItem
			{
				Name = "..",
				Path = $"{_selectedNode.ParentNode.Data.Path}",
				EntryType = FileExplorerItemType.Directory,
				CanCopyMove = false,
				IsReadOnly = true
			});
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
		foreach (var item in items.Where(x => x.EntryType == FileExplorerItemType.File).ToArray())
		{
			if (ShowFiles)
			{
				if (!item.IsNameMatch(FilenamePattern))
				{
					items.Remove(item);
				}
			}
			else
			{
				items.Remove(item);
			}
		}
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
		if (Table?.IsEditing != true)
		{
			if (args.Code == "Delete")
			{
				await DeleteFilesAsync().ConfigureAwait(true);
			}
			else if ((args.Code == "KeyC" || args.Code == "KeyX") && args.CtrlKey)
			{
				_copyPayload.Clear();
				_copyPayload.AddRange(Table!.GetSelectedItems());
				_moveCopyPayload = args.Code == "KeyX";
			}
			else if (args.Code == "KeyV" && args.CtrlKey)
			{
				var selection = Table!.GetSelectedItems();
				var targetPath = selection.Length == 1 && selection[0].EntryType == FileExplorerItemType.Directory ? selection[0].Path : FolderPath;
				await MoveCopyFilesAsync(_copyPayload, targetPath, !_moveCopyPayload).ConfigureAwait(true);
				if (_moveCopyPayload) // clear copy payload only if move
				{
					_copyPayload.Clear();
				}
			}
		}
	}

	private async Task OnTableAfterEditAsync(TableAfterEditEventArgs<FileExplorerItem> args)
	{
		// cancel if new name is empty
		if (args.NewValues.TryGetValue("Name", out object? value))
		{
			var newName = value?.ToString();
			if (newName == args.Item.Name)
			{
				args.Cancel = true;
			}
			else if (string.IsNullOrWhiteSpace(newName))
			{
				args.Cancel = true;
				await ExceptionHandler.InvokeAsync(new PDFileExplorerException("A value is required")).ConfigureAwait(true);
			}
			else if (newName.StartsWith('.'))
			{
				args.Cancel = true;
				await ExceptionHandler.InvokeAsync(new PDFileExplorerException("Names may not begin with a period (.)")).ConfigureAwait(true);
			}
			else
			{
				var previousPath = args.Item.Path;
				var newPath = $"{args.Item.ParentPath}/{newName}";
				if (newPath.StartsWith("//", StringComparison.Ordinal))
				{
					newPath = newPath[1..];
				}

				// check for duplicate name, ignoring case for renaming same file changing case
				var justChangingCase = string.Equals(args.Item.Name, newName, StringComparison.OrdinalIgnoreCase);
				var hasSameFullPath = Table!.ItemsToDisplay.Any(x => x.Path == newPath);
				var hasExistingNameIgnoringCase = Table!.ItemsToDisplay.Any(x => string.Equals(x.Name, newName, StringComparison.OrdinalIgnoreCase));
				if (!justChangingCase && (hasSameFullPath || hasExistingNameIgnoringCase))
				{
					args.Cancel = true;
					await OnException(new PDFileExplorerException($"An item named '{newName}' already exists")).ConfigureAwait(true);
				}
				else
				{
					// inform data provider
					var delta = new Dictionary<string, object?>
					{
						{  "Path", newPath }
					};
					var result = await DataProvider.UpdateAsync(args.Item, delta, CancellationToken.None).ConfigureAwait(true);
					if (result.Success)
					{
						// if folder renamed then update nodes
						if (args.Item.EntryType == FileExplorerItemType.Directory)
						{
							await DirectoryRenameAsync(previousPath, newPath).ConfigureAwait(true);
						}
						else
						{
							args.Item.Name = newName!;
							args.Item.Path = newPath;
						}
					}
					else
					{
						args.Cancel = true;
					}

					// replace selection with new path
					Table.Selection.Clear();
					Table.Selection.Add(newPath);
				}
			}
		}
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
		var selectedFolder = _selectedNode?.Data;

		// determine whether paste is allowed?
		var canPaste = validSelection && _copyPayload.Count > 0;

		// if still okay to paste then determine target
		if (canPaste)
		{
			// did user right click in selected row?
			if (args.SourceElement?.HasAncestor("TR", "selected") == true ||
				 (args.SourceElement?.Find("TR") is ElementInfo trEl && Table.Selection.Contains(trEl.Id)))
			{
				// can only paste if selected item is a folder
				if (selectedItems![0].EntryType == FileExplorerItemType.Directory)
				{
					_pasteTarget = selectedItems![0].Path;
				}
				else
				{
					canPaste = false;
				}
			}
			else
			{
				// if user right clicked in whitespace then use current folder
				if (args.SourceElement?.Tag == "TD" || args.SourceElement?.Tag == "DIV")
				{
					_pasteTarget = FolderPath;
				}
				else
				{
					// find the row clicked on by using id
					if (args.SourceElement?.Find("TR") is ElementInfo parentTrElement
						&& Table.ItemsToDisplay.Find(x => x.Path == parentTrElement.Id) is FileExplorerItem row
						&& row.EntryType == FileExplorerItemType.Directory)
					{
						_pasteTarget = row.Path;
					}
					else
					{
						// use current folder
						_pasteTarget = FolderPath;
					}
				}
			}
		}
		else
		{
			_pasteTarget = string.Empty;
		}

		_menuOpen.IsVisible = selectedItems?.Length == 1 && selectedItems[0].EntryType == FileExplorerItemType.Directory;
		_menuDownload.IsVisible = validSelection && selectedItems?.Length > 0 && selectedItems.All(x => x.EntryType == FileExplorerItemType.File);
		_menuNewFolder.IsVisible = selectedItems?.Length == 0 && selectedFolder?.CanAddItems == true;
		_menuUploadFiles.IsVisible = selectedItems?.Length == 0 && selectedFolder?.CanAddItems == true;
		_menuRename.IsVisible = validSelection && selectedItems?.Length == 1 && selectedItems[0].CanRename && !IsParentDirectoryItem(selectedItems[0]);
		_menuDelete.IsVisible = validSelection && selectedItems?.Length > 0 && selectedItems.All(x => x.CanDelete) && !selectedItems.Any(x => IsParentDirectoryItem(x));
		_menuCopy.IsVisible = validSelection && selectedItems?.Length > 0 && selectedItems.All(x => x.CanCopyMove) && !selectedItems.Any(x => IsParentDirectoryItem(x));
		_menuCut.IsVisible = validSelection && selectedItems?.Length > 0 && selectedItems.All(x => x.CanDelete) && !selectedItems.Any(x => IsParentDirectoryItem(x));
		_menuPaste.IsVisible = canPaste;
		_menuSep3.IsVisible = _menuSep2.IsVisible = _menuSep1.IsVisible = true;
		_menuSep3.IsVisible = ShowSeparator(_menuSep3, TableContextItems);
		_menuSep2.IsVisible = ShowSeparator(_menuSep2, TableContextItems);
		_menuSep1.IsVisible = ShowSeparator(_menuSep1, TableContextItems);

		// allow application to alter table context menu state
		args.Context = selectedItems;
		await UpdateTableContextState.InvokeAsync(args).ConfigureAwait(true);
	}

	private async Task OnTableContextMenuItemClickAsync(MenuItem menuItem)
	{
		if (Table is null)
		{
			throw new InvalidOperationException("_table is null");
		}

		// notify application and allow cancel
		var args = new MenuItemEventArgs(Table, menuItem);
		await TableContextMenuClick.InvokeAsync(args).ConfigureAwait(true);
		var selection = Table!.GetSelectedItems();

		if (!args.Cancel)
		{
			if (menuItem.Text == "Open" && selection.Length == 1)
			{
				await NavigateFolderAsync(selection[0].Path).ConfigureAwait(true);
			}
			else if (menuItem.Text == "Delete")
			{
				await DeleteFilesAsync().ConfigureAwait(true);
			}
			else if (menuItem.Text == "Rename")
			{
				await Table!.BeginEditAsync().ConfigureAwait(true);
			}
			else if (menuItem.Text == "Download")
			{
				var downloadArgs = new TableSelectionEventArgs<FileExplorerItem>(Table.GetSelectedItems());
				await TableDownloadRequest.InvokeAsync(downloadArgs).ConfigureAwait(true);
			}
			else if (menuItem.Text == "Copy" || menuItem.Text == "Cut")
			{
				_copyPayload.Clear();
				_copyPayload.AddRange(Table!.GetSelectedItems());
				_moveCopyPayload = menuItem.Text == "Cut";
			}
			else if (menuItem.Text == "Paste")
			{
				await MoveCopyFilesAsync(_copyPayload, _pasteTarget, !_moveCopyPayload).ConfigureAwait(true);
				if (_moveCopyPayload) // clear copy payload only if move
				{
					_copyPayload.Clear();
				}
			}
			else if (menuItem.Text == "New Folder")
			{
				await CreateNewFolderAsync(false).ConfigureAwait(true);
			}
			else if (menuItem.Text == "Upload Files" && UploadDialog != null)
			{
				await UploadDialog.ShowAsync().ConfigureAwait(true);
			}
		}
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
