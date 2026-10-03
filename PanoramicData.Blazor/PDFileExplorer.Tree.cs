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
		var selectedFolder = _selectedNode?.Data;
		UpdateTreeMenuItems(string.IsNullOrWhiteSpace(selectedFolder?.Path) ? null : selectedFolder);
		UpdateSeparators(TreeContextItems);

		// allow application to alter tree context menu state
		args.Context = selectedFolder;
		await UpdateTreeContextState.InvokeAsync(args).ConfigureAwait(true);
	}

	/// <summary>
	/// Shows the tree's menu items that apply to the selected folder, if any. The root folder cannot be renamed,
	/// deleted, copied or cut.
	/// </summary>
	private void UpdateTreeMenuItems(FileExplorerItem? folder)
	{
		var canAdd = folder?.CanAddItems == true;
		var isChangeable = folder is not null && !string.IsNullOrEmpty(folder.ParentPath);

		_menuNewFolder.IsVisible = canAdd;
		_menuUploadFiles.IsVisible = canAdd;
		_menuRename.IsVisible = isChangeable && folder!.CanRename;
		_menuDelete.IsVisible = isChangeable && folder!.CanDelete;
		_menuCopy.IsVisible = isChangeable;
		_menuCut.IsVisible = _menuDelete.IsVisible;
		_menuPaste.IsVisible = canAdd && _copyPayload.Count > 0;
	}

	private async Task OnTreeContextMenuItemClickAsync(MenuItem item)
	{
		// notify application and allow cancel
		var args = new MenuItemEventArgs(Tree!, item);
		await TreeContextMenuClick.InvokeAsync(args).ConfigureAwait(true);

		if (args.Cancel || Tree?.SelectedNode?.Data is not { } folder)
		{
			return;
		}

		await (item.Text switch
		{
			"Delete" => DeleteFolderAsync(),
			"Rename" => Tree.BeginEdit(),
			"New Folder" => CreateNewFolderAsync(),
			"Upload Files" => ShowUploadDialogAsync(),
			"Copy" or "Cut" => SetCopyPayload([folder], item.Text == "Cut"),
			"Paste" => PasteAsync(folder.Path),
			_ => Task.CompletedTask
		}).ConfigureAwait(true);
	}

	private async Task OnTreeKeyDownAsync(KeyboardEventArgs args)
	{
		var node = Tree?.SelectedNode;
		if (node?.IsEditing == true || node?.Data is not { } folder)
		{
			return;
		}

		await ((args.Code, args.CtrlKey) switch
		{
			("Delete", _) when folder.CanDelete => DeleteFolderAsync(),
			("KeyC", true) => SetCopyPayload([folder], false),
			("KeyX", true) when folder.CanDelete => SetCopyPayload([folder], true),
			("KeyV", true) => PasteAsync(folder.Path),
			_ => Task.CompletedTask
		}).ConfigureAwait(true);
	}

	private async Task OnTreeBeforeEdit(TreeNodeBeforeEditEventArgs<FileExplorerItem> args)
	{
		if (args.Node.Data != null)
		{
			var renameArgs = new RenameArgs { Item = args.Node.Data };
			await BeforeRename.InvokeAsync(renameArgs).ConfigureAwait(true);
			args.Cancel = renameArgs.Cancel;
		}

		if (!CanRenameInTree(args.Node))
		{
			args.Cancel = true;
		}
	}

	/// <summary>
	/// Whether a folder may be renamed in the tree: neither the root nor a folder that cannot be renamed.
	/// </summary>
	private bool CanRenameInTree(TreeNode<FileExplorerItem> node)
		=> AllowRename
			&& node.ParentNode != null
			&& node.Data?.CanRename != false
			&& !string.IsNullOrEmpty(node.Data?.ParentPath);

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
