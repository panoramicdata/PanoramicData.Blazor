using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Extensions;

namespace PanoramicData.Blazor
{
	public partial class PDFileExplorer
    {
		private TreeNode<FileExplorerItem>? _selectedNode;
		private PDTree<FileExplorerItem>? _tree = null!;
		private PDTable<FileExplorerItem>? _table = null!;
		private PDModal _deleteDialog = null!;
		private PDModal _conflictDialog = null!;
		private string _deleteDialogMessage = string.Empty;
		private string _conflictDialogMessage = string.Empty;
		private string[] _conflictDialogList = new string[0];

		public string FolderPath = string.Empty;

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
		/// Event raised whenever the user double clicks on a file.
		/// </summary>
		[Parameter] public EventCallback<FileExplorerItem> ItemDoubleClick { get; set; }

		/// <summary>
		/// Gets or sets CSS classes to append.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = string.Empty;

		/// <summary>
		/// Determines whether the user may rename items.
		/// </summary>
		[Parameter] public bool AllowRename { get; set; } = true;

		/// <summary>
		/// Determines whether the user may drag items.
		/// </summary>
		[Parameter] public bool AllowDrag { get; set; } = true;

		/// <summary>
		/// Determines whether the user may drop dragged items onto other items.
		/// </summary>
		[Parameter] public bool AllowDrop { get; set; } = true;

		/// <summary>
		/// Determines where sub-folders show an entry (..) to allow navigation to the parent folder.
		/// </summary>
		[Parameter] public bool ShowParentFolder { get; set; } = true;

		/// <summary>
		/// Determines whether the toolbar is visible.
		/// </summary>
		[Parameter] public bool ShowToolbar { get; set; } = true;

		/// <summary>
		/// Determines whether the context menu is available.
		/// </summary>
		[Parameter] public bool ShowContextMenu { get; set; } = true;

		/// <summary>
		/// Sets the allowed selection modes.
		/// </summary>
		[Parameter] public TableSelectionMode SelectionMode { get; set; } = TableSelectionMode.Multiple;

		/// <summary>
		/// Event raises whenever the selection changes.
		/// </summary>
		[Parameter] public EventCallback<FileExplorerItem[]> SelectionChanged { get; set; }

