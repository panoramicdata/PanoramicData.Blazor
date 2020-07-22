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
		private List<MenuItem> _treeContextItems = new List<MenuItem>();
		private List<MenuItem> _tableContextItems = new List<MenuItem>();
		private TreeNode<FileExplorerItem>? _selectedNode;
		private PDTable<FileExplorerItem>? _table;

		public string FolderPath = ""; // display 'no data' until first selection

		/// <summary>
		/// Gets or sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

		public List<PDColumnConfig> ColumnConfig = new List<PDColumnConfig>
			{
				new PDColumnConfig { Id = "Icon", Title = "" },
				new PDColumnConfig { Id = "Name", Title = "Name" },
				new PDColumnConfig { Id = "Type", Title = "Type" },
				new PDColumnConfig { Id = "Size", Title = "Size" },
				//new PDColumnConfig { Id = "Created", Title = "Created" },
				new PDColumnConfig { Id = "Modified", Title = "Modified" }
			};

		protected override void OnInitialized()
		{
			_treeContextItems.Add(new MenuItem { Text = "Rename", IconCssClass = "fas fa-pencil-alt" });
			_treeContextItems.Add(new MenuItem { Text = "New Folder", IconCssClass = "fas fa-plus" });
			_treeContextItems.Add(new MenuItem { IsSeparator = true });
			_treeContextItems.Add(new MenuItem { Text = "Delete", IconCssClass = "fas fa-trash-alt" });

			_tableContextItems.Add(new MenuItem { Text = "Open", IconCssClass = "fas fa-folder-open" });
			_tableContextItems.Add(new MenuItem { Text = "Download", IconCssClass = "fas fa-file-download" });
			_tableContextItems.Add(new MenuItem { IsSeparator = true });
			_tableContextItems.Add(new MenuItem { Text = "Rename", IconCssClass = "fas fa-pencil-alt" });
			//_tableContextItems.Add(new MenuItem { Text = "New Folder", IconCssClass = "fas fa-plus" });
			_tableContextItems.Add(new MenuItem { IsSeparator = true });
			_tableContextItems.Add(new MenuItem { Text = "Delete", IconCssClass = "fas fa-trash-alt" });
		}

		public void SelectFolder(string path)
		{
			// update table - set the SearchText to the parent path
			FolderPath = path;
			_table!.Selection.Clear();
		}

		private void OnTreeItemsLoaded(List<FileExplorerItem> items)
		{
			items.RemoveAll(x => x.EntryType == FileExplorerItemType.File);
		}

		private async Task OnTreeNodeUpdated(TreeNode<FileExplorerItem> node)
		{
			if (node?.Data != null && node?.ParentNode == null) // root node updated
			{
				await _tree!.SelectItemAsync(node!.Data);
				StateHasChanged();
			}
		}

		private void OnTreeSelectionChange(TreeNode<FileExplorerItem> node)
		{
			_selectedNode = node;
			if (node != null && node.Data != null)
			{
				SelectFolder(node.Data.Path);
			}
		}

		private void OnTreeBeforeShowContextMenu(CancelEventArgs args)
		{
			if (_selectedNode?.Data != null)
			{
				_treeContextItems.Single(x => x.Text == "Delete").IsDisabled = _selectedNode.Data.ParentPath == string.Empty;
				_treeContextItems.Single(x => x.Text == "Rename").IsDisabled = _selectedNode.Data.ParentPath == string.Empty;
			}
		}

		private void OnTreeContextMenuItemClick(MenuItem item)
		{
		}

		private void OnTableItemsLoaded(List<FileExplorerItem> items)
		{
			if (_selectedNode != null && _selectedNode.ParentNode != null && _selectedNode?.Data != null)
			{
				items.Insert(0, new FileExplorerItem { Path = "..", ParentPath = _selectedNode!.Data!.Path, EntryType = FileExplorerItemType.Directory });
			}
		}

		private async Task OnTableDoubleClick(FileExplorerItem item)
		{
			if (_selectedNode != null && _tree != null && item.EntryType == FileExplorerItemType.Directory)
			{
				if (item.Path == "..")
				{
					var parentPath = _selectedNode?.ParentNode!.Data?.Path;
					if (parentPath != null)
					{
						await _tree.SelectItemAsync(parentPath);
					}
				}
				else
				{
					if (!_selectedNode.IsExpanded)
					{
						await _tree!.ToggleNodeIsExpandedAsync(_selectedNode);
					}
					await _tree!.SelectItemAsync(item.Path);
				}
			}
		}

		private void OnTableSelectionChange()
		{
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
				_tableContextItems.Single(x => x.Text == "Download").IsVisible = false;
				_tableContextItems.ForEach(x => x.IsDisabled = x.Text == "Open" ? false : true);
				return;
			}
			var item = _table.ItemsToDisplay.Single(x => x.Path == selectedPath);
			if(item == null)
			{
				args.Cancel = true;
				return;
			}
			_tableContextItems.ForEach(x => x.IsDisabled = false);
			if(item.EntryType == FileExplorerItemType.Directory)
			{
				_tableContextItems.Single(x => x.Text == "Open").IsVisible = true;
				_tableContextItems.Single(x => x.Text == "Download").IsVisible = false;
			}
			else
			{
				_tableContextItems.Single(x => x.Text == "Download").IsVisible = true;
				_tableContextItems.Single(x => x.Text == "Open").IsVisible = false;
			}
		}

		private void OnTableContextMenuItemClick(MenuItem item)
		{
		}
	}
}
