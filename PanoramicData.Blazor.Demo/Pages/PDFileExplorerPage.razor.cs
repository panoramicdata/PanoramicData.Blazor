using System.IO;

namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFileExplorerPage
{
	private string? _deepLinkPath;
	private readonly string[] _virtualFolders = new string[] { "/Library", "/Users" };
	private readonly IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
	private IPreviewProvider? _previewProvider;

	private PDFileExplorer? FileExplorer { get; set; }

	/// <summary>
	/// Injected javascript interop object.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	/// <summary>
	/// Injected navigation manager.
	/// </summary>
	[Inject] protected NavigationManager NavigationManager { get; set; } = null!;

	[CascadingParameter] protected EventManager? EventManager { get; set; }

	private string GetDownloadUrl(FileExplorerItem item)
	{
		if (item.EntryType == FileExplorerItemType.File)
		{
			// in real world scenarios  you would calculate the download url as <mime>:<filename>:<url>
			//return $"application/octet-stream:{item.Name}:" + NavigationManager.ToAbsoluteUri($"/files/Download?path={item.Path}").ToString();

			// in this demo the content is either a webm or markdown reference file
			if (Path.GetExtension(item.Path) == ".md")
			{
				return $"text/markdown:{item.Path}:" + NavigationManager.ToAbsoluteUri($"/files/Download?path={item.Path}").ToString();
			}

			return $"application/octet-stream:{Path.ChangeExtension(item.Name, ".webm")}:" + NavigationManager.ToAbsoluteUri($"/files/Download?path={item.Path}").ToString();
		}

		return string.Empty;
	}

	protected override void OnInitialized()
	{
		var uri = new Uri(NavigationManager.Uri);
		var query = QueryHelpers.ParseQuery(uri.Query);
		if (query.TryGetValue("path", out var pathQueryStrings) && pathQueryStrings.Count > 0)
		{
			_deepLinkPath = pathQueryStrings[0];
		}
	}

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender && FileExplorer != null)
		{
			_previewProvider = new DemoPreviewProvider() { FileExplorer = FileExplorer };
		}
	}

	private void OnFolderChanged(FileExplorerItem folder) => NavigationManager.SetUri(new Dictionary<string, object> { { "path", $"{folder.Path}" } });

	private async Task OnReady()
	{
		if (!string.IsNullOrWhiteSpace(_deepLinkPath))
		{
			await FileExplorer!.NavigateToAsync(_deepLinkPath).ConfigureAwait(true);
			_deepLinkPath = string.Empty;
		}
	}

	public async Task OnTableDownloadRequest(TableSelectionEventArgs<FileExplorerItem> args)
	{
		// Method A: this method works up to file sizes of 125MB - limit imposed by System.Text.Json (04/08/20)
		//var bytes = System.IO.File.ReadAllBytes("Download/TestVideo.webm");
		//var base64 = System.Convert.ToBase64String(bytes);
		//await JSRuntime.InvokeVoidAsync("downloadFile", $"{System.IO.Path.GetFileNameWithoutExtension(args.Item.Name)}.webm", base64).ConfigureAwait(true);

		// Method B: to avoid size limit and conversion to base64 - use javascript to get from controller method

		// demo downloads the either MD or WEBM file
		foreach (var item in args.Items)
		{
			if (Path.GetExtension(item.Path) != ".md")
			{
				item.Name = Path.ChangeExtension(item.Name, ".webm");
			}
		}

		await JSRuntime.InvokeVoidAsync("panoramicDataDemo.downloadFiles", args).ConfigureAwait(false);
	}

	public static void OnUploadRequest(DropZoneEventArgs args)
	{
		// example of canceling an upload request
		if (args.Files.Any(x => System.IO.Path.GetExtension(x.Name) == ".zip"))
		{
			args.Cancel = true;
			args.CancelReason = "ZIP Archive files can not be uploaded.";
		}
	}

	public void OnUploadStarted(DropZoneUploadEventArgs args)
	{
		// add example additional field to pass with upload
		if (FileExplorer != null)
		{
			args.FormFields.Add("sessionId", FileExplorer.SessionId);
		}
	}

	public async Task OnUploadCompleted(DropZoneUploadCompletedEventArgs args)
	{
		// need to add to data provider as file not really uploaded to physical drive
		if (args.Success)
		{
			await _dataProvider.CreateAsync(new FileExplorerItem
			{
				DateCreated = DateTimeOffset.Now,
				DateModified = DateTimeOffset.Now,
				EntryType = FileExplorerItemType.File,
				FileSize = args.Size,
				Name = args.Name,
				Path = $"{args.Path.TrimEnd('/')}/{args.Name}"
			}, CancellationToken.None).ConfigureAwait(true);
		}
	}

	public void OnUpdateToolbarState(List<ToolbarItem> items)
	{
		// add custom toolbar button - if not already created
		var createFileButton = items.Find(x => x.Key == "create-file");
		if (createFileButton == null)
		{
			items.Insert(3, new ToolbarSeparator());
			items.Insert(4, new ToolbarButton
			{
				Key = "create-file",
				Text = "New File",
				ToolTip = "Create a new file",
				CssClass = "btn-secondary",
				IconCssClass = "fas fa-fw fa-file-medical",
				TextCssClass = "d-none d-lg-inline"
			});
		}
		else
		{
			// update state - can only create file if folder allows items to be added
			createFileButton.IsEnabled = FileExplorer!.GetTreeSelectedFolder()?.CanAddItems == true;
		}
	}

	public async Task OnToolbarClick(string key)
	{
		// example of a custom action
		if (key == "create-file")
		{
			await CreateFile().ConfigureAwait(true);
		}
	}

	public void OnUpdateTableContextMenuState(MenuItemsEventArgs args)
	{
		// add custom toolbar button
		var createFileButton = args.MenuItems.Find(x => x.Key == "create-file");
		if (createFileButton == null)
		{
			// not existing - so create
			createFileButton = new MenuItem { Key = "create-file", Text = "New File", IconCssClass = "fas fa-fw fa-file-medical" };
			args.MenuItems.Insert(3, createFileButton);
		}

		// update custom item state - enabled only when no selection and folder allows items to be added
		var folderItem = FileExplorer!.GetTreeSelectedFolder();
		if (FileExplorer?.SelectedFilesAndFolders.Length == 0 && folderItem?.CanAddItems == true)
		{
			createFileButton.IsVisible = true;
			args.Cancel = false;
		}
		else
		{
			createFileButton.IsVisible = false;
		}
	}

	public async Task OnTableContextMenuClick(MenuItemEventArgs args)
	{
		if (args.MenuItem.Key == "create-file")
		{
			await CreateFile().ConfigureAwait(true);
		}
	}

	private async Task CreateFile()
	{
		var newName = $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt";
		var newPath = $"{FileExplorer?.FolderPath}/{newName}";

		var result = await _dataProvider.CreateAsync(new FileExplorerItem
		{
			Name = newName,
			Path = newPath,
			EntryType = FileExplorerItemType.File,
			FileSize = 100,
			DateCreated = DateTimeOffset.UtcNow,
			DateModified = DateTimeOffset.UtcNow
		}, CancellationToken.None).ConfigureAwait(true);

		if (result.Success && FileExplorer != null)
		{
			await FileExplorer.RefreshTableAsync().ConfigureAwait(true);
			await FileExplorer.RefreshToolbarAsync().ConfigureAwait(true);
		}
	}

	private static string GetCssClass(FileExplorerItem _) => string.Empty;

	private static string GetIconCssClass(FileExplorerItem item)
	{
		if (item.EntryType == FileExplorerItemType.Directory && item.Name != "..")
		{
			if (item.Path == "/Sharepoint")
			{
				return "icon-sharepoint";
			}

			if (item.Path == "/Library")
			{
				return "fas fa-book";
			}

			if (item.Path == "/Users")
			{
				return "fas fa-users";
			}

			if (item.Path == "/")
			{
				return "fas fa-server";
			}

			if (item.ParentPath == "/")
			{
				return "fas fa-hdd";
			}

		}

		return TestFileSystemDataProvider.GetIconClass(item);
	}

	private int OnTreeSort(FileExplorerItem item1, FileExplorerItem item2)
	{
		// shift Library folder to top
		if (_virtualFolders.Contains(item1.Path) && !_virtualFolders.Contains(item2.Path))
		{
			return -1;
		}
		else if (!_virtualFolders.Contains(item1.Path) && _virtualFolders.Contains(item2.Path))
		{
			return 1;
		}

		return item1.Name.CompareTo(item2.Name);
	}
}
