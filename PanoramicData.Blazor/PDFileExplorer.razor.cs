using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor
{
	public partial class PDFileExplorer
    {
		private PDTree<FileExplorerItem>? _tree;
		private PDTable<FileExplorerItem>? _table;

		public string FolderPath = "NULL"; // display 'no data' until first selection

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

		public void OnTreeSelectionChange(FileExplorerItem item)
		{
			SelectFolder(item.Path);
		}

		public void SelectFolder(string path)
		{
			// update table - set the SearchText to the parent path
			FolderPath = path;
			_table.Selection.Clear();
		}
	}
}
