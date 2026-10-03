using PanoramicData.Blazor.PreviewProviders;

namespace PanoramicData.Blazor;

/// <summary>
/// File explorer component with tree/table navigation, uploads, preview, and context actions.
/// </summary>
public partial class PDFileExplorer : IAsyncDisposable
{
	private static int _idSequence;
	private string _deleteDialogMessage = string.Empty;
	private string _conflictDialogMessage = string.Empty;
	private string[] _conflictDialogList = [];
	private readonly SortCriteria _tableSort = new("Name", SortDirection.Ascending);
	private readonly MenuItem _menuOpen = new() { Text = "Open", IconCssClass = "fas fa-fw fa-folder-open" };
	private readonly MenuItem _menuDownload = new() { Text = "Download", IconCssClass = "fas fa-fw fa-file-download" };
	private readonly MenuItem _menuNewFolder = new() { Text = "New Folder", IconCssClass = "fas fa-fw fa-plus" };
	private readonly MenuItem _menuUploadFiles = new() { Text = "Upload Files", IconCssClass = "fas fa-fw fa-upload" };
	private readonly MenuItem _menuSep1 = new() { IsSeparator = true };
	private readonly MenuItem _menuRename = new() { Text = "Rename", IconCssClass = "fas fa-fw fa-pencil-alt" };
	private readonly MenuItem _menuSep2 = new() { IsSeparator = true };
	private readonly MenuItem _menuCopy = new() { Text = "Copy", IconCssClass = "fas fa-fw fa-copy" };
	private readonly MenuItem _menuCut = new() { Text = "Cut", IconCssClass = "fas fa-fw fa-cut" };
	private readonly MenuItem _menuPaste = new() { Text = "Paste", IconCssClass = "fas fa-fw fa-paste" };
	private readonly MenuItem _menuSep3 = new() { IsSeparator = true };
	private readonly MenuItem _menuDelete = new() { Text = "Delete", IconCssClass = "fas fa-fw fa-trash-alt" };
	private readonly List<FileExplorerItem> _copyPayload = [];
	private readonly Dictionary<string, CachedResult<Task<DataResponse<FileExplorerItem>>>> _conflictCache = [];
	private readonly List<FileExplorerItem> _conflicts = [];
	private int _batchCount;
	private int _batchProgress;
	private long _batchTotalBytes;
	private long _batchTotalBytesSent;
	private readonly Dictionary<string, double> _batchFiles = [];
	private bool _moveCopyPayload;
	private string _pasteTarget = string.Empty;
	private TreeNode<FileExplorerItem>? _selectedNode;
	private PDTree<FileExplorerItem>? Tree { get; set; }
	private PDTable<FileExplorerItem>? Table { get; set; }
	private PDModal? DeleteDialog { get; set; } = null!;
	private PDModal? ConflictDialog { get; set; } = null!;
	private PDModal? ProgressDialog { get; set; } = null!;
	private PDModal? UploadDialog { get; set; }
	private PDDropZone _dropZone1 = null!;
	private PDDropZone _dropZone2 = null!;
	private IJSObjectReference? _commonModule;
	private ToolbarButton? _previewPanelButton;
	private PDSplitter? _splitter;
	private FileExplorerItem? _previewItem;
	private double[] _lastSplitSizes = [20, 60, 20];

	/// <summary>
	/// Gets or sets the current folder path.
	/// </summary>
	public string FolderPath { get; set; } = string.Empty;

	private int InitialPreviewSize => PreviewPanel == FilePreviewModes.OptionalOff ? 0 : 1;

	private static bool IsParentDirectoryItem(FileExplorerItem item) => item.EntryType == FileExplorerItemType.Directory && item.Name == "..";

	/// <summary>
	/// Gets the unique component identifier.
	/// </summary>
	public string Id { get; private set; } = string.Empty;

	/// <summary>
	/// Gets whether a navigation operation is currently in progress.
	/// </summary>
	public bool IsNavigating { get; private set; }

	/// <summary>
	/// Gets whether the preview panel is currently visible.
	/// </summary>
	public bool PreviewPanelVisible { get; private set; } = true;

