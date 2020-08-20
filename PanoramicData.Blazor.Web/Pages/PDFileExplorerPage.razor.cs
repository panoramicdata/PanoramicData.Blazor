﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Web.Data;

namespace PanoramicData.Blazor.Web.Pages
{
	public partial class PDFileExplorerPage
    {
		private IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider { RootFolder = "My Computer" };
		private PDFileExplorer _fileExplorer = null!;

		/// <summary>
		/// Injected javascript interop object.
		/// </summary>
		[Inject] public IJSRuntime? JSRuntime { get; set; }

		/// <summary>
		/// Injected navigation manager.
		/// </summary>
		[Inject] protected NavigationManager NavigationManager { get; set; } = null!;

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

		public async Task OnUploadCompleted(DropZoneUploadEventArgs args)
		{
			// need to add to data provider as file not really uploaded to physical drive
			await _dataProvider.CreateAsync(new FileExplorerItem
			{
				DateCreated = DateTimeOffset.Now,
				DateModified = DateTimeOffset.Now,
				EntryType = FileExplorerItemType.File,
				FileSize = args.Size,
				Path = $"{args.Path}{Path.DirectorySeparatorChar}{args.Name}"
			}, CancellationToken.None).ConfigureAwait(true);
		}

		public async Task OnUpdateToolbarState(List<ToolbarItem> items)
		{
			// add custom toolbar button
			var createFileButton = items.Find(x => x.Key == "create-file");
			if(createFileButton == null)
			{
				// not existing - so create
				items.Insert(2, new ToolbarButton { Key = "create-file", Text = "New File", ToolTip = "Create a new file", IconCssClass = "fas fa-fw fa-file-medical" });
			}
		}

		public async Task OnToolbarClick(string key)
		{
			// example of a custom action
			if(key == "create-file")
			{
				await CreateFile().ConfigureAwait(true);
			}
		}

		public async Task OnUpdateTableContextMenuState(MenuItemsEventArgs args)
		{
			// add custom toolbar button
			var createFileButton = args.MenuItems.Find(x => x.Key == "create-file");
			if (createFileButton == null)
			{
				// not existing - so create
				createFileButton = new MenuItem { Key = "create-file", Text = "New File", IconCssClass = "fas fa-fw fa-file-medical" };
				args.MenuItems.Insert(2, createFileButton);
			}

			// update custom item state - enabled only when no selection
			if(_fileExplorer.SelectedFilesAndFolders.Length == 0)
			{
				createFileButton.IsDisabled = false;
				args.Cancel = false;
			}
			else
			{
				createFileButton.IsDisabled = true;
			}
		}

		public async Task OnTreeContextMenuClick(MenuItemEventArgs args)
		{
			if (args.MenuItem.Text == "Delete")
			{
				// prompt user to confirm action
			}
		}

		public async Task OnTableContextMenuClick(MenuItemEventArgs args)
		{
			if (args.MenuItem.Text == "Delete")
			{
				// prompt user to confirm action
			}
			else if(args.MenuItem.Key == "create-file")
			{
				await CreateFile().ConfigureAwait(true);
			}
		}

		private async Task CreateFile()
		{
			var newPath = $"{_fileExplorer.FolderPath}{Path.DirectorySeparatorChar}{DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")}.txt";

			var result = await _dataProvider.CreateAsync(new FileExplorerItem
			{
				 EntryType = FileExplorerItemType.File,
				 Path = newPath,
				 FileSize = 100,
				 DateCreated = DateTimeOffset.UtcNow,
				 DateModified = DateTimeOffset.UtcNow
			}, CancellationToken.None).ConfigureAwait(true);

			if(result.Success)
			{
				await _fileExplorer.RefreshTable().ConfigureAwait(true);
				await _fileExplorer.RefreshToolbar().ConfigureAwait(true);
			}
		}
	}
}
