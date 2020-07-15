using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using System.Threading.Tasks;

namespace PanoramicData.Blazor
{
	public partial class PDFileExplorer
    {
		private PDTree<FileExplorerItem> _tree;
		private PDTable<FileExplorerItem> _table;

		public string FolderPath = "NULL";

		/// <summary>
		/// Gets or sets the IDataProviderService instance to use to fetch data.
		/// </summary>
		[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

		public async Task OnTreeSelectionChange(FileExplorerItem item)
		{
			await SelectFolder(item.Path).ConfigureAwait(true);
		}

		public async Task SelectFolder(string path)
		{
			// update tree

			// update table
			FolderPath = path;
			//await _table.RefreshAsync().ConfigureAwait(true);
			//StateHasChanged();
		}
	}
}
