namespace PanoramicData.Blazor;

/// <summary>
/// Handling of the folder tree of a <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileExplorer
{
	/// <summary>
	/// Filters file items out of tree and shows root items in table on tree first load.
	/// </summary>
	private void OnTreeItemsLoaded(List<FileExplorerItem> items)
	{
		// remove all file entries
		items.RemoveAll(x => x.EntryType == FileExplorerItemType.File);

		// remove any paths to exclude
		items.RemoveAll(x => ExcludedPaths.Contains(x.Path));
	}

	private async Task OnTreeReady()
	{
		if (AutoExpand && Tree?.RootNode?.Nodes?.Count > 0)
		{
			// auto expand root node
			var firstNode = Tree.RootNode.Nodes[0];
			await Tree!.RefreshNodeAsync(firstNode).ConfigureAwait(true);
			await Tree!.SelectNode(firstNode).ConfigureAwait(true);
			await Ready.InvokeAsync(null).ConfigureAwait(true);
		}
	}

	private async Task OnTreeSelectionChangeAsync(TreeNode<FileExplorerItem> node)
	{
		_selectedNode = node;
		if (node?.Data != null && FolderPath != node.Data.Path)
		{
			FolderPath = node.Data.Path;
			await RefreshTableAsync().ConfigureAwait(true); // will clear selection and preview
			await RefreshToolbarAsync().ConfigureAwait(true);
			await FolderChanged.InvokeAsync(node.Data).ConfigureAwait(true);
			_previewItem = node.Data;
		}
	}

	private async Task OnTreeContextMenuUpdateStateAsync(MenuItemsEventArgs args)
	{
		var parentPath = _selectedNode?.Data?.ParentPath ?? string.Empty;
		var selectedFolder = _selectedNode?.Data;
		var selectedPath = _selectedNode?.Data?.Path ?? string.Empty;
		var isRoot = string.IsNullOrEmpty(parentPath);
		var folderSelected = !string.IsNullOrWhiteSpace(selectedPath);

		_menuNewFolder.IsVisible = folderSelected && selectedFolder?.CanAddItems == true;
		_menuUploadFiles.IsVisible = folderSelected && selectedFolder?.CanAddItems == true;
		_menuRename.IsVisible = folderSelected && !isRoot && selectedFolder?.CanRename == true;
		_menuDelete.IsVisible = folderSelected && !isRoot && selectedFolder?.CanDelete == true;
		_menuCopy.IsVisible = folderSelected && !isRoot;
		_menuCut.IsVisible = folderSelected && !isRoot && selectedFolder?.CanDelete == true;
		_menuPaste.IsVisible = folderSelected && selectedFolder?.CanAddItems == true && _copyPayload.Count > 0;
		_menuSep3.IsVisible = _menuSep2.IsVisible = _menuSep1.IsVisible = true;
		_menuSep3.IsVisible = ShowSeparator(_menuSep3, TreeContextItems);
		_menuSep2.IsVisible = ShowSeparator(_menuSep2, TreeContextItems);
		_menuSep1.IsVisible = ShowSeparator(_menuSep1, TreeContextItems);

		// allow application to alter tree context menu state
		args.Context = selectedFolder;
		await UpdateTreeContextState.InvokeAsync(args).ConfigureAwait(true);
	}

	private async Task OnTreeContextMenuItemClickAsync(MenuItem item)
	{
		// notify application and allow cancel
		var args = new MenuItemEventArgs(Tree!, item);
		await TreeContextMenuClick.InvokeAsync(args).ConfigureAwait(true);

		if (!args.Cancel && Tree?.SelectedNode?.Data != null)
		{
			if (item.Text == "Delete")
			{
				await DeleteFolderAsync().ConfigureAwait(true);
			}
			else if (item.Text == "Rename")
			{
				await Tree.BeginEdit().ConfigureAwait(true);
			}
			else if (item.Text == "New Folder")
			{
				await CreateNewFolderAsync().ConfigureAwait(true);
			}
			else if (item.Text == "Upload Files")
			{
				if (UploadDialog != null)
				{
					await UploadDialog.ShowAsync().ConfigureAwait(true);
				}
			}
			else if (item.Text == "Copy" || item.Text == "Cut")
			{
				_copyPayload.Clear();
				_copyPayload.Add(Tree.SelectedNode.Data);
				_moveCopyPayload = item.Text == "Cut";
			}
			else if (item.Text == "Paste")
			{
				var targetPath = Tree.SelectedNode.Data.Path;
				await MoveCopyFilesAsync(_copyPayload, targetPath, !_moveCopyPayload).ConfigureAwait(true);
				if (_moveCopyPayload) // clear copy payload only if move
				{
					_copyPayload.Clear();
				}
			}
		}
	}

	private async Task OnTreeKeyDownAsync(KeyboardEventArgs args)
	{
		if (Tree?.SelectedNode?.IsEditing != true)
		{
			if (args.Code == "Delete" && Tree?.SelectedNode?.Data?.CanDelete == true)
			{
				await DeleteFolderAsync().ConfigureAwait(true);
			}
			else if (args.Code == "KeyC" && args.CtrlKey && Tree!.SelectedNode?.Data != null)
			{
				_copyPayload.Clear();
				_copyPayload.Add(Tree!.SelectedNode.Data);
				_moveCopyPayload = false;
			}
			else if (args.Code == "KeyX" && args.CtrlKey && Tree!.SelectedNode?.Data != null && Tree!.SelectedNode?.Data?.CanDelete == true)
			{
				_copyPayload.Clear();
				_copyPayload.Add(Tree!.SelectedNode.Data);
				_moveCopyPayload = true;
			}
			else if (args.Code == "KeyV" && args.CtrlKey && Tree!.SelectedNode?.Data != null)
			{
				var targetPath = Tree.SelectedNode.Data.Path;
				await MoveCopyFilesAsync(_copyPayload, targetPath, !_moveCopyPayload).ConfigureAwait(true);
				if (_moveCopyPayload) // clear copy payload only if move
				{
					_copyPayload.Clear();
				}
			}
		}
	}

	private async Task OnTreeBeforeEdit(TreeNodeBeforeEditEventArgs<FileExplorerItem> args)
	{
		if (args != null)
		{
			if (args.Node.Data != null)
			{
				var renameArgs = new RenameArgs { Item = args.Node.Data };
				await BeforeRename.InvokeAsync(renameArgs).ConfigureAwait(true);
				args.Cancel = renameArgs.Cancel;
			}

			if (!AllowRename || args.Node.ParentNode == null || args.Node?.Data?.CanRename == false || string.IsNullOrEmpty(args.Node?.Data?.ParentPath))
			{
				args.Cancel = true;
			}
		}
	}

	private async Task OnTreeAfterEditAsync(TreeNodeAfterEditEventArgs<FileExplorerItem> args)
	{
		if (Tree?.SelectedNode?.Data != null)
		{
			var item = Tree.SelectedNode.Data;
			var previousPath = item.Path;
			var newPath = $"{item.ParentPath.TrimEnd('/')}/{args.NewValue}";

			// check for and disallow duplicate folder name
			if (args.NewValue.StartsWith('.'))
			{
				args.Cancel = true;
				await OnException(new PDFileExplorerException($"Names may not begin with a period (.)")).ConfigureAwait(true);
				return;
			}
			else if (string.Equals(args.NewValue, args.OldValue, StringComparison.Ordinal))
			{
				// unchanged, so nothing to do and nothing to report
				args.Cancel = true;
				return;
			}
			else if (HasOtherSiblingWithText(Tree.SelectedNode, args.NewValue))
			{
				// another folder already has the name; the node itself does not count, so a case-only rename is allowed
				args.Cancel = true;
				await OnException(new PDFileExplorerException($"A Folder named '{args.NewValue}' already exists")).ConfigureAwait(true);
				return;
			}

			// inform data provider
			var delta = new Dictionary<string, object?>
			{
				{  "Path", newPath }
			};
			var result = await DataProvider.UpdateAsync(Tree.SelectedNode.Data, delta, CancellationToken.None).ConfigureAwait(true);
			if (result.Success)
			{
				await DirectoryRenameAsync(previousPath, newPath).ConfigureAwait(true);
			}
		}
	}

	private static bool HasOtherSiblingWithText(TreeNode<FileExplorerItem> node, string text)
		=> node.ParentNode?.Nodes?.Any(x => x != node && string.Equals(x.Text, text, StringComparison.OrdinalIgnoreCase)) == true;

	private int OnTreeSort(FileExplorerItem item1, FileExplorerItem item2)
	{
		if (TreeSort is null)
		{
			return string.Compare(item1.Name, item2.Name, StringComparison.Ordinal);
		}

		return TreeSort(item1, item2);
	}
}