		/// <summary>
		/// Gets or sets a delegate to be called if an exception occurs.
		/// </summary>
		[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

		/// <summary>
		/// Event called whenever the user requests to delete one or more items.
		/// </summary>
		[Parameter] public EventCallback<DeleteArgs> DeleteRequest { get; set; }

		/// <summary>
		/// Event called whenever a move or copy operation is subject to conflicts.
		/// </summary>
		[Parameter] public EventCallback<MoveCopyArgs> MoveCopyConflict { get; set; }

		/// <summary>
		/// Filters file items out of tree and shows root items in table on tree first load.
		/// </summary>
		private void OnTreeItemsLoaded(List<FileExplorerItem> items)
		{
		 	items.RemoveAll(x => x.EntryType == FileExplorerItemType.File);
		}

		private async Task OnTreeNodeUpdatedAsync(TreeNode<FileExplorerItem> node)
		{
			if (node?.Data != null && node?.ParentNode == null) // root node updated
			{
				await _tree!.SelectNode(node!).ConfigureAwait(true);
				StateHasChanged();
			}
		}

		private async Task OnTreeSelectionChangeAsync(TreeNode<FileExplorerItem> node)
		{
			_selectedNode = node;
			if (node?.Data != null)
			{
				FolderPath = node.Data.Path;
				await RefreshTableAsync().ConfigureAwait(true);
				await RefreshToolbarAsync().ConfigureAwait(true);
			}
		}

		private async Task OnTreeContextMenuUpdateStateAsync(MenuItemsEventArgs args)
		{
			if (_selectedNode?.Data != null)
			{
				TreeContextItems.Single(x => x.Text == "Delete").IsDisabled = _selectedNode.Data.ParentPath?.Length == 0;
				TreeContextItems.Single(x => x.Text == "Rename").IsDisabled = _selectedNode.Data.ParentPath?.Length == 0;

				// allow application to alter tree context menu state
				await UpdateTreeContextState.InvokeAsync(args).ConfigureAwait(true);
			}
		}

		private async Task OnTreeContextMenuItemClickAsync(MenuItem item)
		{
			// notify application and allow cancel
			var args = new MenuItemEventArgs(_tree!, item);
			await TreeContextMenuClick.InvokeAsync(args).ConfigureAwait(true);

			if (!args.Cancel)
			{
				if (_tree?.SelectedNode?.Data != null)
				{
					if (item.Text == "Delete")
					{
						await DeleteFolderAsync().ConfigureAwait(true);
					}
					else if (item.Text == "Rename")
					{
						await _tree.BeginEdit().ConfigureAwait(true);
					}
					else if (item.Text == "New Folder")
					{
						await CreateNewFolderAsync().ConfigureAwait(true);
					}
				}
			}
		}

		private void OnTreeBeforeEdit(TreeNodeBeforeEditEventArgs<FileExplorerItem> args)
		{
			if (!AllowRename || args.Node.ParentNode == null)
			{
				args.Cancel = true;
			}
		}

		private async Task OnTreeAfterEditAsync(TreeNodeAfterEditEventArgs<FileExplorerItem> args)
		{
			if (_tree?.SelectedNode?.Data != null)
			{
				var item = _tree.SelectedNode.Data;
				var previousPath = item.Path;
				var newPath = $"{item.ParentPath.TrimEnd('/')}/{args.NewValue}";
				// inform data provider
				var delta = new Dictionary<string, object>
					{
						{  "Path", newPath }
					};
				var result = await DataProvider.UpdateAsync(_tree.SelectedNode.Data, delta, CancellationToken.None).ConfigureAwait(true);
				if(result.Success)
				{
					// synchronize existing node paths for tree and table
					_tree.RootNode.Walk((x) => {
						if (x.Data != null)
						{
							x.Key = x.Key.ReplacePathPrefix(previousPath, newPath);
							x.Data.Path = x.Data.Path.ReplacePathPrefix(previousPath, newPath);
						}
						return true;
					});
					_table!.ItemsToDisplay.ToList().ForEach(x => x.Path = x.Path.ReplacePathPrefix(previousPath, newPath));
					await OnTreeSelectionChangeAsync(_tree.SelectedNode).ConfigureAwait(true);
				}
			}
		}

		private void OnTableItemsLoaded(List<FileExplorerItem> items)
		{
			if (ShowParentFolder && _selectedNode?.Data?.Path != null && _selectedNode.Data.Path != "/")
			{
				items.Insert(0, new FileExplorerItem
				{
					Path = $"{_selectedNode.Data.Path}/..",
					EntryType = FileExplorerItemType.Directory,
					CanCopyMove = false
				});
			}
		}

		private async Task OnTableDoubleClickAsync(FileExplorerItem item)
		{
			if (!_table!.IsEditing)
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

		private void OnTableBeforeEdit(TableBeforeEditEventArgs<FileExplorerItem> args)
		{
			if(!AllowRename || args.Item.Name == ".." || args.Item.IsUploading)
			{
				args.Cancel = true;
			}
			else
			{
				// only want to select the filename portion of the text
				args.SelectionEnd = Path.GetFileNameWithoutExtension(args.Item.Name).Length;
			}
		}

		private async Task OnTableKeyDownAsync(KeyboardEventArgs args)
		{
			if(args.Code == "Delete")
			{
				await DeleteFilesAsync().ConfigureAwait(true);
			}
		}

		private async Task OnTableAfterEditAsync(TableAfterEditEventArgs<FileExplorerItem> args)
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
					if (_table!.ItemsToDisplay.Any(x => x.Path == newPath))
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
								await DirectoryRenameAsync(previousPath, newPath).ConfigureAwait(true);
							}
						}
					}
				}
			}
		}

		private async Task OnTableContextMenuUpdateStateAsync(MenuItemsEventArgs args)
		{
			// reset all states
			TableContextItems.ForEach(x => { x.IsDisabled = false; x.IsVisible = true; });

			if (_table!.Selection.Count == 0)
			{
				TableContextItems.ForEach(x => x.IsDisabled = true);
				TableContextItems.Single(x => x.Text == "Download").IsVisible = false;
				args.Cancel = true;
			}
			else if (_table!.Selection.Count == 1)
			{
				// if .. selected then only allow open
				var selectedPath = _table!.Selection[0];
				if (!IsValidSelection())
				{
					TableContextItems.Single(x => x.Text == "Download").IsVisible = false;
					TableContextItems.Single(x => x.Text == "Rename").IsDisabled = true;
					TableContextItems.Single(x => x.Text == "Delete").IsDisabled = true;
					return;
				}

				// update state - disallow open files and download folders
				var item = _table.ItemsToDisplay.Single(x => x.Path == selectedPath);
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
				// multiple selection - only delete and download are options
				TableContextItems.Single(x => x.Text == "Open").IsVisible = false;
				TableContextItems.Single(x => x.Text == "Rename").IsVisible = false;

				// check can delete all selection?
				if (IsValidSelection())
				{
					// also check for directory
					if(_table.ItemsToDisplay.Any(x => _table.Selection.Contains(x.Path) && x.EntryType == FileExplorerItemType.Directory))
					{
						TableContextItems.Single(x => x.Text == "Download").IsDisabled = true;
					}
				}
				else
				{
					TableContextItems.Single(x => x.Text == "Delete").IsDisabled = true;
					TableContextItems.Single(x => x.Text == "Download").IsDisabled = true;
				}
			}

			// allow application to alter table context menu state
			await UpdateTableContextState.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnTableContextMenuItemClickAsync(MenuItem menuItem)
		{
			if (_table is null)
			{
				throw new InvalidOperationException("_table is null");
			}

			// notify application and allow cancel
			var args = new MenuItemEventArgs(_table, menuItem);
			await TableContextMenuClick.InvokeAsync(args).ConfigureAwait(true);

			if (_table!.Selection.Count > 0)
			{
				if (!args.Cancel)
				{
					var path = _table!.Selection[0];
					if (menuItem.Text == "Open")
					{
						await NavigateFolderAsync(path).ConfigureAwait(true);
					}
					else if (menuItem.Text == "Delete")
					{
						await DeleteFilesAsync().ConfigureAwait(true);
					}
					else if(menuItem.Text == "Rename")
					{
						await _table!.BeginEditAsync().ConfigureAwait(true);
					}
					else if(menuItem.Text == "Download")
					{
						foreach (var selectedPath in _table!.Selection)
						{
							var item = _table.ItemsToDisplay.Find(x => x.Path == selectedPath);
							if (item != null)
							{
								await TableDownloadRequest.InvokeAsync(new TableEventArgs<FileExplorerItem>(item)).ConfigureAwait(true);
							}
						}
					}
				}
			}
		}

		private async Task OnTableSelectionChangedAsync()
		{
			await RefreshToolbarAsync().ConfigureAwait(true);
			var selection = _table?.GetSelectedItems() ?? new FileExplorerItem[0];
			await SelectionChanged.InvokeAsync(selection).ConfigureAwait(true);
		}

		private async Task DirectoryRenameAsync(string oldPath, string newPath)
		{
			// synchronize existing node paths for tree and table
			_tree!.RootNode.Walk((x) =>
			{
				if (x.Data != null)
				{
					x.Key = x.Key.ReplacePathPrefix(oldPath, newPath);
					x.Data.Path = x.Data.Path.ReplacePathPrefix(oldPath, newPath);
				}
				return true;
			});
			_table!.ItemsToDisplay.ToList().ForEach(x => x.Path = x.Path.ReplacePathPrefix(oldPath, newPath));
			if (_tree?.SelectedNode != null)
			{
				await OnTreeSelectionChangeAsync(_tree.SelectedNode).ConfigureAwait(true);
			}
		}

		private async Task OnFilesDroppedAsync(DropZoneEventArgs args)
		{
			if (_tree?.SelectedNode?.Data != null)
			{
				// notify application and allow for cancel
				await UploadRequest.InvokeAsync(args).ConfigureAwait(true);

				// check for conflicts
				var targetPath = _tree.SelectedNode.Data.Path;
				var filenames = args.Files.Select(x => x.Name ?? string.Empty).ToArray();
				var conflicts = await GetConflictsAsync(targetPath, filenames).ConfigureAwait(true);
				if(conflicts.Count > 0)
				{
					var choice = await PromptUserForConflictResolution(conflicts.Select(x => x.Name).ToArray()).ConfigureAwait(true);
					if (choice == "Cancel")
					{
						args.Cancel = true;
						args.CancelReason = "User canceled";
						return;
					}
					else
					{
						foreach (var conflict in conflicts)
						{
							if (choice == "Skip")
							{
								var item = Array.Find(args.Files, x => x.Name == conflict.Name);
								if (item != null)
								{
									item.Skip = true;
								}
							}
							else
							{
								// need to delete from server first to avoid conflict
								await DataProvider.DeleteAsync(conflict, CancellationToken.None).ConfigureAwait(true);
							}
						}
					}
				}

				// add current path so it can be passed along with uploads
				args.State = targetPath;
			}
		}

		private async Task OnUploadStartedAsync(DropZoneUploadEventArgs args)
		{
			await UploadStarted.InvokeAsync(args).ConfigureAwait(false);
		}

		private async Task OnUploadProgressAsync(DropZoneUploadProgressEventArgs args)
		{
			if(_table is null)
			{
				return;
			}

			// is the upload happening to the current path?
			if(args.Path == FolderPath)
			{
				// add virtual file item
				var item = _table.ItemsToDisplay.Find(x => x.Name == args.Name);
				if (item is null)
				{
					item = new FileExplorerItem
					{
						Path = $"{args.Path}/{args.Name}",
						DateCreated = DateTimeOffset.Now,
						DateModified = DateTimeOffset.Now,
						EntryType = FileExplorerItemType.File,
						FileSize = args.Size,
						IsUploading = true
					};
					_table.ItemsToDisplay.Add(item);
				}
				item.UploadProgress = args.Progress;
			}

			await UploadProgress.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnUploadCompletedAsync(DropZoneUploadEventArgs args)
		{
			// is the upload happening to the current path?
			if (args.Path == FolderPath)
			{
				// add virtual file item
				var item = _table?.ItemsToDisplay.Find(x => x.Name == args.Name);
				if(item != null)
				{
					item.IsUploading = false;
				}
			}

			await UploadCompleted.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnToolbarButtonClickAsync(string key)
		{
			switch(key)
			{
				case "navigate-up":
					await NavigateUpAsync().ConfigureAwait(true);
					break;

				case "create-folder":
					await CreateNewFolderAsync().ConfigureAwait(true);
					break;

				case "delete":
					await DeleteFilesAsync().ConfigureAwait(true);
					break;

				default:
					await ToolbarClick.InvokeAsync(key).ConfigureAwait(true);
					break;
			}
		}

		private async Task OnDropAsync(DropEventArgs args)
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

				// move items into folder
				await MoveCopyFilesAsync(payload, targetPath, args.Ctrl).ConfigureAwait(true);
			}
		}

		protected override void OnAfterRender(bool firstRender)
		{
			if (firstRender)
			{
				_deleteDialog.Buttons.Clear();
				_deleteDialog.Buttons.AddRange(new[]
				{
					new ToolbarButton { Text = "Yes", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-check" },
					new ToolbarButton { Text = "No", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" },
				});

				// add third button needed for conflict resolution
				_conflictDialog.Buttons.Clear();
				_conflictDialog.Buttons.AddRange(new[]
				{
					new ToolbarButton { Text = "Overwrite", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-save" },
					new ToolbarButton { Text = "Skip", CssClass = "btn-primary", IconCssClass = "fas fa-fw fa-forward" },
					new ToolbarButton { Text = "Cancel", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" }
				});
			}
		}

		private bool IsValidSelection()
		{
			foreach (var path in _table!.Selection)
			{
				// disallow if .. selected
				if (path.EndsWith(".."))
				{
					return false;
				}

				// disallow delete if uploading
				var item = _table.ItemsToDisplay.Single(x => x.Path == path);
				if (item?.IsUploading != false)
				{
					return false;
				}
			}
			return true;
		}

		private async Task NavigateFolderAsync(string path)
		{
			if (Path.GetFileName(path) == "..")
			{
				if (_selectedNode?.ParentNode != null)
				{
					await _tree!.SelectNode(_selectedNode.ParentNode).ConfigureAwait(true);
				}
			}
			else
			{
				if (_selectedNode?.IsExpanded == false)
				{
					await _tree!.ToggleNodeIsExpandedAsync(_selectedNode).ConfigureAwait(true);
				}
				var node = _tree!.RootNode.Find(path);
				if (node != null)
				{
					await _tree!.SelectNode(node).ConfigureAwait(true);
				}
			}
		}

		private async Task NavigateUpAsync()
		{
			await NavigateFolderAsync("..").ConfigureAwait(true);
		}

		private async Task CreateNewFolderAsync()
		{
			if (_tree?.SelectedNode?.Data != null)
			{
				var newFolderName = _tree.SelectedNode.MakeUniqueText("New Folder");
				var newPath = $"{_tree.SelectedNode.Data.Path.TrimEnd('/')}/{newFolderName}";
				var newItem = new FileExplorerItem { EntryType = FileExplorerItemType.Directory, Path = newPath };
				var result = await DataProvider.CreateAsync(newItem, CancellationToken.None).ConfigureAwait(true);
				if (result.Success)
				{
					// refresh current node, select new node and finally begin edit mode
					await _tree.RefreshNodeAsync(_tree.SelectedNode).ConfigureAwait(true);
					var newNode = _tree.RootNode.Find(newItem.Path);
					if (newNode != null)
					{
						await _tree.SelectNode(newNode).ConfigureAwait(true);
						await _tree.BeginEdit().ConfigureAwait(true);
					}
				}
			}
		}

		private async Task DeleteFolderAsync()
		{
			if (_selectedNode?.Data != null)
			{
				var deleteArgs = new DeleteArgs
				{
					Items = new[] { _selectedNode.Data },
					Resolution = DeleteArgs.DeleteResolutions.Prompt
				};

				await DeleteRequest.InvokeAsync(deleteArgs).ConfigureAwait(true);

				if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Prompt)
				{
					_deleteDialogMessage = $"Are you sure you wish to delete '{deleteArgs.Items[0].Name}'?";
					StateHasChanged();
					var choice = await _deleteDialog.ShowAndWaitResultAsync().ConfigureAwait(true);
					deleteArgs.Resolution = choice == "Yes" ? DeleteArgs.DeleteResolutions.Delete : DeleteArgs.DeleteResolutions.Cancel;
				}

				if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Delete && deleteArgs.Items.Length > 0)
				{
					try
					{
						await DataProvider.DeleteAsync(deleteArgs.Items[0], CancellationToken.None).ConfigureAwait(true);
						if (_tree?.SelectedNode != null)
						{
							await _tree.RemoveNodeAsync(_tree.SelectedNode).ConfigureAwait(true);
						}
					}
					catch (Exception ex)
					{
						await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
					}
				}
			}
		}

		private async Task DeleteFilesAsync()
		{
			if (_table?.Selection != null)
			{
				var deleteArgs = new DeleteArgs
				{
					Items = _table.GetSelectedItems(),
					Resolution = DeleteArgs.DeleteResolutions.Prompt
				};

				await DeleteRequest.InvokeAsync(deleteArgs).ConfigureAwait(true);

				if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Prompt)
				{
					_deleteDialogMessage = deleteArgs.Items.Length == 1
						? $"Are you sure you wish to delete '{deleteArgs.Items[0].Name}'?"
						: $"Are you sure you wish to delete these {deleteArgs.Items.Length} items?";
					StateHasChanged();
					var choice = await _deleteDialog.ShowAndWaitResultAsync();
					deleteArgs.Resolution = choice == "Yes" ? DeleteArgs.DeleteResolutions.Delete : DeleteArgs.DeleteResolutions.Cancel;
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
							await ExceptionHandler.InvokeAsync(ex).ConfigureAwait(true);
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
			// check for conflicts - top level only
			var conflictArgs = new MoveCopyArgs
			{
				Payload = payload,
				TargetPath = targetPath,
				IsCopy = isCopy,
				ConflictResolution = MoveCopyArgs.ConflictResolutions.Prompt,
				Conflicts = await GetConflictsAsync(targetPath, payload.Select(x => x.Name).ToArray()).ConfigureAwait(true)
			};
			if(conflictArgs.Conflicts.Count > 0)
			{
				// allow application to process conflicts
				await MoveCopyConflict.InvokeAsync(conflictArgs).ConfigureAwait(true);

				if(conflictArgs.ConflictResolution == MoveCopyArgs.ConflictResolutions.Prompt)
				{
					var choice = await PromptUserForConflictResolution(conflictArgs.Conflicts.Select(x => x.Name).ToArray()).ConfigureAwait(true);
					conflictArgs.ConflictResolution = choice == "Overwrite" ? MoveCopyArgs.ConflictResolutions.Overwrite
						: choice == "Skip" ? MoveCopyArgs.ConflictResolutions.Skip
						: MoveCopyArgs.ConflictResolutions.Cancel;
				}
			}

			if (conflictArgs.Conflicts.Count == 0 || conflictArgs.ConflictResolution != MoveCopyArgs.ConflictResolutions.Cancel)
			{
				foreach (var source in conflictArgs.Payload)
				{
					// delete conflicting target file?
					if(conflictArgs.Conflicts.Any(x => x.Name == source.Name) && conflictArgs.ConflictResolution == MoveCopyArgs.ConflictResolutions.Overwrite)
					{
						var target = new FileExplorerItem { EntryType = source.EntryType, Path = $"{conflictArgs.TargetPath}/{source.Name}" };
						var result = await DataProvider.DeleteAsync(target, CancellationToken.None).ConfigureAwait(true);
					}

					// move or copy entry if no conflict or overwrite chosen
					if (!conflictArgs.Conflicts.Any(x => x.Name == source.Name) || conflictArgs.ConflictResolution == MoveCopyArgs.ConflictResolutions.Overwrite)
					{
						var delta = new Dictionary<string, object>
						{
							{  "Path", conflictArgs.TargetPath },
							{  "Copy", conflictArgs.IsCopy }
						};

						var result = await DataProvider.UpdateAsync(source, delta, CancellationToken.None).ConfigureAwait(true);
					}
				}
				if (_table != null && !conflictArgs.IsCopy)
				{
					await _table.RefreshAsync().ConfigureAwait(true);
					if (_tree?.SelectedNode != null)
					{
						await _tree.RefreshNodeAsync(_tree.SelectedNode).ConfigureAwait(true);
					}
				}
			}
		}

		/// <summary>
		/// Forces the tree component of the file explorer to be refreshed.
		/// </summary>
		public async Task RefreshTreeAsync()
		{
			// refresh tree - parent node will already be selected
			var node = _tree?.SelectedNode;
			if (node != null)
			{
				node.Nodes = null;
				await _tree!.RefreshNodeAsync(node)!.ConfigureAwait(true);
			}
		}

		/// <summary>
		/// Forces the table component of the file explorer to be refreshed.
		/// </summary>
		public async Task RefreshTableAsync()
		{
			if(_table is null)
			{
				throw new InvalidOperationException("_table should not be null.");
			}

			_table.Selection.Clear();

			// explicitly state search path else fetch will use previous value as OnParametersSet not yet called
			await _table
				.RefreshAsync(FolderPath)
				.ConfigureAwait(true);
			StateHasChanged();
		}

		/// <summary>
		/// Forces the toolbar component of the file explorer to be refreshed.
		/// </summary>
		public async Task RefreshToolbarAsync()
		{
			// up button
			var upButton = ToolbarItems.Find(x => x.Key == "navigate-up");
			if (upButton != null)
			{
				upButton.IsEnabled = _tree?.SelectedNode?.ParentNode != null;
			}

			// create folder button - always enabled by default

			// delete button
			var deleteButton = ToolbarItems.Find(x => x.Key == "delete");
			if (deleteButton != null)
			{
				deleteButton.IsEnabled = _table!.Selection.Count > 0 && !_table!.Selection.Any(x => x.EndsWith(".."));
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
				return _table!.Selection.ToArray();
			}
		}

		private async Task<List<FileExplorerItem>> GetConflictsAsync(string targetPath, IEnumerable<string> names)
		{
			var conflicts = new List<FileExplorerItem>();
			var request = new DataRequest<FileExplorerItem> { SearchText = targetPath };
			var response = await DataProvider.GetDataAsync(request, CancellationToken.None).ConfigureAwait(true);
			foreach (var item in response.Items)
			{
				if (names.Any(x => string.Equals(item.Name, x, StringComparison.OrdinalIgnoreCase)))
				{
					conflicts.Add(item);
				}
			}
			return conflicts;
		}

		private async Task<string> PromptUserForConflictResolution(IEnumerable<string> names)
		{
			var namesSummary = names.Take(5).ToList();
			if (names.Count() > 5)
			{
				namesSummary.Add($"+ {names.Count() - 5} other items");
			}
			_conflictDialogMessage = $"{names.Count()} conflicts found : -";
			_conflictDialogList = namesSummary.ToArray();
			StateHasChanged();
			return await _conflictDialog.ShowAndWaitResultAsync();
		}
	}
}