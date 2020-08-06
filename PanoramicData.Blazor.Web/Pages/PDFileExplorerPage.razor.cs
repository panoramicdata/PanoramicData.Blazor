using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Web.Data;
using System.Linq;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDFileExplorerPage
    {
		private IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider { RootFolder = "My Computer" };

		/// <summary>
		/// Injected javascript interop object.
		/// </summary>
		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// Injected navigation manager.
		/// </summary>
		[Inject] protected NavigationManager NavigationManager { get; set; } = null!;

		public async Task OnTreeContextMenuClick(MenuItemEventArgs args)
		{
			//var tree = (PDTree<FileExplorerItem>)args.Sender;
			if (args.MenuItem.Text == "Delete")
			{
				// prompt user to confirm action
			}
		}

		public void OnTableContextMenuClick(MenuItemEventArgs args)
		{
			if (args.MenuItem.Text == "Delete")
			{
				// prompt user to confirm action
			}
		}

		public async Task OnTableDownloadRequest(TableEventArgs<FileExplorerItem> args)
		{
			// Method A: this method works up to file sizes of 125MB - limit imposed by System.Text.Json (04/08/20)
			//var bytes = System.IO.File.ReadAllBytes("Download/file_example_WEBM_1920_3_7MB.webm");
			//var base64 = System.Convert.ToBase64String(bytes);
			//await JSRuntime.InvokeVoidAsync("downloadFile", $"{System.IO.Path.GetFileNameWithoutExtension(args.Item.Name)}.webm", base64).ConfigureAwait(true);

			// Method B: to avoid size limit and conversion to base64 - redirect to controller method
			NavigationManager.NavigateTo($"/files/download?path={args.Item.Path}", true);
		}

		public async Task OnUploadRequest(DropZoneEventArgs args)
		{
			if(args.Files.Any(x => x.Size > 1000000000)) // 1GB
			{
				args.Cancel = true;
				args.CancelReason = "Upload limit is 1GB per file";
			}
		}
	}
}
