using System.IO;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Extensions;
using System.Runtime.CompilerServices;
using Microsoft.JSInterop;
using System;

namespace PanoramicData.Blazor
{
	public partial class PDFileExplorer
    {
		private PDTree<FileExplorerItem>? _tree;
		private PDTable<FileExplorerItem>? _table;
		private TreeNode<FileExplorerItem>? _selectedNode;
		private bool _firstLoad = true;
		public string FolderPath = "";

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
				new MenuItem { Text = "Rename", IconCssClass = "fas fa-pencil-alt" },
				new MenuItem { Text = "New Folder", IconCssClass = "fas fa-plus" },
				new MenuItem { IsSeparator = true },
				new MenuItem { Text = "Delete", IconCssClass = "fas fa-trash-alt" }
			};

		/// <summary>
		/// Sets the Table context menu items.
		/// </summary>
		[Parameter]
		public List<MenuItem> TableContextItems { get; set; } = new List<MenuItem>
			{
				new MenuItem { Text = "Open", IconCssClass = "fas fa-folder-open" },
				new MenuItem { Text = "Download", IconCssClass = "fas fa-file-download" },
				new MenuItem { IsSeparator = true },
				new MenuItem { Text = "Rename", IconCssClass = "fas fa-pencil-alt" },
				new MenuItem { IsSeparator = true },
				new MenuItem { Text = "Delete", IconCssClass = "fas fa-trash-alt" }
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
		/// Filters file items out of tree and shows root items in table on tree first load.
		/// </summary>
		private async Task OnTreeItemsLoaded(List<FileExplorerItem> items)
		{
		 	items.RemoveAll(x => x.EntryType == FileExplorerItemType.File);
			if(_firstLoad)
			{
				_firstLoad = false;
				// should be single root item
				if (items.Count > 0)
				{
					FolderPath = items[0].Path;
					await _table!.RefreshAsync();
				}
			}
		}

		private async Task OnTreeNodeUpdated(TreeNode<FileExplorerItem> node)
		{
			if (node?.Data != null && node?.ParentNode == null) // root node updated
			{
				await _tree!.SelectNode(node!).ConfigureAwait(true);
				StateHasChanged();
			}
		}

		private async Task OnTreeSelectionChange(TreeNode<FileExplorerItem> node)
		{
			_selectedNode = node;
			if (node != null && node.Data != null)
			{
				FolderPath = node.Data.Path;
				_table!.Selection.Clear();
				await _table!.RefreshAsync(node.Data.Path); // explicitly state search path else fetch will use previous value as OnParametersSet not yet called
				StateHasChanged();
			}
		}

		private void OnTreeBeforeShowContextMenu(CancelEventArgs args)
		{
			if (_selectedNode?.Data != null)
			{
				TreeContextItems.Single(x => x.Text == "Delete").IsDisabled = _selectedNode.Data.ParentPath == string.Empty;
				TreeContextItems.Single(x => x.Text == "Rename").IsDisabled = _selectedNode.Data.ParentPath == string.Empty;
			}
		}

		private async Task OnTreeContextMenuItemClick(MenuItem item)
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
						var result = await DataProvider.DeleteAsync(_tree.SelectedNode.Data, CancellationToken.None).ConfigureAwait(true);
						if (result.Success)
						{
							await _tree.RemoveNodeAsync(_tree.SelectedNode).ConfigureAwait(true);
						}
					}
					else if (item.Text == "Rename")
					{
						await _tree.BeginEdit().ConfigureAwait(true);
					}
					else if (item.Text == "New Folder")
					{
						var newFolderName = _tree.SelectedNode.MakeUniqueText("New Folder");
						var newPath = $"{_tree.SelectedNode.Data.Path}{Path.DirectorySeparatorChar}{newFolderName}";
						var newItem = new FileExplorerItem { EntryType = FileExplorerItemType.Directory, Path = newPath };
						var result = await DataProvider.CreateAsync(newItem, CancellationToken.None).ConfigureAwait(true);
						if(result.Success)
						{
							// refresh current node, select new node and finally begin edit mode
							await _tree.RefreshNodeAsync(_tree.SelectedNode).ConfigureAwait(true);
							var newNode = _tree.RootNode.Find(newItem.Path);
							if(newNode != null)
							{
								await _tree.SelectNode(newNode).ConfigureAwait(true);
								await _tree.BeginEdit().ConfigureAwait(true);
							}
						}
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
			if (_tree?.SelectedNode?.Data != null)
			{
				var item = _tree.SelectedNode.Data;
				var previousPath = item.Path;
				var newPath = $"{item.ParentPath}{Path.DirectorySeparatorChar}{args.NewValue}";
				// inform data provider
				var result = await DataProvider.UpdateAsync(_tree.SelectedNode.Data, new { Path = newPath }, CancellationToken.None).ConfigureAwait(true);
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
					_table!.ItemsToDisplay.ToList().ForEach(x =>
					{
						x.Path = x.Path.ReplacePathPrefix(previousPath, newPath);
					});
					await OnTreeSelectionChange(_tree.SelectedNode).ConfigureAwait(true);
				}
			}
		}

		private void OnTableItemsLoaded(List<FileExplorerItem> items)
		{
			items.Insert(0, new FileExplorerItem { Path = Path.Combine(_selectedNode?.Data?.Path ?? "", ".."), EntryType = FileExplorerItemType.Directory });
		}

		private async Task OnTableDoubleClick(FileExplorerItem item)
		{
			if (!_table!.IsEditing && item.EntryType == FileExplorerItemType.Directory)
			{
				await OpenFolder(item.Path).ConfigureAwait(true);
			}
		}

		private void OnTableBeforeEdit(TableBeforeEditEventArgs<FileExplorerItem> args)
		{
			if(args.Item.Name == "..")
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
					var newPath = $"{args.Item.ParentPath}{Path.DirectorySeparatorChar}{args.NewValues["Name"]}";
					// check for duplicate name
					if (_table!.ItemsToDisplay.Any(x => x.Path == newPath))
					{
						args.Cancel = true;
					}
					else
					{
						// inform data provider
						var result = await DataProvider.UpdateAsync(args.Item, new { Path = newPath }, CancellationToken.None).ConfigureAwait(true);
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

		private async Task OpenFolder(string path)
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

		private void OnTableBeforeShowContextMenu(CancelEventArgs args)
		{
			if(_table!.Selection.Count == 0)
			{
				args.Cancel = true;
				return;
			}
			var selectedPath = _table!.Selection[0];
			if(selectedPath == "..")
			{
				TableContextItems.Single(x => x.Text == "Download").IsVisible = false;
				TableContextItems.ForEach(x => x.IsDisabled = x.Text != "Open");
				return;
			}
			var item = _table.ItemsToDisplay.Single(x => x.Path == selectedPath);
			if(item == null)
			{
				args.Cancel = true;
				return;
			}
			TableContextItems.ForEach(x => x.IsDisabled = false);
			if(item.EntryType == FileExplorerItemType.Directory)
			{
				TableContextItems.Single(x => x.Text == "Open").IsVisible = true;
				TableContextItems.Single(x => x.Text == "Download").IsVisible = false;
			}
			else
			{
				TableContextItems.Single(x => x.Text == "Download").IsVisible = true;
				TableContextItems.Single(x => x.Text == "Open").IsVisible = false;
			}
		}

		private async Task OnTableContextMenuItemClick(MenuItem menuItem)
		{
			if (_table!.Selection.Count == 1)
			{
				// notify application and allow cancel
				var args = new MenuItemEventArgs(_table, menuItem);
				await TableContextMenuClick.InvokeAsync(args).ConfigureAwait(true);
				if (!args.Cancel)
				{
					var path = _table!.Selection[0];
					if (menuItem.Text == "Open")
					{
						await OpenFolder(path).ConfigureAwait(true);
					}
					else if (menuItem.Text == "Delete")
					{
						// selection key is path of item
						var fileItem = _table.ItemsToDisplay.FirstOrDefault(x => x.Path == path);
						if (fileItem != null)
						{
							var result = await DataProvider.DeleteAsync(fileItem, CancellationToken.None);
							if (result.Success)
							{
								// refresh tree - parent node will already be selected
								var node = _tree!.RootNode.Find(fileItem.ParentPath);
								if (node != null)
								{
									await _tree!.RefreshNodeAsync(node)!.ConfigureAwait(true);
								}
							}
						}
					}
					else if(menuItem.Text == "Rename")
					{
						await _table!.BeginEdit().ConfigureAwait(true);
					}
					else if(menuItem.Text == "Download")
					{
						var item = _table.ItemsToDisplay.FirstOrDefault(x => x.Path == _table!.Selection[0]);
						await TableDownloadRequest.InvokeAsync(new TableEventArgs<FileExplorerItem>(item)).ConfigureAwait(true);
					}
				}
			}
		}

		private async Task DirectoryRename(string oldPath, string newPath)
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
			_table!.ItemsToDisplay.ToList().ForEach(x =>
			{
				x.Path = x.Path.ReplacePathPrefix(oldPath, newPath);
			});
			if (_tree?.SelectedNode != null)
			{
				await OnTreeSelectionChange(_tree.SelectedNode).ConfigureAwait(true);
			}
		}
	}
}
