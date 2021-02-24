using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDFileExplorer : IDisposable
	{
		private static int _idSequence;
		private string _deleteDialogMessage = string.Empty;
		private string _conflictDialogMessage = string.Empty;
		private string[] _conflictDialogList = new string[0];
		private readonly SortCriteria _tableSort = new SortCriteria("Name", SortDirection.Ascending);
		private readonly MenuItem _menuOpen = new MenuItem { Text = "Open", IconCssClass = "fas fa-fw fa-folder-open" };
		private readonly MenuItem _menuDownload = new MenuItem { Text = "Download", IconCssClass = "fas fa-fw fa-file-download" };
		private readonly MenuItem _menuNewFolder = new MenuItem { Text = "New Folder", IconCssClass = "fas fa-fw fa-plus" };
		private readonly MenuItem _menuUploadFiles = new MenuItem { Text = "Upload Files", IconCssClass = "fas fa-fw fa-upload" };
		private readonly MenuItem _menuSep1 = new MenuItem { IsSeparator = true };
		private readonly MenuItem _menuRename = new MenuItem { Text = "Rename", IconCssClass = "fas fa-fw fa-pencil-alt" };
		private readonly MenuItem _menuSep2 = new MenuItem { IsSeparator = true };
		private readonly MenuItem _menuCopy = new MenuItem { Text = "Copy", IconCssClass = "fas fa-fw fa-copy" };
		private readonly MenuItem _menuCut = new MenuItem { Text = "Cut", IconCssClass = "fas fa-fw fa-cut" };
		private readonly MenuItem _menuPaste = new MenuItem { Text = "Paste", IconCssClass = "fas fa-fw fa-paste" };
		private readonly MenuItem _menuSep3 = new MenuItem { IsSeparator = true };
		private readonly MenuItem _menuDelete = new MenuItem { Text = "Delete", IconCssClass = "fas fa-fw fa-trash-alt" };
		private readonly List<FileExplorerItem> _copyPayload = new List<FileExplorerItem>();
		private bool _moveCopyPayload = false;
		private TreeNode<FileExplorerItem>? _selectedNode;
		private PDTree<FileExplorerItem>? Tree { get; set; }
		private PDTable<FileExplorerItem>? Table { get; set; }
		private PDModal? DeleteDialog { get; set; } = null!;
		private PDModal? ConflictDialog { get; set; } = null!;
		private PDModal UploadDialog { get; set; } = null!;
		private PDDropZone DialogDropZone { get; set; } = null!;

		public string FolderPath = string.Empty;
		public string Id { get; private set; } = string.Empty;

		/// <summary>
		/// Determines whether the user may drag items.
		/// </summary>
		[Parameter] public bool AllowDrag { get; set; } = true;

		/// <summary>
		/// Determines whether the user may drop dragged items onto other items.
		/// </summary>
		[Parameter] public bool AllowDrop { get; set; } = true;

		/// <summary>
		/// Determines whether the user may rename items.
		/// </summary>
		[Parameter] public bool AllowRename { get; set; } = true;

		/// <summary>
		/// Determines whether the first node is automatically expanded on load.
		/// </summary>
		[Parameter] public bool AutoExpand { get; set; } = false;

		/// <summary>
		/// Gets or sets a delegate to be called before an item is renamed.
		/// </summary>
		[Parameter] public EventCallback<RenameArgs> BeforeRename { get; set; }

		/// <summary>
		/// Sets the Table column configuration.
		/// </summary>
		[Parameter]
		public List<PDColumnConfig> ColumnConfig { get; set; } = new List<PDColumnConfig>
			{
				new PDColumnConfig { Id = "Icon", Title = "" },
				new PDColumnConfig { Id = "Name", Title = "Name" },
				new PDColumnConfig { Id = "Type", Title = "Type" },
				new PDColumnConfig { Id = "Size", Title = "Size" },
				new PDColumnConfig { Id = "Modified", Title = "Modified" }
			};


		/// <summary>
		/// Determines the action taken when copying conflicting named items into a folder.
		/// </summary>
		[Parameter] public ConflictResolutions ConflictResolution { get; set; } = ConflictResolutions.Prompt;

		/// <summary>
		/// Gets or sets CSS classes to append.
		/// </summary>
		[Parameter] public string CssClass { get; set; } = string.Empty;

		/// <summary>
		/// Sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

		/// <summary>
		/// Event called whenever the user requests to delete one or more items.
		/// </summary>
		[Parameter] public EventCallback<DeleteArgs> DeleteRequest { get; set; }

		/// <summary>
		/// Gets or sets a delegate to be called if an exception occurs.
		/// </summary>
		[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

		/// <summary>
		/// Provides a function that determines the icon CSS class for a given file extension.
		/// </summary>
		[Parameter] public Func<FileExplorerItem, string>? GetIconClass { get; set; }

		/// <summary>
		/// Determines whether folders are always grouped together and shown first.
		/// </summary>
		[Parameter] public bool GroupFolders { get; set; } = true;

		/// <summary>
		/// Event raised whenever the user double clicks on a file.
		/// </summary>
		[Parameter] public EventCallback<FileExplorerItem> ItemDoubleClick { get; set; }

		[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

		/// <summary>
		/// Event called whenever a move or copy operation is subject to conflicts.
		/// </summary>
		[Parameter] public EventCallback<MoveCopyArgs> MoveCopyConflict { get; set; }

		/// <summary>
		/// Determines where sub-folders show an entry (..) to allow navigation to the parent folder.
		/// </summary>
		[Parameter] public bool ShowParentFolder { get; set; } = true;

		/// <summary>
		/// Determines whether the toolbar is visible.
		/// </summary>
		[Parameter] public bool ShowToolbar { get; set; } = true;

		/// <summary>
		/// Event raises whenever the selection changes.
		/// </summary>
		[Parameter] public EventCallback<FileExplorerItem[]> SelectionChanged { get; set; }

		/// <summary>
		/// Sets the allowed selection modes.
		/// </summary>
		[Parameter] public TableSelectionMode SelectionMode { get; set; } = TableSelectionMode.Multiple;

		/// <summary>
		/// Determines whether the context menu is available.
		/// </summary>
		[Parameter] public bool ShowContextMenu { get; set; } = true;

		/// <summary>
		/// Event raised whenever the user clicks on a context menu item from the table.
		/// </summary>
		[Parameter] public EventCallback<MenuItemEventArgs> TableContextMenuClick { get; set; }

		/// <summary>
		/// Sets the Table context menu items.
		/// </summary>
		[Parameter]
		public List<MenuItem> TableContextItems { get; set; } = new List<MenuItem>();

		/// <summary>
		/// Event raised when user requests to download one or more files.
		/// </summary>
		[Parameter] public EventCallback<TableSelectionEventArgs<FileExplorerItem>> TableDownloadRequest { get; set; }

		/// <summary>
		/// Event raised whenever the user clicks on a toolbar button.
		/// </summary>
		[Parameter] public EventCallback<string> ToolbarClick { get; set; }

		/// <summary>
		/// Sets the Table context menu items.
		/// </summary>
		[Parameter]
		public List<ToolbarItem> ToolbarItems { get; set; } = new List<ToolbarItem>
			{
				new ToolbarButton { Key = "navigate-up", ToolTip="Navigate up to parent folder", IconCssClass="fas fa-fw fa-arrow-up",  CssClass="btn-secondary" },
				new ToolbarButton { Key = "create-folder", Text = "New Folder", ToolTip="Create a new folder", IconCssClass="fas fa-fw fa-folder-plus", CssClass="btn-secondary" },
				new ToolbarButton { Key = "upload", Text = "Upload", ToolTip="Upload one or more files", IconCssClass="fas fa-fw fa-upload", CssClass="btn-secondary" },
				new ToolbarButton { Key = "delete", Text = "Delete", ToolTip="Delete the selected files and folders", IconCssClass="fas fa-fw fa-trash-alt", CssClass="btn-danger", ShiftRight = true }
			};

		/// <summary>
		/// Event raised whenever the user clicks on a context menu item from the tree.
		/// </summary>
		[Parameter] public EventCallback<MenuItemEventArgs> TreeContextMenuClick { get; set; }

		/// <summary>
		/// Sets the Tree context menu items.
		/// </summary>
		[Parameter] public List<MenuItem> TreeContextItems { get; set; } = new List<MenuItem>();

		/// <summary>
		/// Optional sort function to use on sibling tree nodes.
		/// </summary>
		[Parameter] public Comparison<FileExplorerItem>? TreeSort { get; set; }

		/// <summary>
		/// Event raised whenever a file upload completes.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadEventArgs> UploadCompleted { get; set; }

		/// <summary>
		/// Event raised periodically during a file upload.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadProgressEventArgs> UploadProgress { get; set; }

		/// <summary>
		/// Event raised whenever the user drops one or more files on to the file explorer.
		/// </summary>
		[Parameter] public EventCallback<DropZoneEventArgs> UploadRequest { get; set; }

		/// <summary>
		/// Event raised whenever a file upload starts.
		/// </summary>
		[Parameter] public EventCallback<DropZoneUploadEventArgs> UploadStarted { get; set; }

		/// <summary>
		/// URL where files are uploaded.
		/// </summary>
		[Parameter] public string? UploadUrl { get; set; }

		/// <summary>
		/// Event raised whenever the table context menu may need updating.
		/// </summary>
		[Parameter] public EventCallback<MenuItemsEventArgs> UpdateTableContextState { get; set; }

		/// <summary>
		/// Event raised whenever the toolbar may need updating.
		/// </summary>
		[Parameter] public EventCallback<List<ToolbarItem>> UpdateToolbarState { get; set; }

		/// <summary>
		/// Event raised whenever the tree context menu may need updating.
		/// </summary>
		[Parameter] public EventCallback<MenuItemsEventArgs> UpdateTreeContextState { get; set; }

		protected override void OnInitialized()
		{
			Id = $"pdfe{++_idSequence}";
			TableContextItems.AddRange(new[]
				{
				_menuOpen,
				_menuDownload,
				_menuNewFolder,
				_menuSep1,
				_menuRename,
				_menuSep2,
				_menuCopy,
				_menuCut,
				_menuPaste,
				_menuSep3,
				_menuDelete
			});
			TreeContextItems.AddRange(new[]
			{
				_menuRename,
				_menuNewFolder,
				_menuUploadFiles,
				_menuSep2,
				_menuCopy,
				_menuCut,
				_menuPaste,
				_menuSep3,
				_menuDelete
			});
		}

		/// <summary>
		/// Gets or sets file items.
		/// </summary>
		public FileExplorerItem[]? FileItems
		{
			get
			{
				return Table?.ItemsToDisplay.ToArray();
			}
		}

		/// <summary>
		/// Filters file items out of tree and shows root items in table on tree first load.
		/// </summary>
		private void OnTreeItemsLoaded(List<FileExplorerItem> items)
		{
			items.RemoveAll(x => x.EntryType == FileExplorerItemType.File);
		}

		private async Task OnTreeNodeUpdatedAsync(TreeNode<FileExplorerItem> node)
		{
			// auto expand first node?
			if (AutoExpand && node.Text == "Root" && node.Nodes?.Count == 1)
			{
				var firstNode = node.Nodes[0];
				await Tree!.SelectNode(firstNode).ConfigureAwait(true);
				await Tree.RefreshNodeAsync(firstNode).ConfigureAwait(true);
				StateHasChanged();
			}
		}

		private async Task OnTreeSelectionChangeAsync(TreeNode<FileExplorerItem> node)
		{
			_selectedNode = node;
			if (node?.Data != null && FolderPath != node.Data.Path)
			{
				FolderPath = node.Data.Path;
				await RefreshTableAsync().ConfigureAwait(true);
				await RefreshToolbarAsync().ConfigureAwait(true);
			}
		}

		private async Task OnTreeContextMenuUpdateStateAsync(MenuItemsEventArgs args)
		{
			var parentPath = _selectedNode?.Data?.ParentPath ?? string.Empty;
			var selectedPath = _selectedNode?.Data?.Path ?? string.Empty;
			var isRoot = string.IsNullOrEmpty(parentPath);
			var folderSelected = !string.IsNullOrWhiteSpace(selectedPath);

			_menuNewFolder.IsVisible = folderSelected;
			_menuUploadFiles.IsVisible = folderSelected;
			_menuRename.IsVisible = folderSelected && !isRoot;
			_menuDelete.IsVisible = folderSelected && !isRoot;
			_menuCopy.IsVisible = folderSelected && !isRoot;
			_menuCut.IsVisible = folderSelected && !isRoot;
			_menuPaste.IsVisible = folderSelected && _copyPayload.Count > 0;
			_menuSep3.IsVisible = _menuSep2.IsVisible = _menuSep1.IsVisible = true;
			_menuSep3.IsVisible = ShowSeparator(_menuSep3, TreeContextItems);
			_menuSep2.IsVisible = ShowSeparator(_menuSep2, TreeContextItems);
			_menuSep1.IsVisible = ShowSeparator(_menuSep1, TreeContextItems);

			// allow application to alter tree context menu state
			await UpdateTreeContextState.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnTreeContextMenuItemClickAsync(MenuItem item)
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
						await UploadDialog.ShowAsync().ConfigureAwait(true);
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
		}

		private async Task OnTreeKeyDownAsync(KeyboardEventArgs args)
		{
			if (Tree?.SelectedNode?.IsEditing != true)
			{
				if (args.Code == "Delete")
				{
					await DeleteFolderAsync().ConfigureAwait(true);
				}
				else if ((args.Code == "KeyC" || args.Code == "KeyX") && args.CtrlKey && Tree!.SelectedNode?.Data != null)
				{
					_copyPayload.Clear();
					_copyPayload.Add(Tree!.SelectedNode.Data);
					_moveCopyPayload = args.Code == "KeyX";
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
			if (args.Node.Data != null)
			{
				var renameArgs = new RenameArgs { Item = args.Node.Data };
				await BeforeRename.InvokeAsync(renameArgs).ConfigureAwait(true);
				args.Cancel = renameArgs.Cancel;
			}
			if (!AllowRename || args.Node.ParentNode == null)
			{
				args.Cancel = true;
			}
		}

		private async Task OnTreeAfterEditAsync(TreeNodeAfterEditEventArgs<FileExplorerItem> args)
		{
			if (Tree?.SelectedNode?.Data != null)
			{
				var item = Tree.SelectedNode.Data;
				var previousPath = item.Path;
				var newPath = $"{item.ParentPath.TrimEnd('/')}/{args.NewValue}";
				// inform data provider
				var delta = new Dictionary<string, object>
				{
					{  "Path", newPath }
				};
				var result = await DataProvider.UpdateAsync(Tree.SelectedNode.Data, delta, CancellationToken.None).ConfigureAwait(true);
				if (result.Success)
				{
					// synchronize existing node paths for tree and table
					Tree.RootNode.Walk((x) =>
					{
						if (x.Data != null)
						{
							x.Key = x.Key.ReplacePathPrefix(previousPath, newPath);
							x.Data.Path = x.Data.Path.ReplacePathPrefix(previousPath, newPath);
						}
						return true;
					});
					Table!.ItemsToDisplay.ToList().ForEach(x => x.Path = x.Path.ReplacePathPrefix(previousPath, newPath));
					await OnTreeSelectionChangeAsync(Tree.SelectedNode).ConfigureAwait(true);
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

			if (GroupFolders)
			{
				var folders = items.Where(x => x.EntryType == FileExplorerItemType.Directory).ToList();
				var files = items.Where(x => x.EntryType == FileExplorerItemType.File).ToList();
				items.Clear();
				items.AddRange(folders);
				items.AddRange(files);
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
				if (renameArgs.Cancel || !AllowRename || args.Item.Name == ".." || args.Item.IsUploading)
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
			// cancel is new name is empty
			if (args.NewValues.ContainsKey("Name"))
			{
				if (string.IsNullOrWhiteSpace(args.NewValues["Name"]?.ToString()))
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
								await DirectoryRenameAsync(previousPath, newPath).ConfigureAwait(true);
							}
						}

						// replace selection with new path
						Table.Selection.Clear();
						Table.Selection.Add(newPath);
					}
				}
			}
		}

		private bool ShowSeparator(MenuItem separator, IEnumerable<MenuItem> items)
		{
			var visibleItems = items.Where(x => x.IsVisible).ToList();
			if (visibleItems.Count == 0 || separator == visibleItems[0] || separator == visibleItems[visibleItems.Count - 1])
			{
				return false;
			}
			var idx = visibleItems.IndexOf(separator);
			return idx <= 0 || !visibleItems[idx - 1].IsSeparator;
		}

		private async Task OnTableContextMenuUpdateStateAsync(MenuItemsEventArgs args)
		{
			var selectedItems = Table!.GetSelectedItems();
			var validSelection = IsValidSelection();

			_menuOpen.IsVisible = selectedItems.Length == 1 && selectedItems[0].EntryType == FileExplorerItemType.Directory;
			_menuDownload.IsVisible = validSelection && selectedItems.Length > 0 && selectedItems.All(x => x.EntryType == FileExplorerItemType.File);
			_menuNewFolder.IsVisible = false;
			_menuRename.IsVisible = validSelection && selectedItems.Length == 1;
			_menuDelete.IsVisible = validSelection && selectedItems.Length > 0;
			_menuCopy.IsVisible = validSelection && selectedItems.Length > 0;
			_menuCut.IsVisible = validSelection && selectedItems.Length > 0;
			_menuPaste.IsVisible = validSelection && _copyPayload.Count > 0 && (selectedItems.Length == 0 || (selectedItems.Length == 1 && selectedItems[0].EntryType == FileExplorerItemType.Directory));
			_menuSep3.IsVisible = _menuSep2.IsVisible = _menuSep1.IsVisible = true;
			_menuSep3.IsVisible = ShowSeparator(_menuSep3, TableContextItems);
			_menuSep2.IsVisible = ShowSeparator(_menuSep2, TableContextItems);
			_menuSep1.IsVisible = ShowSeparator(_menuSep1, TableContextItems);

			// allow application to alter table context menu state
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
					var targetPath = selection.Length == 1 && selection[0].EntryType == FileExplorerItemType.Directory ? selection[0].Path : FolderPath;
					await MoveCopyFilesAsync(_copyPayload, targetPath, !_moveCopyPayload).ConfigureAwait(true);
					if (_moveCopyPayload) // clear copy payload only if move
					{
						_copyPayload.Clear();
					}
				}
				else if (menuItem.Text == "New Folder")
				{
					await CreateNewFolderAsync().ConfigureAwait(true);
				}
			}
		}

		private async Task OnTableSelectionChangedAsync()
		{
			await RefreshToolbarAsync().ConfigureAwait(true);
			var selection = Table?.GetSelectedItems() ?? new FileExplorerItem[0];
			await SelectionChanged.InvokeAsync(selection).ConfigureAwait(true);
		}

		private async Task DirectoryRenameAsync(string oldPath, string newPath)
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
				await OnTreeSelectionChangeAsync(Tree.SelectedNode).ConfigureAwait(true);
			}
		}

		private async Task OnFilesDroppedAsync(DropZoneEventArgs args)
		{
			// cancel upload dialog if shown
			await UploadDialog.HideAsync().ConfigureAwait(true);

			if (Tree?.SelectedNode?.Data != null)
			{
				// notify application and allow for cancel
				await UploadRequest.InvokeAsync(args).ConfigureAwait(true);

				// check for conflicts
				var moveCopyArgs = new MoveCopyArgs
				{
					TargetPath = Tree.SelectedNode.Data.Path,
					Payload = args.Files.Select(x => new FileExplorerItem { Path = x.Path ?? string.Empty }).ToList()
				};
				await GetConflictsAsync(moveCopyArgs).ConfigureAwait(true);
				if (moveCopyArgs.Conflicts.Count > 0)
				{
					var choice = await PromptUserForConflictResolution(moveCopyArgs.Conflicts.Select(x => x.Name).ToArray(), true).ConfigureAwait(true);
					if (choice == ConflictResolutions.Cancel)
					{
						args.Cancel = true;
						args.CancelReason = "User canceled";
						return;
					}
					else
					{
						foreach (var conflict in moveCopyArgs.Conflicts)
						{
							if (choice == ConflictResolutions.Skip)
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
				args.State = moveCopyArgs.TargetPath;
			}
		}

		private async Task OnUploadStartedAsync(DropZoneUploadEventArgs args)
		{
			await UploadStarted.InvokeAsync(args).ConfigureAwait(false);
		}

		private async Task OnUploadProgressAsync(DropZoneUploadProgressEventArgs args)
		{
			if (Table is null)
			{
				return;
			}

			// is the upload happening to the current path?
			if (args.Path == FolderPath)
			{
				// add virtual file item
				var item = Table.ItemsToDisplay.Find(x => x.Name == args.Name);
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
					Table.ItemsToDisplay.Add(item);
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
				var item = Table?.ItemsToDisplay.Find(x => x.Name == args.Name);
				if (item != null)
				{
					item.IsUploading = false;
				}
			}

			await UploadCompleted.InvokeAsync(args).ConfigureAwait(true);
		}

		private async Task OnToolbarButtonClickAsync(string key)
		{
			switch (key)
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

				case "upload":
					await UploadDialog.ShowAsync().ConfigureAwait(true);
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
			if (args.Target is FileExplorerItem target && target.EntryType == FileExplorerItemType.Directory)
			{
				List<FileExplorerItem> payload = new List<FileExplorerItem>();
				if (args.Payload is List<FileExplorerItem> mfe)
				{
					payload = mfe;
				}
				else if (args.Payload is FileExplorerItem sfe)
				{
					payload.Add(sfe);
				}

				// check not dropping an item onto itself
				if (payload.Any(x => x.Path == target.Path))
				{
					return;
				}

				// check can move/copy all items
				if (payload.Any(x => !x.CanCopyMove))
				{
					return;
				}

				// moving item to parent folder?
				var targetPath = target.Path;
				if (target.Name == "..")
				{
					targetPath = Path.GetDirectoryName(target.ParentPath);
				}

				// move items into folder
				await MoveCopyFilesAsync(payload, targetPath, args.Ctrl).ConfigureAwait(true);
			}
		}

		protected async override Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				// initialize file select
				//_dotNetReference = DotNetObjectReference.Create(this);
				await JSRuntime.InvokeVoidAsync("panoramicData.initializeFileSelect", $"{Id}-file-select", DialogDropZone.Id).ConfigureAwait(false);

				if (DeleteDialog != null)
				{
					DeleteDialog.Buttons.Clear();
					DeleteDialog.Buttons.AddRange(new[]
					{
						new ToolbarButton { Key="yes", Text = "Yes", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-check", ShiftRight = true },
						new ToolbarButton { Key="no", Text = "No", CssClass = "btn-primary", IconCssClass = "fas fa-fw fa-times" },
					});
				}

				// add third button needed for conflict resolution
				if (ConflictDialog != null)
				{
					ConflictDialog.Buttons.Clear();
					ConflictDialog.Buttons.AddRange(new[]
					{
						new ToolbarButton { Text = "Overwrite", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-save" },
						new ToolbarButton { Text = "Rename", CssClass = "btn-primary", IconCssClass = "fas fa-fw fa-pen-square", ShiftRight = true },
						new ToolbarButton { Text = "Skip", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-forward" },
						new ToolbarButton { Text = "Cancel", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" }
					});
				}

				// set up buttons on upload dialog
				if (UploadDialog != null)
				{
					UploadDialog!.Buttons.First(x => x.Key == "Yes").IsVisible = false;
					if (UploadDialog!.Buttons.First(x => x.Key == "No") is ToolbarButton btn)
					{
						btn.ShiftRight = true;
						btn.Text = "Cancel";
						btn.CssClass = "btn-primary";
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

		private async Task NavigateFolderAsync(string path)
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

		private async Task NavigateUpAsync()
		{
			await NavigateFolderAsync("..").ConfigureAwait(true);
		}

		private async Task CreateNewFolderAsync()
		{
			if (Tree?.SelectedNode?.Data != null)
			{
				var newFolderName = Tree.SelectedNode.MakeUniqueText("New Folder");
				var newPath = $"{Tree.SelectedNode.Data.Path.TrimEnd('/')}/{newFolderName}";
				var newItem = new FileExplorerItem { EntryType = FileExplorerItemType.Directory, Path = newPath, HasSubFolders = false };
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

		private async Task DeleteFolderAsync()
		{
			if (_selectedNode?.Data != null && DeleteDialog != null)
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
					var choice = await DeleteDialog.ShowAndWaitResultAsync().ConfigureAwait(true);
					deleteArgs.Resolution = choice == "yes" ? DeleteArgs.DeleteResolutions.Delete : DeleteArgs.DeleteResolutions.Cancel;
				}

				if (deleteArgs.Resolution == DeleteArgs.DeleteResolutions.Delete && deleteArgs.Items.Length > 0)
				{
					try
					{
						await DataProvider.DeleteAsync(deleteArgs.Items[0], CancellationToken.None).ConfigureAwait(true);
						if (Tree?.SelectedNode != null)
						{
							await Tree.RemoveNodeAsync(Tree.SelectedNode).ConfigureAwait(true);
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
			await GetConflictsAsync(conflictArgs).ConfigureAwait(true);

			if (conflictArgs.Conflicts.Count > 0)
			{
				// allow application to process conflicts
				await MoveCopyConflict.InvokeAsync(conflictArgs).ConfigureAwait(true);

				// if any source and target path are the same then user is copy/moving from same folder - so hide overwrite option
				var showOverwrite = !conflictArgs.Payload.Any(x => conflictArgs.Conflicts.Any(y => y.Path == x.Path));

				if (conflictArgs.ConflictResolution == ConflictResolutions.Prompt)
				{
					conflictArgs.ConflictResolution = await PromptUserForConflictResolution(conflictArgs.Conflicts.Select(x => x.Name).ToArray(), showOverwrite).ConfigureAwait(true);
				}
			}

			if (conflictArgs.Conflicts.Count == 0 || conflictArgs.ConflictResolution != ConflictResolutions.Cancel)
			{
				foreach (var source in conflictArgs.Payload)
				{
					if (conflictArgs.ConflictResolution == ConflictResolutions.Rename)
					{
						// get a unique name
						var newPath = $"{conflictArgs.TargetPath.TrimEnd('/')}/{GetUniqueName(source, conflictArgs.TargetItems)}";
						var delta = new Dictionary<string, object>
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
								await ExceptionHandler.InvokeAsync(new Exception("Operation Failed: Source and Destination are the same")).ConfigureAwait(true);
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
							var delta = new Dictionary<string, object>
							{
								{  "Path", newPath },
								{  "Copy", conflictArgs.IsCopy }
							};
							var result = await DataProvider.UpdateAsync(source, delta, CancellationToken.None).ConfigureAwait(true);
						}
					}
				}

				// refresh the current table view
				await Table!.RefreshAsync().ConfigureAwait(true);

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

		private string GetUniqueName(FileExplorerItem source, List<FileExplorerItem> targetItems)
		{
			var count = 1;
			var newName = PostFixFilename(source.Name, " Copy");
			while (targetItems.Any(x => x.Name == newName))
			{
				newName = PostFixFilename(source.Name, $" Copy {count++}");
			}
			return newName;
		}

		private string PostFixFilename(string filename, string postfix)
		{
			if (string.IsNullOrWhiteSpace(filename))
			{
				return postfix.Trim();
			}
			var idx = filename.IndexOf('.');
			if (idx == -1)
			{
				return $"{filename}{postfix}";
			}
			return $"{filename.Substring(0, idx)}{postfix}{filename.Substring(idx)}";
		}

		/// <summary>
		/// Forces the tree component of the file explorer to be refreshed.
		/// </summary>
		public async Task RefreshTreeAsync()
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
		public async Task RefreshTableAsync()
		{
			if (Table is null)
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
		public async Task RefreshToolbarAsync()
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

		/// <summary>
		/// Populates the move copy arguments with conflicting items.
		/// </summary>
		private async Task GetConflictsAsync(MoveCopyArgs args)
		{
			var conflicts = new List<FileExplorerItem>();
			var names = args.Payload.Select(x => x.Name).ToArray();
			var request = new DataRequest<FileExplorerItem> { SearchText = args.TargetPath };
			var response = await DataProvider.GetDataAsync(request, CancellationToken.None).ConfigureAwait(true);
			args.TargetItems = response.Items.ToList();
			foreach (var item in response.Items)
			{
				if (names.Any(x => string.Equals(item.Name, x, StringComparison.OrdinalIgnoreCase)))
				{
					conflicts.Add(item);
				}
			}
			args.Conflicts = conflicts;
		}

		private async Task<ConflictResolutions> PromptUserForConflictResolution(IEnumerable<string> names, bool showOverwrite)
		{
			var namesSummary = names.Take(5).ToList();
			if (names.Count() > 5)
			{
				namesSummary.Add($"+ {names.Count() - 5} other items");
			}
			_conflictDialogMessage = $"{names.Count()} conflicts found : -";
			_conflictDialogList = namesSummary.ToArray();
			ConflictDialog!.Buttons.Find(x => x.Key == "Overwrite").IsVisible = showOverwrite;
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

		private string GetIconCssClass(FileExplorerItem? item)
		{
			if (item != null)
			{
				var cssClass = GetIconClass is null ? null : GetIconClass(item);
				if (cssClass is null)
				{
					return item.EntryType == FileExplorerItemType.File ? "far fa-fw fa-file" : "far fa-fw fa-folder";
				}
				if (cssClass.Length == 0)
				{
					return "far fa-fw fa-hidden fa-file";
				}
				return cssClass;
			}
			return string.Empty;
		}

		private int OnTreeSort(FileExplorerItem item1, FileExplorerItem item2)
		{
			if (TreeSort is null)
			{
				return item1.Name.CompareTo(item2.Name);
			}
			else
			{
				return TreeSort(item1, item2);
			}
		}

		public void Dispose()
		{
			JSRuntime.InvokeVoidAsync("panoramicData.disposeFileSelect", $"{Id}-file-select");
		}
	}
}