using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public partial class PDFileExplorer
    {
		private TreeNode<FileExplorerItem>? _selectedNode;
		public string FolderPath = string.Empty;

		private PDTree<FileExplorerItem>? Tree { get; set; }
		private PDTable<FileExplorerItem>? Table { get; set; }

		/// <summary>
		/// Sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

		/// <summary>
		/// Sets the Tree context menu items.
		/// </summary>
		[Parameter]
		public List<MenuItem> TreeContextItems { get; set; } = new List<MenuItem>
			{
				new MenuItem { Text = "Rename", IconCssClass = "fas fa-fw fa-pencil-alt" },
				new MenuItem { Text = "New Folder", IconCssClass = "fas fa-fw fa-plus" },
				new MenuItem { IsSeparator = true },
				new MenuItem { Text = "Delete", IconCssClass = "fas fa-fw fa-trash-alt" }
			};

		/// <summary>
		/// Sets the Table context menu items.
		/// </summary>
		[Parameter]
		public List<MenuItem> TableContextItems { get; set; } = new List<MenuItem>
			{
				new MenuItem { Text = "Open", IconCssClass = "fas fa-fw fa-folder-open" },
				new MenuItem { Text = "Download", IconCssClass = "fas fa-fw fa-file-download" },
				new MenuItem { IsSeparator = true },
				new MenuItem { Text = "Rename", IconCssClass = "fas fa-fw fa-pencil-alt" },
				new MenuItem { IsSeparator = true },
				new MenuItem { Text = "Delete", IconCssClass = "fas fa-fw fa-trash-alt" }
			};

		/// <summary>
		/// Sets the Table context menu items.
		/// </summary>
		[Parameter]
		public List<ToolbarItem> ToolbarItems { get; set; } = new List<ToolbarItem>
			{
				new ToolbarButton { Key = "navigate-up", ToolTip="Navigate up to parent folder", IconCssClass="fas fa-fw fa-arrow-up",  CssClass="btn-secondary" },
				new ToolbarButton { Key = "create-folder", Text = "New Folder", ToolTip="Create a new folder", IconCssClass="fas fa-fw fa-folder-plus", CssClass="btn-secondary" },
				new ToolbarButton { Key = "delete", Text = "Delete", ToolTip="Delete the selected files and folders", IconCssClass="fas fa-fw fa-trash-alt", CssClass="btn-danger", ShiftRight = true },
			};

		/// <summary>
		/// Sets the Table column configuration.
		/// </summary>
		[Parameter] public List<PDColumnConfig> ColumnConfig { get; set; } = new List<PDColumnConfig>
			{
				new PDColumnConfig { Id = "Icon", Title = "" },
				new PDColumnConfig { Id = "Name", Title = "Name" },
				new PDColumnConfig { Id = "Type", Title = "Type" },
				new PDColumnConfig { Id = "Size", Title = "Size" },
				new PDColumnConfig { Id = "Modified", Title = "Modified" }
			};

		[Parameter] public string? UploadUrl { get; set; }

		/// <summary>
		/// Event raised whenever the user clicks on a context menu item from the tree.
		/// </summary>
		[Parameter] public EventCallback<MenuItemEventArgs> TreeContextMenuClick { get; set; }

		/// <summary>
		/// Event raised whenever the user clicks on a context menu item from the table.
		/// </summary>
		[Parameter] public EventCallback<MenuItemEventArgs> TableContextMenuClick { get; set; }

		/// <summary>
		/// Event raised whenever the user requests to download a file.
		/// </summary>
		[Parameter] public EventCallback<TableEventArgs<FileExplorerItem>> TableDownloadRequest { get; set; }

		/// <summary>
		/// Event raised whenever the user drops one or more files on to the file explorer.
		/// </summary>
		[Parameter] public EventCallback<DropZoneEventArgs> UploadRequest { get; set; }

		/// <summary>
		/// Event raised whenever a file upload starts.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadEventArgs> UploadStarted { get; set; }

		/// <summary>
		/// Event raised periodically during a file upload.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadProgressEventArgs> UploadProgress { get; set; }

		/// <summary>
		/// Event raised whenever a file upload completes.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadEventArgs> UploadCompleted { get; set; }

		/// <summary>
		/// Event raised whenever the tree context menu may need updating.
		/// </summary>
		[Parameter] public EventCallback<MenuItemsEventArgs> UpdateTreeContextState { get; set; }

		/// <summary>
		/// Event raised whenever the table context menu may need updating.
		/// </summary>
		[Parameter] public EventCallback<MenuItemsEventArgs> UpdateTableContextState { get; set; }

		/// <summary>
		/// Event raised whenever the toolbar may need updating.
		/// </summary>
		[Parameter] public EventCallback<List<ToolbarItem>> UpdateToolbarState { get; set; }

		/// <summary>
		/// Event raised whenever the user clicks on a toolbar button.
		/// </summary>
		[Parameter] public EventCallback<string> ToolbarClick { get; set; }

		/// <summary>
		/// Gets or sets CSS classes to append.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = string.Empty;

		/// <summary>
		/// Filters file items out of tree and shows root items in table on tree first load.
		/// </summary>
		private void OnTreeItemsLoaded(List<FileExplorerItem> items)
		{
		 	items.RemoveAll(x => x.EntryType == FileExplorerItemType.File);
		}

		private async Task OnTreeNodeUpdated(TreeNode<FileExplorerItem> node)
		{
			if (node?.Data != null && node?.ParentNode == null) // root node updated
			{
				await Tree!.SelectNode(node!).ConfigureAwait(true);
				StateHasChanged();
			}
		}

		private async Task OnTreeSelectionChange(TreeNode<FileExplorerItem> node)
		{
			_selectedNode = node;
			if (node?.Data != null)
			{
				FolderPath = node.Data.Path;
				await RefreshTable().ConfigureAwait(true);
				await RefreshToolbar().ConfigureAwait(true);
			}
		}

		private async Task OnTreeContextMenuUpdateState(MenuItemsEventArgs args)
		{
			if (_selectedNode?.Data != null)
			{
				TreeContextItems.Single(x => x.Text == "Delete").IsDisabled = _selectedNode.Data.ParentPath?.Length == 0;
				TreeContextItems.Single(x => x.Text == "Rename").IsDisabled = _selectedNode.Data.ParentPath?.Length == 0;

				// allow application to alter tree context menu state
				await UpdateTreeContextState.InvokeAsync(args).ConfigureAwait(true);
			}
		}

		private async Task OnTreeContextMenuItemClick(MenuItem item)
		{
			// notify application and allow cancel
			var args = new MenuItemEventArgs(Tree!, item);
			await TreeContextMenuClick.InvokeAsync(args).ConfigureAwait(true);

			if (!args.Cancel)
			{
				if (Tree?.SelectedNode?.Data != null)
				{
					if (item.Text == "Delete")
					{
						var result = await DataProvider.DeleteAsync(Tree.SelectedNode.Data, CancellationToken.None).ConfigureAwait(true);
						if (result.Success)
						{
							await Tree.RemoveNodeAsync(Tree.SelectedNode).ConfigureAwait(true);
						}
					}
					else if (item.Text == "Rename")
					{
						await Tree.BeginEdit().ConfigureAwait(true);
					}
					else if (item.Text == "New Folder")
					{
						await CreateNewFolder().ConfigureAwait(true);
					}
				}
			}
		}

		private void OnTreeBeforeEdit(TreeNodeBeforeEditEventArgs<FileExplorerItem> args)
		{
			if (args.Node.ParentNode == null)
			{
				args.Cancel = true;
			}
		}

		private async Task OnTreeAfterEdit(TreeNodeAfterEditEventArgs<FileExplorerItem> args)
		{
			if (Tree?.SelectedNode?.Data != null)
			{
				var item = Tree.SelectedNode.Data;
				var previousPath = item.Path;
				var newPath = $"{item.ParentPath}/{args.NewValue}";
				// inform data provider
				var delta = new Dictionary<string, object>
					{
						{  "Path", newPath }
					};
				var result = await DataProvider.UpdateAsync(Tree.SelectedNode.Data, delta, CancellationToken.None).ConfigureAwait(true);
				if(result.Success)
				{
					// synchronize existing node paths for tree and table
					Tree.RootNode.Walk((x) => {
						if (x.Data != null)
						{
							x.Key = x.Key.ReplacePathPrefix(previousPath, newPath);
							x.Data.Path = x.Data.Path.ReplacePathPrefix(previousPath, newPath);
						}
						return true;
					});
					Table!.ItemsToDisplay.ToList().ForEach(x => x.Path = x.Path.ReplacePathPrefix(previousPath, newPath));
					await OnTreeSelectionChange(Tree.SelectedNode).ConfigureAwait(true);
				}
			}
		}

		private void OnTableItemsLoaded(List<FileExplorerItem> items)
		{
			items.Insert(0, new FileExplorerItem { Path = Path.Combine(_selectedNode?.Data?.Path ?? "", ".."), EntryType = FileExplorerItemType.Directory, CanCopyMove = false });
		}

		private async Task OnTableDoubleClick(FileExplorerItem item)
		{
			if (!Table!.IsEditing && item.EntryType == FileExplorerItemType.Directory)
			{
				await NavigateFolder(item.Path).ConfigureAwait(true);
			}
		}

		private void OnTableBeforeEdit(TableBeforeEditEventArgs<FileExplorerItem> args)
		{
			if(args.Item.Name == ".." || args.Item.IsUploading)
			{
				args.Cancel = true;
			}
			else
			{
				// only want to select the filename portion of the text
				args.SelectionEnd = Path.GetFileNameWithoutExtension(args.Item.Name).Length;
			}
		}

		private async Task OnTableAfterEdit(TableAfterEditEventArgs<FileExplorerItem> args)
		{
			// cancel is new name is empty
			if (args.NewValues.ContainsKey("Name"))
			{
				if (string.IsNullOrWhiteSpace(args.NewValues["Name"]))
				{
					args.Cancel = true;
				}
				else
				{
					var previousPath = args.Item.Path;
					var newPath = $"{args.Item.ParentPath}/{args.NewValues["Name"]}";
					// check for duplicate name
					if (Table!.ItemsToDisplay.Any(x => x.Path == newPath))
					{
						args.Cancel = true;
					}
					else
					{
						// inform data provider
						var delta = new Dictionary<string, object>
						{
							{  "Path", newPath }
						};
						var result = await DataProvider.UpdateAsync(args.Item, delta, CancellationToken.None).ConfigureAwait(true);
						if (result.Success)
						{
							// if folder renamed then update nodes
							if (args.Item.EntryType == FileExplorerItemType.Directory)
							{
								await DirectoryRename(previousPath, newPath).ConfigureAwait(true);
							}
						}
					}
				}
			}
		}

		private async Task OnTableContextMenuUpdateState(MenuItemsEventArgs args)
		{
			// reset all states
			TableContextItems.ForEach(x => { x.IsDisabled = false; x.IsVisible = true; });

			if (Table!.Selection.Count == 0)
			{
				TableContextItems.ForEach(x => x.IsDisabled = true);
				TableContextItems.Single(x => x.Text == "Download").IsVisible = false;
				args.Cancel = true;
			}
			else if (Table!.Selection.Count == 1)
			{
				// if .. selected then only allow open
				var selectedPath = Table!.Selection[0];
				if (!IsValidSelection())
				{
					TableContextItems.Single(x => x.Text == "Download").IsVisible = false;
					TableContextItems.Single(x => x.Text == "Rename").IsDisabled = true;
					TableContextItems.Single(x => x.Text == "Delete").IsDisabled = true;
					return;
				}

				// update state - disallow open files and download folders
				var item = Table.ItemsToDisplay.Single(x => x.Path == selectedPath);
				if (item.EntryType == FileExplorerItemType.Directory)
				{
					TableContextItems.Single(x => x.Text == "Download").IsVisible = false;
				}
				else
				{
					TableContextItems.Single(x => x.Text == "Open").IsVisible = false;
				}
			}
			else
			{
				// multiple selection - only delete is an option
				TableContextItems.Single(x => x.Text == "Open").IsVisible = false;
				TableContextItems.Single(x => x.Text == "Download").IsVisible = false;
				TableContextItems.Single(x => x.Text == "Rename").IsVisible = false;

				// check can delete all selection?
				if(!IsValidSelection())
				{
					TableContextItems.Single(x => x.Text == "Delete").IsDisabled = true;
				}
			}

			// allow application to alter table context menu state
			await UpdateTableContextState.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnTableContextMenuItemClick(MenuItem menuItem)
		{
			if (Table is null)
			{
				throw new InvalidOperationException("_table is null");
			}

			// notify application and allow cancel
			var args = new MenuItemEventArgs(Table, menuItem);
			await TableContextMenuClick.InvokeAsync(args).ConfigureAwait(true);

			if (Table!.Selection.Count > 0)
			{
				if (!args.Cancel)
				{
					var path = Table!.Selection[0];
					if (menuItem.Text == "Open")
					{
						await NavigateFolder(path).ConfigureAwait(true);
					}
					else if (menuItem.Text == "Delete")
					{
						await DeleteSelectedFiles().ConfigureAwait(true);
					}
					else if(menuItem.Text == "Rename")
					{
						await Table!.BeginEdit().ConfigureAwait(true);
					}
					else if(menuItem.Text == "Download")
					{
						var item = Table.ItemsToDisplay.Find(x => x.Path == Table!.Selection[0]);
						await TableDownloadRequest.InvokeAsync(new TableEventArgs<FileExplorerItem>(item)).ConfigureAwait(true);
					}
				}
			}
		}

		private async Task OnTableSelectionChanged()
		{
			await RefreshToolbar()
				.ConfigureAwait(true);
		}

		private async Task DirectoryRename(string oldPath, string newPath)
		{
			// synchronize existing node paths for tree and table
			Tree!.RootNode.Walk((x) =>
			{
				if (x.Data != null)
				{
					x.Key = x.Key.ReplacePathPrefix(oldPath, newPath);
					x.Data.Path = x.Data.Path.ReplacePathPrefix(oldPath, newPath);
				}
				return true;
			});
			Table!.ItemsToDisplay.ToList().ForEach(x => x.Path = x.Path.ReplacePathPrefix(oldPath, newPath));
			if (Tree?.SelectedNode != null)
			{
				await OnTreeSelectionChange(Tree.SelectedNode).ConfigureAwait(true);
			}
		}

		private async Task OnFilesDropped(DropZoneEventArgs args)
		{
			// notify application and allow for cancel
			await UploadRequest.InvokeAsync(args).ConfigureAwait(true);

			// add current path so it can be passed along with uploads
			if (Tree?.SelectedNode?.Data != null)
			{
				args.State = Tree.SelectedNode.Data.Path;
			}
		}

		private void OnUploadStarted(DropZoneUploadEventArgs args)
		{
			UploadStarted.InvokeAsync(args);
		}

		private async Task OnUploadProgress(DropZoneUploadProgressEventArgs args)
		{
			if(Table is null)
			{
				return;
			}

			// is the upload happening to the current path?
			if(args.Path == FolderPath)
			{
				// add virtual file item
				var item = Table.ItemsToDisplay.Find(x => x.Name == args.Name);
				if (item is null)
				{
					item = new FileExplorerItem
					{
						Path = args.Path,
						DateCreated = DateTimeOffset.Now,
						DateModified = DateTimeOffset.Now,
						EntryType = FileExplorerItemType.File,
						FileSize = args.Size,
						Name = args.Name,
						IsUploading = true
					};
					Table.ItemsToDisplay.Add(item);
				}
				item.UploadProgress = args.Progress;
			}

			await UploadProgress.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnUploadCompleted(DropZoneUploadEventArgs args)
		{
			// is the upload happening to the current path?
			if (args.Path == FolderPath)
			{
				// add virtual file item
				var item = Table?.ItemsToDisplay.Find(x => x.Name == args.Name);
				if(item != null)
				{
					item.IsUploading = false;
				}
			}

			await UploadCompleted.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnToolbarButtonClick(string key)
		{
			switch(key)
			{
				case "navigate-up":
					await NavigateUp().ConfigureAwait(true);
					break;

				case "create-folder":
					await CreateNewFolder().ConfigureAwait(true);
					break;

				case "delete":
					await DeleteSelectedFiles().ConfigureAwait(true);
					break;

				default:
					await ToolbarClick.InvokeAsync(key).ConfigureAwait(true);
					break;
			}
		}

		private async Task OnDrop(DropEventArgs args)
		{
			// unwrap FileExplorerItem
			if (args.Target is TreeNode<FileExplorerItem> node)
			{
				args.Target = node.Data;
			}

			// source and target are file items - and target is folder?
			if (args.Target is FileExplorerItem target && target.EntryType == FileExplorerItemType.Directory &&
			   args.Payload is List<FileExplorerItem> payload)
			{
				// check not dropping an item onto itself
				if(payload.Any(x => x.Path == target.Path))
				{
					return;
				}

				// check can move/copy all items
				if(payload.Any(x => !x.CanCopyMove))
				{
					return;
				}

				// moving item to parent folder?
				var targetPath = target.Path;
				if(target.Name == "..")
				{
					targetPath = Path.GetDirectoryName(target.ParentPath);
				}

				// move file or folder into folder
				//await MoveOrCopyFiles(payload, target, args.Ctrl).ConfigureAwait(true);

				foreach (var source in payload)
				{
					var delta = new Dictionary<string, object>
					{
						{  "Path", targetPath },
						{  "Copy", args.Ctrl }
					};
					var result = await DataProvider.UpdateAsync(source, delta, CancellationToken.None).ConfigureAwait(true);
				}
				if (Table != null && !args.Ctrl)
				{
					await Table.RefreshAsync().ConfigureAwait(true);
					if (Tree?.SelectedNode != null)
					{
						await Tree.RefreshNodeAsync(Tree.SelectedNode).ConfigureAwait(true);
					}
				}
			}
		}

		private bool IsValidSelection()
		{
			foreach (var path in Table!.Selection)
			{
				// disallow if .. selected
				if (path.EndsWith(".."))
				{
					return false;
				}

				// disallow delete if uploading
				var item = Table.ItemsToDisplay.Single(x => x.Path == path);
				if (item?.IsUploading != false)
				{
					return false;
				}
			}
			return true;
		}

		private async Task NavigateFolder(string path)
		{
			if (Path.GetFileName(path) == "..")
			{
				if (_selectedNode?.ParentNode != null)
				{
					await Tree!.SelectNode(_selectedNode.ParentNode).ConfigureAwait(true);
				}
			}
			else
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
		}

		private async Task NavigateUp()
		{
			await NavigateFolder("..").ConfigureAwait(true);
		}

		private async Task CreateNewFolder()
		{
			if (Tree?.SelectedNode?.Data != null)
			{
				var newFolderName = Tree.SelectedNode.MakeUniqueText("New Folder");
				var newPath = $"{Tree.SelectedNode.Data.Path}/{newFolderName}";
				var newItem = new FileExplorerItem { EntryType = FileExplorerItemType.Directory, Path = newPath };
				var result = await DataProvider.CreateAsync(newItem, CancellationToken.None).ConfigureAwait(true);
				if (result.Success)
				{
					// refresh current node, select new node and finally begin edit mode
					await Tree.RefreshNodeAsync(Tree.SelectedNode).ConfigureAwait(true);
					var newNode = Tree.RootNode.Find(newItem.Path);
					if (newNode != null)
					{
						await Tree.SelectNode(newNode).ConfigureAwait(true);
						await Tree.BeginEdit().ConfigureAwait(true);
					}
				}
			}
		}

		private async Task DeleteSelectedFiles()
		{
			// selection key is path of item
			if (Table?.Selection != null)
			{
				// delete all selected items
				foreach (var path in Table.Selection)
				{
					var fileItem = Table!.ItemsToDisplay.Find(x => x.Path == path);
					if (fileItem != null)
					{
						await DataProvider.DeleteAsync(fileItem, CancellationToken.None).ConfigureAwait(true);
					}
				}

				// refresh tree, table and toolbar
				await RefreshTree().ConfigureAwait(true);
				await RefreshTable().ConfigureAwait(true);
				await RefreshToolbar().ConfigureAwait(true);
			}
		}

		/// <summary>
		/// Forces the tree component of the file explorer to be refreshed.
		/// </summary>
		public async Task RefreshTree()
		{
			// refresh tree - parent node will already be selected
			var node = Tree?.SelectedNode;
			if (node != null)
			{
				node.Nodes = null;
				await Tree!.RefreshNodeAsync(node)!.ConfigureAwait(true);
			}
		}

		/// <summary>
		/// Forces the table component of the file explorer to be refreshed.
		/// </summary>
		public async Task RefreshTable()
		{
			if(Table is null)
			{
				throw new InvalidOperationException("_table should not be null.");
			}

			Table.Selection.Clear();

			// explicitly state search path else fetch will use previous value as OnParametersSet not yet called
			await Table
				.RefreshAsync(FolderPath)
				.ConfigureAwait(true);
			StateHasChanged();
		}

		/// <summary>
		/// Forces the toolbar component of the file explorer to be refreshed.
		/// </summary>
		public async Task RefreshToolbar()
		{
			// up button
			var upButton = ToolbarItems.Find(x => x.Key == "navigate-up");
			if (upButton != null)
			{
				upButton.IsEnabled = Tree?.SelectedNode?.ParentNode != null;
			}

			// create folder button - always enabled by default

			// delete button
			var deleteButton = ToolbarItems.Find(x => x.Key == "delete");
			if (deleteButton != null)
			{
				deleteButton.IsEnabled = Table!.Selection.Count > 0 && !Table!.Selection.Any(x => x.EndsWith(".."));
			}

			// allow application to alter toolbar state
			await UpdateToolbarState.InvokeAsync(ToolbarItems).ConfigureAwait(true);
		}

		/// <summary>
		/// Gets the paths of all currently selected files and folders in the table view.
		/// </summary>
		public string[] SelectedFilesAndFolders
		{
			get
			{
				return Table!.Selection.ToArray();
			}
		}
	}
}
