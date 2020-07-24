using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;

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
				await _tree!.SelectItemAsync(node!.Data);
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
				await _table!.RefreshAsync();
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

		private void OnTreeContextMenuItemClick(MenuItem item)
		{
		}

		private void OnTableItemsLoaded(List<FileExplorerItem> items)
		{
			items.Insert(0, new FileExplorerItem { Path = "..", ParentPath = _selectedNode?.Data?.Path ?? "", EntryType = FileExplorerItemType.Directory });
		}

		private async Task OnTableDoubleClick(FileExplorerItem item)
		{
			if (item.EntryType == FileExplorerItemType.Directory)
			{
				await OpenFolder(item.Path).ConfigureAwait(true);
			}
		}

		private async Task OpenFolder(string path)
		{
			if (path == "..")
			{
				if (_selectedNode?.ParentNode != null)
				{
					var parentPath = _selectedNode?.ParentNode!.Data?.Path;
					if (parentPath != null)
					{
						await _tree!.SelectItemAsync(parentPath).ConfigureAwait(true);
					}
				}
			}
			else
			{
				if (_selectedNode?.IsExpanded == false)
				{
					await _tree!.ToggleNodeIsExpandedAsync(_selectedNode).ConfigureAwait(true);
				}
				await _tree!.SelectItemAsync(path).ConfigureAwait(true);
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

		private async Task OnTableContextMenuItemClick(MenuItem item)
		{
			if (_table!.Selection.Count == 1)
			{
				if (item.Text == "Open")
				{
					await OpenFolder(_table!.Selection[0]).ConfigureAwait(true);
				}
			}
		}
	}
}
