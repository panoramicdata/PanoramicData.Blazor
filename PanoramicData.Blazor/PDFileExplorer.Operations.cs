namespace PanoramicData.Blazor;

/// <summary>
/// Toolbar, navigation and file operations of a <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorer
{
	private async Task OnTogglePreviewPanelAsync()
	{
		if (_splitter != null)
		{
			if (PreviewPanelVisible)
			{
				_lastSplitSizes = await _splitter.GetSizesAsync().ConfigureAwait(true);
				if (_lastSplitSizes.Length > 2)
				{
					await _splitter.SetSizesAsync([_lastSplitSizes[0], _lastSplitSizes[1] + _lastSplitSizes[2], 0]).ConfigureAwait(true);
				}
			}
			else
			{
				await _splitter.SetSizesAsync(_lastSplitSizes).ConfigureAwait(true);
			}

			PreviewPanelVisible = !PreviewPanelVisible;

			await RefreshToolbarAsync();
		}
	}

	private async Task OnToolbarButtonClickAsync(KeyedEventArgs<MouseEventArgs> args)
	{
		switch (args.Key)
		{
			case "navigate-up":
				await NavigateUpAsync().ConfigureAwait(true);
				break;

			case "open":
				var selectedFolderPath = Table?.Selection[0];
				if (selectedFolderPath != null)
				{
					await NavigateFolderAsync(selectedFolderPath).ConfigureAwait(true);
				}

				break;

			case "create-folder":
				await CreateNewFolderAsync(false).ConfigureAwait(true);
				break;

			case "delete":
				await DeleteFilesAsync().ConfigureAwait(true);
				break;

			case "upload":
				if (UploadDialog != null)
				{
					await UploadDialog.ShowAsync().ConfigureAwait(true);
				}

				break;

			case "refresh":
				await RefreshAllAsync().ConfigureAwait(true);
				break;

			case "preview":
				if (PreviewPanel == FilePreviewModes.OptionalOff || PreviewPanel == FilePreviewModes.OptionalOn)
				{
					await OnTogglePreviewPanelAsync().ConfigureAwait(true);
				}

				break;

			default:
				await ToolbarClick.InvokeAsync(args.Key).ConfigureAwait(true);
				break;
		}
	}

	private async Task OnDropAsync(DropEventArgs args)
	{
		// unwrap FileExplorerItem
		if (args.Target is null)
		{
			args.Target = _selectedNode?.Data;
		}
		else if (args.Target is TreeNode<FileExplorerItem> node)
		{
			args.Target = node.Data;
		}

		// source and target are file items - and target is folder?
		// and the folder accepts new items (the tree checks this before a drop, the table does not)?
		if (args.Target is FileExplorerItem target && target.EntryType == FileExplorerItemType.Directory && CanDropInto(target))
		{
			List<FileExplorerItem> payload = [];
			if (args.Payload is List<FileExplorerItem> mfe)
			{
				payload = mfe;
			}
			else if (args.Payload is FileExplorerItem sfe)
			{
				payload.Add(sfe);
			}

			// check not dropping an item onto itself (or sub folder)
			if (payload.Any(x => x.Path == target.Path || target.Path.StartsWith(x.Path, StringComparison.InvariantCultureIgnoreCase)))
			{
				return;
			}

			// check can move/copy all items
			if (payload.Any(x => !x.CanCopyMove))
			{
				return;
			}

			// move items into folder
			var targetPath = target.Path;
			await MoveCopyFilesAsync(payload, targetPath, args.Ctrl).ConfigureAwait(true);
		}
	}
	/// <summary>
	/// Whether a drop may add items to the given folder. The ".." row is always read-only itself, so it defers to
	/// the parent folder it stands for.
	/// </summary>
	private bool CanDropInto(FileExplorerItem folder)
		=> IsParentDirectoryItem(folder)
			? _selectedNode?.ParentNode?.Data?.CanAddItems != false
			: folder.CanAddItems;

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
		if (Tree?.SelectedNode?.Data != null)
		{
			// determine default name
			var newFolderName = NewFolderName;
			if (createInTree)
			{
				// current logic uses tree node sub-items to create a new folder with a unique name
				// ensure that the node is expanded so all sub-items are fetched.
				if (!Tree.SelectedNode.IsExpanded)
				{
					await Tree.ToggleNodeIsExpandedAsync(Tree.SelectedNode).ConfigureAwait(true);
				}

				newFolderName = Tree!.SelectedNode.MakeUniqueText(NewFolderName) ?? NewFolderName;
			}
			else
			{
				for (var count = 2; Table!.ItemsToDisplay.Any(x => string.Equals(x.Name, newFolderName, StringComparison.OrdinalIgnoreCase)); count++)
				{
					newFolderName = $"{NewFolderName} ({count})";
				}
			}

			// create new folder via api
			var newPath = $"{Tree.SelectedNode.Data.Path.TrimEnd('/')}/{newFolderName}";
			var newItem = new FileExplorerItem
			{
				EntryType = FileExplorerItemType.Directory,
				Name = newFolderName,
				Path = newPath,
				HasSubFolders = false
			};
			var result = await DataProvider.CreateAsync(newItem, CancellationToken.None).ConfigureAwait(true);
			if (result.Success)
			{
				// refresh current node, select new node and finally begin edit mode
				await Tree.RefreshNodeAsync(Tree.SelectedNode).ConfigureAwait(true);

				if (createInTree)
				{
					var newNode = Tree.RootNode.Find(newItem.Path);
					if (newNode != null)
					{
						await Tree.SelectNode(newNode).ConfigureAwait(true);
						await Tree.BeginEdit().ConfigureAwait(true);
					}
				}
				else
				{
					// refresh table, select new folder row and begin edit
					await Table!.RefreshAsync().ConfigureAwait(true);
					var row = Table!.ItemsToDisplay.FirstOrDefault(x => x.Name == newFolderName);
					if (row != null && Table!.KeyField!(row)?.ToString() is string key)
					{
						await Table!.SelectItemAsync(key).ConfigureAwait(true);
						await Table!.BeginEditAsync().ConfigureAwait(true);
					}
				}
			}
		}
	}

	private async Task DeleteFolderAsync()
	{
		if (_selectedNode?.Data != null && DeleteDialog != null)
		{
			var deleteArgs = new DeleteArgs
			{
				Items = [_selectedNode.Data],
				Resolution = DeleteArgs.DeleteResolutions.Prompt
			};

			await DeleteRequest.InvokeAsync(deleteArgs).ConfigureAwait(true);

			if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Prompt)
			{
				_deleteDialogMessage = $"Are you sure you wish to delete '{deleteArgs.Items[0].Name}'?";
				StateHasChanged();
				var choice = await DeleteDialog.ShowAndWaitResultAsync().ConfigureAwait(true);
				deleteArgs.Resolution = choice == "yes" ? DeleteArgs.DeleteResolutions.Delete : DeleteArgs.DeleteResolutions.Cancel;
			}

			if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Delete && deleteArgs.Items.Length > 0)
			{
				try
				{
					var result = await DataProvider.DeleteAsync(deleteArgs.Items[0], CancellationToken.None).ConfigureAwait(true);
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
		}
	}

	private async Task DeleteFilesAsync()
	{
		if (Table?.Selection != null && DeleteDialog != null)
		{
			var deleteArgs = new DeleteArgs
			{
				Items = Table.GetSelectedItems(),
				Resolution = DeleteArgs.DeleteResolutions.Prompt
			};

			await DeleteRequest.InvokeAsync(deleteArgs).ConfigureAwait(true);

			if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Prompt)
			{
				_deleteDialogMessage = deleteArgs.Items.Length == 1
					? $"Are you sure you wish to delete '{deleteArgs.Items[0].Name}'?"
					: $"Are you sure you wish to delete these {deleteArgs.Items.Length} items?";
				StateHasChanged();
				var choice = await DeleteDialog.ShowAndWaitResultAsync().ConfigureAwait(true);
				deleteArgs.Resolution = choice == "yes" ? DeleteArgs.DeleteResolutions.Delete : DeleteArgs.DeleteResolutions.Cancel;
			}

			if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Delete)
			{
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
		}
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
			// allow application to process conflicts
			await MoveCopyConflict.InvokeAsync(conflictArgs).ConfigureAwait(true);

			// if any source and target path are the same then user is copy/moving from same folder - so hide overwrite option
			var showOverwrite = !conflictArgs.Payload.Any(x => conflictArgs.Conflicts.Any(y => y.Path == x.Path));

			if (conflictArgs.ConflictResolution == ConflictResolutions.Prompt)
			{
				// check if target folder is source folder?
				var parentPaths = payload.Select(x => x.ParentPath).Distinct().ToArray();
				conflictArgs.ConflictResolution = parentPaths.Any(x => x == targetPath)
					? await PromptUserForConflictResolution([], false, AllowRenameConflicts, "The source and destination filenames are the same.").ConfigureAwait(true)
					: await PromptUserForConflictResolution([.. conflictArgs.Conflicts.Select(x => FileExplorerItem.GetNameFromPath(x.Path))], showOverwrite).ConfigureAwait(true);
			}
		}

		if (conflictArgs.Conflicts.Count == 0 || conflictArgs.ConflictResolution != ConflictResolutions.Cancel)
		{
			// allow app to perform custom move / copy
			var performMoveCopy = true;
			if (CustomMoveCopy.HasDelegate)
			{
				var customArgs = new CustomMoveCopyArgs
				{
					ConflictResolution = conflictArgs.ConflictResolution,
					Payload = conflictArgs.Payload,
					Conflicts = conflictArgs.Conflicts,
					IsCopy = isCopy,
					TargetPath = targetPath
				};
				await CustomMoveCopy.InvokeAsync(customArgs).ConfigureAwait(true);
				performMoveCopy = !customArgs.CancelDefault;
			}

			// perform default move / copy behaviour?
			if (performMoveCopy)
			{
				foreach (var source in conflictArgs.Payload.ToArray())
				{
					if (conflictArgs.ConflictResolution == ConflictResolutions.Rename)
					{
						// get a unique name
						var newPath = $"{conflictArgs.TargetPath.TrimEnd('/')}/{GetUniqueName(source, conflictArgs.TargetItems)}";
						var delta = new Dictionary<string, object?>
						{
							{  "Path", newPath },
							{  "Copy", conflictArgs.IsCopy }
						};
						var result = await DataProvider.UpdateAsync(source, delta, CancellationToken.None).ConfigureAwait(true);
					}
					else
					{
						// delete conflicting target file?
						var newPath = $"{conflictArgs.TargetPath}/{source.Name}";
						if (conflictArgs.ConflictResolution == ConflictResolutions.Overwrite && conflictArgs.Conflicts.Any(x => x.Name == source.Name))
						{
							// check source and destination are not same file
							if (newPath == source.Path)
							{
								await ExceptionHandler.InvokeAsync(new InvalidOperationException("Operation Failed: Source and Destination are the same")).ConfigureAwait(true);
								continue;
							}
							else
							{
								var target = new FileExplorerItem { EntryType = source.EntryType, Path = newPath };
								var result = await DataProvider.DeleteAsync(target, CancellationToken.None).ConfigureAwait(true);
							}
						}

						// move or copy entry if no conflict or overwrite chosen
						if (conflictArgs.ConflictResolution == ConflictResolutions.Overwrite || !conflictArgs.Conflicts.Any(x => x.Name == source.Name))
						{
							var delta = new Dictionary<string, object?>
							{
								{  "Path", newPath },
								{  "Copy", conflictArgs.IsCopy }
							};
							var result = await DataProvider.UpdateAsync(source, delta, CancellationToken.None).ConfigureAwait(true);
						}
					}
				}
			}

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
				var node = Tree?.Search(x => x.Data?.Path == targetPath);
				if (Tree != null && node != null)
				{
					await Tree.SelectNode(node).ConfigureAwait(true);
				}
			}

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