	/// <summary>
	/// Gets the logical session identifier used by this component instance.
	/// </summary>
	public string SessionId { get; private set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Returns display text for a file explorer item.
	/// </summary>
	/// <param name="item">Item to render.</param>
	/// <returns>Display name.</returns>
	public static string GetItemDisplayName(FileExplorerItem? item) =>
		item is null
			? string.Empty
			: item.Name;

	/// <summary>
	/// Determines whether the read-only indicator should be shown for an item.
	/// </summary>
	/// <param name="item">Item to evaluate.</param>
	/// <returns>True when read-only indicator should be displayed.</returns>
	public static bool ShouldShowReadOnlyIndicator(FileExplorerItem? item) =>
		item is not null && item.Name != ".." && item.IsReadOnly;

	/// <summary>
	/// Determines whether read-only items should use icon-based indicators.
	/// </summary>
	/// <returns>True when a read-only icon class is configured.</returns>
	public bool UseReadOnlyIcon() => !string.IsNullOrWhiteSpace(ReadOnlyIconClass);


	#region Inject
	/// <summary>
	/// Gets or sets the overlay service used during long-running operations.
	/// </summary>
	[Inject] public IBlockOverlayService BlockOverlayService { get; set; } = null!;

	/// <summary>
	/// Gets or sets JavaScript runtime used by this component.
	/// </summary>
	[Inject] public IJSRuntime JSRuntime { get; set; } = null!;

	#endregion

	#region Parameters

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
	/// Determines whether the to rename items when conflicting with existing items.
	/// </summary>
	[Parameter] public bool AllowRenameConflicts { get; set; } = false;

	/// <summary>
	/// Determines whether the first node is automatically expanded on load.
	/// </summary>
	[Parameter] public bool AutoExpand { get; set; }

	/// <summary>
	/// Gets or sets a delegate to be called before an item is renamed.
	/// </summary>
	[Parameter] public EventCallback<RenameArgs> BeforeRename { get; set; }

	/// <summary>
	/// Gets or sets the button sizes.
	/// </summary>
	[Parameter] public ButtonSizes ButtonSize { get; set; } = ButtonSizes.Medium;

	/// <summary>
	/// Sets the Table column configuration.
	/// </summary>
	[Parameter]
	public List<PDColumnConfig> ColumnConfig { get; set; } =
		[
			new PDColumnConfig { Id = "Icon", Title = "" },
			new PDColumnConfig { Id = "Name", Title = "Name" },
			new PDColumnConfig { Id = "Type", Title = "Type" },
			new PDColumnConfig { Id = "Size", Title = "Size" },
			new PDColumnConfig { Id = "Modified", Title = "Modified" }
		];

	/// <summary>
	/// Determines the action taken when copying conflicting named items into a folder.
	/// </summary>
	[Parameter] public ConflictResolutions ConflictResolution { get; set; } = ConflictResolutions.Prompt;

	/// <summary>
	/// Gets or sets CSS classes to append.
	/// </summary>
	[Parameter] public string CssClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets callback that allows host app to perform custom move or copy operations.
	/// </summary>
	[Parameter] public EventCallback<CustomMoveCopyArgs> CustomMoveCopy { get; set; }

	/// <summary>
	/// Sets the IDataProviderService instance to use to fetch data.
	/// </summary>
	[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

	/// <summary>
	/// Sets the date format.
	/// </summary>
	[Parameter] public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm";

	/// <summary>
	/// Event called whenever the user requests to delete one or more items.
	/// </summary>
	[Parameter] public EventCallback<DeleteArgs> DeleteRequest { get; set; }

	/// <summary>
	/// Function that calculates and returns the download url for the given item.
	/// </summary>
	[Parameter] public Func<FileExplorerItem, string?> DownloadUrlFunc { get; set; } = (_) => null;

	/// <summary>
	/// An optional array of paths to be excluded.
	/// </summary>
	[Parameter] public string[] ExcludedPaths { get; set; } = [];

	/// <summary>
	/// Gets or sets a delegate to be called if an exception occurs.
	/// </summary>
	[Parameter] public EventCallback<Exception> ExceptionHandler { get; set; }

	/// <summary>
	/// Gets or sets an optional semi-colon delimited list of wild card patterns to filter filenames by.
	/// </summary>
	/// <remarks>* matches 0 or more characters and ? matches exactly one character.</remarks>
	[Parameter] public string FilenamePattern { get; set; } = "";

	/// <summary>
	/// Event raised whenever the current folder changes.
	/// </summary>
	[Parameter] public EventCallback<FileExplorerItem> FolderChanged { get; set; }

	/// <summary>
	/// Provides an optional function that allows a bagde icon CSS class to be provided for items.
	/// </summary>
	[Parameter] public Func<FileExplorerItem, IconInfo?>? GetItemBadgeCssClass { get; set; }

	/// <summary>
	/// Provides a function that determines the CSS class for a given item.
	/// </summary>
	[Parameter] public Func<FileExplorerItem, string>? GetItemCssClass { get; set; }

	/// <summary>
	/// Provides a function that determines the icon CSS class for a given item.
	/// </summary>
	[Parameter] public Func<FileExplorerItem, string>? GetItemIconCssClass { get; set; }

	/// <summary>
	/// Determines whether folders are always grouped together and shown first.
	/// </summary>
	[Parameter] public bool GroupFolders { get; set; } = true;

	/// <summary>
	/// Event raised whenever the user double clicks on a file.
	/// </summary>
	[Parameter] public EventCallback<FileExplorerItem> ItemDoubleClick { get; set; }

	/// <summary>
	/// Gets or sets the maximum file upload size in MB.
	/// </summary>
	[Parameter] public int UploadMaxSize { get; set; } = 256;

	/// <summary>
	/// Event called whenever a move or copy operation is subject to conflicts.
	/// </summary>
	[Parameter] public EventCallback<MoveCopyArgs> MoveCopyConflict { get; set; }

	/// <summary>
	/// Gets or sets the default name for new folders.
	/// </summary>
	[Parameter] public string NewFolderName { get; set; } = "New Folder";

	/// <summary>
	/// Gets or sets an optional File Preview provider. Defaults to a <see cref="FileExplorerPreviewProvider"/>
	/// bound to this explorer. A supplied <see cref="FileExplorerPreviewProvider"/> whose
	/// <see cref="FileExplorerPreviewProvider.FileExplorer"/> is not set is bound to this explorer when it
	/// initializes; any other supplied provider is used as given.
	/// </summary>
	[Parameter] public IPreviewProvider PreviewProvider { get; set; } = new FileExplorerPreviewProvider();

	/// <summary>
	/// Preview Panel mode.
	/// </summary>
	[Parameter] public FilePreviewModes PreviewPanel { get; set; } = FilePreviewModes.Off;

	/// <summary>
	/// Gets or sets string to append after a Read-Only items name.
	/// </summary>
	[Parameter] public string ReadOnlyPostfix { get; set; } = "(ro)";

	/// <summary>
	/// Gets or sets the CSS class for the read-only icon (e.g., "fa fa-solid fa-lock").
	/// When set, this icon is used instead of ReadOnlyPostfix text.
	/// </summary>
	[Parameter] public string? ReadOnlyIconClass { get; set; }

	/// <summary>
	/// Gets or sets the position of the read-only indicator relative to the file/folder name.
	/// Default is After for backward compatibility with the text postfix behavior.
	/// Use Before for better visual alignment when multiple items have indicators.
	/// </summary>
	[Parameter] public ReadOnlyIndicatorPosition ReadOnlyIndicatorPosition { get; set; } = ReadOnlyIndicatorPosition.After;

	/// <summary>
	/// Gets or sets an event callback raised when the component has perform all it initialization.
	/// </summary>
	[Parameter] public EventCallback Ready { get; set; }

	/// <summary>
	/// Gets or sets whether right clicking on an item selects it?
	/// </summary>
	[Parameter] public bool RightClickSelectsItem { get; set; } = true;

	/// <summary>
	/// Determines whether the navigate up to the parent folder button is visible or not.
	/// </summary>
	[Parameter] public bool ShowNavigateUpButton { get; set; } = true;

	/// <summary>
	/// Determines where sub-folders show an entry (..) to allow navigation to the parent folder.
	/// </summary>
	[Parameter] public bool ShowParentFolder { get; set; } = true;

	/// <summary>
	/// Determines whether the toolbar is visible.
	/// </summary>
	[Parameter] public bool ShowToolbar { get; set; } = true;

	/// <summary>
	/// Determines whether the upload progress dialog is shown.
	/// </summary>
	[Parameter] public bool ShowUploadProgressDialog { get; set; } = true;

	/// <summary>
	/// Determines when the upload progress dialog is shown.
	/// </summary>
	/// <remarks>The dialog is shown when the number of files to upload exceeds the threshold.
	/// ShowUploadProgressDialog must be set to true.</remarks>
	[Parameter] public int UploadProgressDialogThreshold { get; set; } = 1;

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
	/// Determines whether file entries should be listed.
	/// </summary>
	[Parameter] public bool ShowFiles { get; set; } = true;

	/// <summary>
	/// Sets the size (humanizer) format.
	/// </summary>
	[Parameter] public string SizeFormat { get; set; } = "#,0 KB";

	/// <summary>
	/// Event raised whenever the user clicks on a context menu item from the table.
	/// </summary>
	[Parameter] public EventCallback<MenuItemEventArgs> TableContextMenuClick { get; set; }

	/// <summary>
	/// Sets the Table context menu items.
	/// </summary>
	[Parameter]
	public List<MenuItem> TableContextItems { get; set; } = [];

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
	public List<ToolbarItem> ToolbarItems { get; set; } = [];

	/// <summary>
	/// Event raised whenever the user clicks on a context menu item from the tree.
	/// </summary>
	[Parameter] public EventCallback<MenuItemEventArgs> TreeContextMenuClick { get; set; }

	/// <summary>
	/// Sets the Tree context menu items.
	/// </summary>
	[Parameter] public List<MenuItem> TreeContextItems { get; set; } = [];

	/// <summary>
	/// Optional sort function to use on sibling tree nodes.
	/// </summary>
	[Parameter] public Comparison<FileExplorerItem>? TreeSort { get; set; }

	/// <summary>
	/// Event raised whenever a file upload completes.
	/// </summary>
	[Parameter] public EventCallback<DropZoneUploadCompletedEventArgs> UploadCompleted { get; set; }

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
	/// Upload timeout in seconds.
	/// </summary>
	[Parameter] public int UploadTimeout { get; set; } = 60;

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

	#endregion

	/// <summary>
	/// Validates parameter constraints.
	/// </summary>
	protected override void OnParametersSet()
	{
		if (string.IsNullOrWhiteSpace(NewFolderName))
		{
			throw new ArgumentException("The NewFolderName parameter cannot be null or whitespace.");
		}
	}

	/// <summary>
	/// Initializes static menu and identifier state.
	/// </summary>
	/// <returns>A completed task.</returns>
	protected override Task OnInitializedAsync()
	{
		Id = $"pdfe{++_idSequence}";

		// bind the default provider (or a supplied, unbound FileExplorerPreviewProvider) to this explorer,
		// and use any other supplied provider exactly as given
		if (PreviewProvider is FileExplorerPreviewProvider { FileExplorer: null } fileExplorerPreviewProvider)
		{
			fileExplorerPreviewProvider.FileExplorer = this;
		}

		TableContextItems.AddRange(
		[
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
		]);

		TreeContextItems.AddRange(
		[
			_menuNewFolder,
			_menuRename,
			_menuSep2,
			_menuCopy,
			_menuCut,
			_menuPaste,
			_menuSep3,
			_menuDelete
		]);

		return Task.CompletedTask;
	}

	/// <summary>
	/// Initializes JavaScript modules and first-render UI state.
	/// </summary>
	/// <param name="firstRender">True on first render; otherwise false.</param>
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			try
			{
				_commonModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", JSInteropVersionHelper.CommonJsUrl).ConfigureAwait(true);
			}
			catch
			{
				// Do nothing
			}

			var isTouchDevice = _commonModule != null && await _commonModule.InvokeAsync<bool>("isTouchDevice").ConfigureAwait(true);

			ToolbarItems.Add(new ToolbarButton { Key = "navigate-up", ToolTip = "Navigate up to parent folder", IconCssClass = "fas fa-fw fa-arrow-up", CssClass = "btn-secondary d-none d-lg-inline", TextCssClass = "d-none d-lg-inline", IsVisible = ShowNavigateUpButton });
			if (isTouchDevice)
			{
				ToolbarItems.Add(new ToolbarButton { Key = "open", Text = "Open", ToolTip = "Navigate into folder", IconCssClass = "fas fa-fw fa-folder-open", CssClass = "btn-secondary", TextCssClass = "d-none d-lg-inline" });
			}

			ToolbarItems.Add(new ToolbarButton { Key = "refresh", Text = "Refresh", ToolTip = "Refreshes the current folder", IconCssClass = "fas fa-fw fa-sync-alt", CssClass = "btn-secondary", TextCssClass = "d-none d-lg-inline" });

			if (PreviewPanel == FilePreviewModes.OptionalOff || PreviewPanel == FilePreviewModes.OptionalOn)
			{
				_previewPanelButton = new ToolbarButton { Key = "preview", Text = "Preview", ToolTip = "Toggles display of the Preview panel", IconCssClass = "fas fa-fw fa-eye", CssClass = "btn-secondary", TextCssClass = "d-none d-lg-inline" };
				ToolbarItems.Add(_previewPanelButton);
			}

			ToolbarItems.Add(new ToolbarButton { Key = "create-folder", Text = "New Folder", ToolTip = "Create a new folder", IconCssClass = "fas fa-fw fa-folder-plus", CssClass = "btn-secondary", TextCssClass = "d-none d-lg-inline" });
			ToolbarItems.Add(new ToolbarButton { Key = "delete", Text = "Delete", ToolTip = "Delete the selected files and folders", IconCssClass = "fas fa-fw fa-trash-alt", CssClass = "btn-danger", ShiftRight = true, TextCssClass = "d-none d-lg-inline" });

			if (!string.IsNullOrWhiteSpace(UploadUrl))
			{
				TableContextItems.Insert(1, _menuUploadFiles);
				TreeContextItems.Insert(0, _menuUploadFiles);
				ToolbarItems.Insert(2, new ToolbarButton { Key = "upload", Text = "Upload", ToolTip = "Upload one or more files", IconCssClass = "fas fa-fw fa-upload", CssClass = "btn-secondary", TextCssClass = "d-none d-lg-inline" });
			}

			if (PreviewPanel == FilePreviewModes.OptionalOff)
			{
				PreviewPanelVisible = false;
			}

			if (DeleteDialog != null)
			{
				DeleteDialog.Buttons.Clear();
				DeleteDialog.Buttons.AddRange(
				[
					new ToolbarButton { Key="yes", Text = "Yes", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-check", ShiftRight = true },
					new ToolbarButton { Key="no", Text = "No", CssClass = "btn-primary", IconCssClass = "fas fa-fw fa-times" },
				]);
			}

			// add third button needed for conflict resolution
			if (ConflictDialog != null)
			{
				ConflictDialog.Buttons.Clear();
				ConflictDialog.Buttons.AddRange(
				[
					new ToolbarButton { Text = "Overwrite", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-save", ShiftRight = true },
					new ToolbarButton { Text = "Rename", CssClass = "btn-primary", IconCssClass = "fas fa-fw fa-pen-square" },
					new ToolbarButton { Text = "Skip", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-forward" },
					new ToolbarButton { Text = "Cancel", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" }
				]);
			}

			// set up buttons on upload dialog
			if (UploadDialog != null)
			{
				UploadDialog!.Buttons.First(x => x.Key == "Yes").IsVisible = false;
				if (UploadDialog!.Buttons.First(x => x.Key == "No") is ToolbarButton btn)
				{
					btn.ShiftRight = true;
					btn.Text = "Close";
					btn.CssClass = "btn-primary";
				}
			}

			await RefreshToolbarAsync().ConfigureAwait(true);
		}
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
	/// Gets the tree root node.
	/// </summary>
	public TreeNode<FileExplorerItem>? TreeRootNode => Tree?.RootNode;

	/// <summary>
	/// Attempts to navigate down to the given path.
	/// </summary>
	/// <param name="path">The relative path from root to the intended folder.</param>
	public async Task NavigateToAsync(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		IsNavigating = true;

		// must start from root
		if (FolderPath != "/")
		{
			await NavigateFolderAsync("/").ConfigureAwait(true);
		}

		// descend folder by folder
		var parts = path.Split(["/"], StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < parts.Length; i++)
		{
			var subPath = "/" + string.Join("/", parts.Take(i + 1));
			await NavigateFolderAsync(subPath).ConfigureAwait(true);
		}

		IsNavigating = false;
	}

	/// <summary>
	/// Forces the tree component of the file explorer to be refreshed.
	/// </summary>
	public async Task RefreshTreeAsync() => await Tree!.RefreshAsync().ConfigureAwait(true);

	/// <summary>
	/// Refreshes the tree and table panes.
	/// </summary>
	public async Task RefreshAllAsync()
	{
		await RefreshTreeAsync().ConfigureAwait(true);
		await RefreshTableAsync().ConfigureAwait(true);
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
		_previewItem = null;

		// explicitly state search path else fetch will use previous value as OnParametersSet not yet called
		await Table.RefreshAsync(FolderPath).ConfigureAwait(true);
		StateHasChanged();
	}

	/// <summary>
	/// Forces the toolbar component of the file explorer to be refreshed.
	/// </summary>
	public async Task RefreshToolbarAsync()
	{
		var selectedItems = Table!.GetSelectedItems();

		// up button
		var upButton = ToolbarItems.Find(x => x.Key == "navigate-up");
		if (upButton != null)
		{
			upButton.IsEnabled = Tree?.SelectedNode?.ParentNode?.ParentNode != null;
			upButton.IsVisible = ShowNavigateUpButton;
		}

		// open button
		var openButton = ToolbarItems.Find(x => x.Key == "open");
		if (openButton != null)
		{
			openButton.IsEnabled = selectedItems.Length == 1 && selectedItems[0].EntryType == FileExplorerItemType.Directory;
		}

		// create folder button - acts on selected folder
		var createFolderButton = ToolbarItems.Find(x => x.Key == "create-folder");
		if (createFolderButton != null)
		{
			createFolderButton.IsEnabled = (Tree?.SelectedNode?.Data) != null && Tree.SelectedNode.Data.CanAddItems;
		}

		// upload button
		var uploadButton = ToolbarItems.Find(x => x.Key == "upload");
		if (uploadButton != null)
		{
			uploadButton.IsEnabled = Tree?.SelectedNode?.Data?.CanAddItems == true;
		}

		// delete button
		var deleteButton = ToolbarItems.Find(x => x.Key == "delete");
		if (deleteButton != null)
		{
			deleteButton.IsEnabled = Table!.Selection.Count > 0
				&& selectedItems.All(x => x.CanDelete)
				&& !selectedItems.Any(x => IsParentDirectoryItem(x));
		}

		// preview button
		if (_previewPanelButton != null)
		{
			_previewPanelButton.IconCssClass = PreviewPanelVisible ? "fas fa-fw fa-eye-slash" : "fas fa-fw fa-eye";
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
			return [.. Table!.Selection];
		}
	}

	/// <summary>
	/// Gets the folder currently selected in the tree view.
	/// </summary>
	public FileExplorerItem? GetTreeSelectedFolder() => Tree?.SelectedNode?.Data;

	private string GetCssClass(FileExplorerItem? item)
	{
		if (item is null)
		{
			return string.Empty;
		}

		var defaultCss = $"{(item.IsHidden ? "file-hidden" : "")} {(item.IsSystem ? "file-system" : "")} {(item.IsReadOnly ? "file-readonly" : "")}";
		if (GetItemCssClass != null)
		{
			return GetItemCssClass(item) ?? defaultCss;
		}

		return defaultCss;
	}

	/// <summary>
	/// Returns icon CSS class for a file explorer item.
	/// </summary>
	/// <param name="item">Item to evaluate.</param>
	/// <returns>CSS class string for the item icon.</returns>
	public string GetIconCssClass(FileExplorerItem? item)
	{
		if (item != null)
		{
			// allow app supplied icon css
			var cssClass = GetItemIconCssClass is null ? null : GetItemIconCssClass(item);

			if (cssClass is null)
			{
				return item.EntryType == FileExplorerItemType.File ? "far fa-fw fa-file" : "fa fa-fw fa-folder";
			}

			if (cssClass.Length == 0)
			{
				return "far fa-fw fa-hidden fa-file";
			}

			return cssClass;
		}

		return string.Empty;
	}

	/// <summary>
	/// Disposes JavaScript resources held by the component.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		try
		{
			GC.SuppressFinalize(this);
			if (_commonModule != null)
			{
				await _commonModule.DisposeAsync().ConfigureAwait(true);
			}
		}
		catch
		{
			// Too bad...
		}
	}
}
