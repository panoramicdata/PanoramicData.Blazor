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
		}

		private async Task OnTreeItemsLoaded(IEnumerable<FileExplorerItem> items)
		{
			if(items.Any() && _selectedNode == null)
			{
				await _tree!.SelectItemAsync(items.First());
				StateHasChanged();
			}
		}

		public void OnTreeSelectionChange(TreeNode<FileExplorerItem> node)
		{
			_selectedNode = node;
			if (node != null && node.Data != null)
			{
				SelectFolder(node.Data.Path);
			}
		}

		public void SelectFolder(string path)
		{
			// update table - set the SearchText to the parent path
			FolderPath = path;
			_table!.Selection.Clear();
		}

		public void OnTreeBeforeShowContextMenu(CancelEventArgs args)
		{
			if (_selectedNode?.Data != null)
			{
				_treeContextItems.Single(x => x.Text == "Delete").IsDisabled = _selectedNode.Data.ParentPath == string.Empty;
				_treeContextItems.Single(x => x.Text == "Rename").IsDisabled = _selectedNode.Data.ParentPath == string.Empty;
			}
		}

		public void OnTreeContextMenuItemClick(MenuItem item)
		{
		}
	}
}
