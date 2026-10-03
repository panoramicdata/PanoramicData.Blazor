namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a modal file open/save dialog backed by a <see cref="PDFileExplorer"/>.
/// </summary>
public partial class PDFileModal
{
	/// <summary>
	/// Gets the underlying modal dialog component.
	/// </summary>
	public PDModal Modal { get; private set; } = null!;

	private bool _showOpen;
	private bool _showFiles = true;
	private bool _folderSelect;
	private string _filenamePattern = string.Empty;
	private FileExplorerItem? _currentFolderItem;
	private PDModal ModalConfirm { get; set; } = null!;
	private string _modalTitle = string.Empty;
	private PDFileExplorer FileExplorer { get; set; } = null!;
	private readonly List<ToolbarItem> _toolbarItems = [];
	private readonly ToolbarTextBox _filenameTextbox = new() { Key = "Filename", Label = "File name", Width = "100%" };
	private readonly ToolbarButton _cancelButton = new() { Key = "Cancel", Text = "Cancel", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" };
	private readonly ToolbarButton _okButton = new() { Key = "OK", Text = "OK", CssClass = "btn-primary", ShiftRight = true, IsEnabled = false };
	private readonly List<ToolbarItem> _confirmToolbarItems = [];
	private readonly ToolbarButton _overwriteButton = new() { Key = "Yes", Text = "Yes - Overwrite", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-save", ShiftRight = true };
	private readonly ToolbarButton _noButton = new() { Key = "No", Text = "No", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" };

	/// <summary>
	/// When set, called for each folder the user navigates to or selects. Return false to prevent selection.
	/// </summary>
	[Parameter] public Func<FileExplorerItem, bool>? CanSelectFolder { get; set; }

	/// <summary>
	/// Gets or sets whether the modal should close when the escape key is pressed.
	/// </summary>
	[Parameter] public bool CloseOnEscape { get; set; } = true;

	/// <summary>
	/// Gets or sets the data provider for the file explorer.
	/// </summary>
	[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

	/// <summary>
	/// Gets or sets a collection of paths to exclude from the file explorer.
	/// </summary>
	[Parameter] public string[] ExcludedPaths { get; set; } = [];

	/// <summary>
	/// Gets or sets the CSS height of the file explorer area.
	/// </summary>
	[Parameter] public string Height { get; set; } = "400px";

	/// <summary>
	/// A function to get the CSS class for a given file explorer item.
	/// </summary>
	[Parameter] public Func<FileExplorerItem, string>? GetItemIconCssClass { get; set; }

	/// <summary>
	/// Gets or sets the text for the 'Open' button.
	/// </summary>
	[Parameter] public string OpenButtonText { get; set; } = "Open";

	/// <summary>
	/// Gets or sets the icon CSS class for the 'Open' button.
	/// </summary>
	[Parameter] public string OpenButtonIconCssClass { get; set; } = "fas fa-fw fa-folder-open";

	/// <summary>
	/// Gets or sets the text for the 'Save' button.
	/// </summary>
	[Parameter] public string SaveButtonText { get; set; } = "Save";

	/// <summary>
	/// Gets or sets the icon CSS class for the 'Save' button.
	/// </summary>
	[Parameter] public string SaveButtonIconCssClass { get; set; } = "fas fa-fw fa-save";

	/// <summary>
	/// Gets or sets whether to show the context menu in the file explorer.
	/// </summary>
	[Parameter] public bool ShowContextMenu { get; set; }

	/// <summary>
	/// Gets or sets whether to show the 'Navigate Up' button in the file explorer.
	/// </summary>
	[Parameter] public bool ShowNavigateUpButton { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the toolbar in the file explorer.
	/// </summary>
	[Parameter] public bool ShowToolbar { get; set; }

	/// <summary>
	/// Gets or sets the title of the modal when in 'Open' mode.
	/// </summary>
	[Parameter] public string OpenTitle { get; set; } = "File Open";

	/// <summary>
	/// Gets or sets the title of the modal when in 'Save' mode.
	/// </summary>
	[Parameter] public string SaveTitle { get; set; } = "File Save";

	/// <summary>
	/// Gets or sets the size of the modal.
	/// </summary>
	[Parameter] public ModalSizes Size { get; set; } = ModalSizes.Large;

	/// <summary>
	/// Gets or sets whether the modal should hide when the background is clicked.
	/// </summary>
	[Parameter] public bool HideOnBackgroundClick { get; set; }

	/// <summary>
	/// An event callback that is invoked when the modal is hidden.
	/// </summary>
	[Parameter] public EventCallback<string> ModalHidden { get; set; }

	/// <summary>
	/// Optional sort function to use on sibling tree nodes.
	/// </summary>
	[Parameter] public Comparison<FileExplorerItem>? TreeSort { get; set; }

	/// <summary>
	/// Gets or sets the CSS class for the read-only icon (e.g., "fa fa-solid fa-lock").
	/// When set, this icon is used instead of ReadOnlyPostfix text.
	/// </summary>
	[Parameter] public string? ReadOnlyIconClass { get; set; }

	/// <summary>
	/// Gets or sets the position of the read-only indicator relative to the filename.
	/// Default is After for backward compatibility.
	/// </summary>
	[Parameter] public ReadOnlyIndicatorPosition ReadOnlyIndicatorPosition { get; set; } = ReadOnlyIndicatorPosition.After;

	private string GetOpenResult() => _folderSelect
			? (FileExplorer.SelectedFilesAndFolders.Length == 1 ? FileExplorer.SelectedFilesAndFolders[0] : FileExplorer.FolderPath)
			: $"{FileExplorer.FolderPath.TrimEnd('/')}/{_filenameTextbox.Value}";

	private void OnFolderChanged(FileExplorerItem item)
	{
		_currentFolderItem = item;
		if (!_showFiles)
		{
			_okButton.IsEnabled =
				!string.IsNullOrEmpty(FileExplorer.FolderPath) &&
				(CanSelectFolder == null || CanSelectFolder(item));
		}
	}

	/// <inheritdoc />
	protected override void OnInitialized()
	{
		// create toolbar contents
		_toolbarItems.AddRange([
			_filenameTextbox,
			_okButton,
			_cancelButton
		]);

		_confirmToolbarItems.AddRange([
			_overwriteButton,
			_noButton
		]);

		// wire up filename events
		_filenameTextbox.KeypressEvent = true;
		_filenameTextbox.ValueChanged = OnFilenameChanged;
		_filenameTextbox.Keypress = OnFilenameKeypress;
	}

	private string? GetItemIconCssClassInternal(FileExplorerItem item) => GetItemIconCssClass is null ? null : GetItemIconCssClass(item);

	/// <summary>
	/// Shows the dialog in Open mode for any file, starting at the root, and returns once the user has dismissed it.
	/// </summary>
	public Task ShowOpenAsync() => ShowOpenAsync(false, "", "");

	/// <summary>
	/// Shows the dialog in Open mode, for any file or for a folder, starting at the root, and returns once the user
	/// has dismissed it.
	/// </summary>
	public Task ShowOpenAsync(bool folderSelect) => ShowOpenAsync(folderSelect, "", "");

	/// <summary>
	/// Shows the dialog in Open mode, starting at the root, and returns once the user has dismissed it.
	/// </summary>
	public Task ShowOpenAsync(bool folderSelect, string filenamePattern) => ShowOpenAsync(folderSelect, filenamePattern, "");

	/// <summary>
	/// Shows the dialog in Open mode and returns once the user has dismissed it.
	/// </summary>
	public async Task ShowOpenAsync(bool folderSelect, string filenamePattern, string initialFolder)
	{
		PrepareOpen(folderSelect, filenamePattern);

		// show the modal
		await Modal.ShowAsync().ConfigureAwait(true);

		// default to the given folder, or the root
		var folder = string.IsNullOrWhiteSpace(initialFolder) ? "/" : initialFolder;
		if (string.Equals(FileExplorer.FolderPath, folder, StringComparison.Ordinal))
		{
			// navigating to the current folder does nothing, so reload it to apply the new file / folder mode
			await FileExplorer.RefreshTableAsync().ConfigureAwait(true);
		}
		else
		{
			await FileExplorer.NavigateToAsync(folder).ConfigureAwait(true);
		}
	}

	/// <summary>
	/// Shows the dialog in Open mode for any file and waits for the user to select one, returning the selected path.
	/// </summary>
	public Task<string> ShowOpenAndWaitResultAsync() => ShowOpenAndWaitResultAsync(false, "");

	/// <summary>
	/// Shows the dialog in Open mode, for any file or for a folder, and waits for the user to select one, returning
	/// the selected path.
	/// </summary>
	public Task<string> ShowOpenAndWaitResultAsync(bool folderSelect) => ShowOpenAndWaitResultAsync(folderSelect, "");

	/// <summary>
	/// Shows the dialog in Open mode and waits for the user to select a file, returning the selected path.
	/// </summary>
	public async Task<string> ShowOpenAndWaitResultAsync(bool folderSelect, string filenamePattern)
	{
		PrepareOpen(folderSelect, filenamePattern);

		// refresh the current folder contents
		await FileExplorer.RefreshTableAsync().ConfigureAwait(true);

		var userAction = await Modal.ShowAndWaitResultAsync().ConfigureAwait(true);

		if (userAction == "Cancel")
		{
			return string.Empty;
		}

		return GetOpenResult();
	}

	/// <summary>
	/// Provides the ability to force a refresh of the both the file explorer tree and the files table
	/// </summary>
	public async Task RefreshFileExplorerAsync() => await FileExplorer.RefreshAllAsync().ConfigureAwait(true);

	/// <summary>
	/// Shows the dialog in Save As mode, with no initial file, and returns once the user has dismissed it.
	/// </summary>
	public Task ShowSaveAsAsync() => ShowSaveAsAsync("", "");

	/// <summary>
	/// Shows the dialog in Save As mode, for any file, and returns once the user has dismissed it.
	/// </summary>
	public Task ShowSaveAsAsync(string initialFilename) => ShowSaveAsAsync(initialFilename, "");

	/// <summary>
	/// Shows the dialog in Save As mode and returns once the user has dismissed it.
	/// </summary>
	public async Task ShowSaveAsAsync(string initialFilename, string filenamePattern)
	{
		PrepareSaveAs(initialFilename, filenamePattern);

		await Modal.ShowAsync().ConfigureAwait(true);

		await NavigateToSaveAsFolderAsync(initialFilename).ConfigureAwait(true);
	}

	/// <summary>
	/// Shows the dialog in Save As mode, with no initial file, and waits for the user to confirm a filename,
	/// returning the selected path.
	/// </summary>
	public Task<string> ShowSaveAsAndWaitResultAsync() => ShowSaveAsAndWaitResultAsync("", "");

	/// <summary>
	/// Shows the dialog in Save As mode, for any file, and waits for the user to confirm a filename, returning the
	/// selected path.
	/// </summary>
	public Task<string> ShowSaveAsAndWaitResultAsync(string initialFilename) => ShowSaveAsAndWaitResultAsync(initialFilename, "");

	/// <summary>
	/// Shows the dialog in Save As mode and waits for the user to confirm a filename, returning the selected path.
	/// </summary>
	public async Task<string> ShowSaveAsAndWaitResultAsync(string initialFilename, string filenamePattern)
	{
		PrepareSaveAs(initialFilename, filenamePattern);

		await NavigateToSaveAsFolderAsync(initialFilename).ConfigureAwait(true);

		do
		{
			var userAction = await Modal.ShowAndWaitResultAsync().ConfigureAwait(true);
			if (userAction == "Cancel")
			{
				return string.Empty;
			}
		}
		while (!await ConfirmOverwriteAsync().ConfigureAwait(true));

		return $"{FileExplorer.FolderPath.TrimEnd('/')}/{_filenameTextbox.Value}";
	}

	/// <summary>
	/// Sets the dialog up to open a file, or to select a folder.
	/// </summary>
	private void PrepareOpen(bool folderSelect, string filenamePattern)
	{
		_showOpen = true;
		_showFiles = !folderSelect;
		_folderSelect = folderSelect;
		_filenamePattern = filenamePattern;
		_modalTitle = OpenTitle;
		_filenameTextbox.Value = "";
		_okButton.IsEnabled = folderSelect && !string.IsNullOrEmpty(FileExplorer.FolderPath);
		_filenameTextbox.IsVisible = false;
		_okButton.Text = OpenButtonText;
		_okButton.IconCssClass = OpenButtonIconCssClass;
		StateHasChanged();
	}

	/// <summary>
	/// Sets the dialog up to save a file, starting with the given name, if any.
	/// </summary>
	private void PrepareSaveAs(string initialFilename, string filenamePattern)
	{
		_showOpen = false;
		_showFiles = true;
		_filenamePattern = filenamePattern;
		_modalTitle = SaveTitle;
		if (!string.IsNullOrWhiteSpace(initialFilename))
		{
			_filenameTextbox.Value = FileExplorerItem.GetNameFromPath(initialFilename);
			_okButton.IsEnabled = true;
		}

		_filenameTextbox.IsVisible = true;
		_okButton.Text = SaveButtonText;
		_okButton.IconCssClass = SaveButtonIconCssClass;
		StateHasChanged();
	}

	/// <summary>
	/// Shows the initial file's location, or the root when there is no initial file.
	/// </summary>
	private Task NavigateToSaveAsFolderAsync(string initialFilename)
		=> FileExplorer.NavigateToAsync(string.IsNullOrWhiteSpace(initialFilename) ? "/" : initialFilename);

	/// <summary>
	/// Returns whether the entered file name can be saved: either no file has it, or the user agrees to overwrite it.
	/// </summary>
	private async Task<bool> ConfirmOverwriteAsync()
	{
		var existing = Array.Find(FileExplorer.FileItems ?? [], x => x.EntryType == FileExplorerItemType.File && x.Name == _filenameTextbox.Value);
		return existing is null
			|| await ModalConfirm.ShowAndWaitResultAsync().ConfigureAwait(true) == "Yes";
	}

	private async Task OnButtonClick(string text)
	{
		var result = string.Empty;
		if (text != "Cancel")
		{
			if (_showOpen)
			{
				// open modal
				result = GetOpenResult();
			}
			else
			{
				// save as
				var existing = Array.Find(FileExplorer.FileItems ?? [], x => x.EntryType == FileExplorerItemType.File && x.Name == _filenameTextbox.Value);
				if (existing != null)
				{
					// prompt user for over write
					await Modal.HideAsync().ConfigureAwait(true);
					var confirmation = await ModalConfirm.ShowAndWaitResultAsync().ConfigureAwait(true);
					if (confirmation == "No")
					{
						await Modal.ShowAsync().ConfigureAwait(true);
						return;
					}
				}

				result = $"{FileExplorer.FolderPath.TrimEnd('/')}/{_filenameTextbox.Value}";
			}
		}

		// inform caller of result and hide modal
		try
		{
			await this.InvokeAsync(async () => await ModalHidden.InvokeAsync(result).ConfigureAwait(true));
		}
		catch
		{
			// a failing ModalHidden handler is the caller's problem, and must not leave the dialog open
		}

		await Modal.HideAsync().ConfigureAwait(true);
	}

	private void OnSelectionChanged(FileExplorerItem[] selection)
	{
		_filenameTextbox.Value = selection.Length == 1 && selection[0].EntryType == FileExplorerItemType.File ? selection[0].Name : string.Empty;
		if (_showFiles)
		{
			_okButton.IsEnabled = !string.IsNullOrWhiteSpace(_filenameTextbox.Value);
		}
		else
		{
			_okButton.IsEnabled = selection.Length switch
			{
				0 => IsCurrentFolderSelectable(),
				1 => IsSelectableFolder(selection[0]),
				_ => false
			};
		}
	}

	private bool IsCurrentFolderSelectable()
		=> !string.IsNullOrEmpty(FileExplorer.FolderPath)
			&& (CanSelectFolder == null || (_currentFolderItem != null && CanSelectFolder(_currentFolderItem)));

	private bool IsSelectableFolder(FileExplorerItem item)
		=> item.EntryType == FileExplorerItemType.Directory
			&& item.Name != ".."
			&& (CanSelectFolder == null || CanSelectFolder(item));

	private async Task OnItemDoubleClick(FileExplorerItem item)
	{
		if (item.EntryType == FileExplorerItemType.File)
		{
			_filenameTextbox.Value = item.Name;
			await Modal.OnButtonClick(new KeyedEventArgs<MouseEventArgs>(_okButton.Key)).ConfigureAwait(true);
		}
	}

	private void OnFilenameChanged(string value)
	{
		_filenameTextbox.Value = value;
		_okButton.IsEnabled = !string.IsNullOrWhiteSpace(_filenameTextbox.Value);
	}

	private void OnFilenameKeypress(KeyboardEventArgs args)
	{
		if (args.Code == "Enter" && !string.IsNullOrWhiteSpace(_filenameTextbox.Value))
		{
			Task.Run(async () => await this.InvokeAsync(async () => await Modal.OnButtonClick(new KeyedEventArgs<MouseEventArgs>(_okButton.Key)).ConfigureAwait(true)).ConfigureAwait(true)).ConfigureAwait(true);
		}
	}
}
