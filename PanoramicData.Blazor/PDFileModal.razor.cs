namespace PanoramicData.Blazor;

public partial class PDFileModal
{
	public PDModal Modal { get; private set; } = null!;

	private bool _showOpen;
	private bool _showFiles = true;
	private bool _folderSelect;
	private string _filenamePattern = string.Empty;
	private PDModal ModalConfirm { get; set; } = null!;
	private string _modalTitle = string.Empty;
	private PDFileExplorer FileExplorer { get; set; } = null!;
	private readonly List<ToolbarItem> _toolbarItems = new();
	private readonly ToolbarTextBox _filenameTextbox = new() { Key = "Filename", Label = "File name", Width = "100%" };
	private readonly ToolbarButton _cancelButton = new() { Key = "Cancel", Text = "Cancel", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" };
	private readonly ToolbarButton _okButton = new() { Key = "OK", Text = "OK", CssClass = "btn-primary", ShiftRight = true, IsEnabled = false };
	private readonly List<ToolbarItem> _confirmToolbarItems = new();
	private readonly ToolbarButton _overwriteButton = new() { Key = "Yes", Text = "Yes - Overwrite", CssClass = "btn-danger", IconCssClass = "fas fa-fw fa-save", ShiftRight = true };
	private readonly ToolbarButton _noButton = new() { Key = "No", Text = "No", CssClass = "btn-secondary", IconCssClass = "fas fa-fw fa-times" };

	[Parameter] public bool CloseOnEscape { get; set; } = true;

	[Parameter] public IDataProviderService<FileExplorerItem> DataProvider { get; set; } = null!;

	[Parameter] public string[] ExcludedPaths { get; set; } = Array.Empty<string>();

	[Parameter] public Func<FileExplorerItem, string>? GetItemIconCssClass { get; set; }

	[Parameter] public string OpenButtonText { get; set; } = "Open";

	[Parameter] public string SaveButtonText { get; set; } = "Save";
	[Parameter] public bool ShowContextMenu { get; set; }

	[Parameter] public bool ShowNavigateUpButton { get; set; } = true;

	[Parameter] public bool ShowToolbar { get; set; }

	[Parameter] public string OpenTitle { get; set; } = "File Open";

	[Parameter] public string SaveTitle { get; set; } = "File Save";

	[Parameter] public ModalSizes Size { get; set; } = ModalSizes.Large;

	[Parameter] public bool HideOnBackgroundClick { get; set; }

	[Parameter] public EventCallback<string> ModalHidden { get; set; }

	private string GetOpenResult() => _folderSelect
			? (FileExplorer.SelectedFilesAndFolders.Length == 1 ? FileExplorer.SelectedFilesAndFolders[0] : FileExplorer.FolderPath)
			: $"{FileExplorer.FolderPath.TrimEnd('/')}/{_filenameTextbox.Value}";

	private void OnFolderChanged(FileExplorerItem item)
	{
		if (!_showFiles)
		{
			_okButton.IsEnabled = !string.IsNullOrEmpty(FileExplorer.FolderPath);
		}
	}

	protected override void OnInitialized()
	{
		// create toolbar contents
		_toolbarItems.AddRange(new ToolbarItem[] {
			_filenameTextbox,
			_okButton,
			_cancelButton
		});

		_confirmToolbarItems.AddRange(new ToolbarItem[] {
			_overwriteButton,
			_noButton
		});

		// wire up filename events
		_filenameTextbox.KeypressEvent = true;
		_filenameTextbox.ValueChanged = OnFilenameChanged;
		_filenameTextbox.Keypress = OnFilenameKeypress;
	}

	private string? GetItemIconCssClassInternal(FileExplorerItem item) => GetItemIconCssClass is null ? null : GetItemIconCssClass(item);

	public async Task ShowOpenAsync(bool folderSelect = false, string filenamePattern = "", string initialFolder = "")
	{
		_showOpen = true;
		_showFiles = !folderSelect;
		_folderSelect = folderSelect;
		_filenamePattern = filenamePattern;
		_modalTitle = OpenTitle;
		_filenameTextbox.Value = "";
		_okButton.IsEnabled = folderSelect && !string.IsNullOrEmpty(FileExplorer.FolderPath);
		if (_filenameTextbox.IsVisible)
		{
			_filenameTextbox.IsVisible = false;
		}

		if (_okButton.Text != OpenButtonText)
		{
			_okButton.Text = OpenButtonText;
		}

		if (_okButton.IconCssClass != "fas fa-fw fa-folder-open")
		{
			_okButton.IconCssClass = "fas fa-fw fa-folder-open";
		}

		StateHasChanged();

		// show the modal
		await Modal.ShowAsync().ConfigureAwait(true);

		// default folder?
		if (!string.IsNullOrWhiteSpace(initialFolder))
		{
			// default to given folder
			await FileExplorer.NavigateToAsync(initialFolder).ConfigureAwait(true);
		}
		else
		{
			// default to root
			await FileExplorer.NavigateToAsync("/").ConfigureAwait(true);
			//await FileExplorer.RefreshTableAsync().ConfigureAwait(true);
		}
	}

	public async Task<string> ShowOpenAndWaitResultAsync(bool folderSelect = false, string filenamePattern = "")
	{
		_showOpen = true;
		_showFiles = !folderSelect;
		_folderSelect = folderSelect;
		_filenamePattern = filenamePattern;
		_modalTitle = OpenTitle;
		_filenameTextbox.Value = "";
		_okButton.IsEnabled = folderSelect && !string.IsNullOrEmpty(FileExplorer.FolderPath);
		if (_filenameTextbox.IsVisible)
		{
			_filenameTextbox.IsVisible = false;
		}

		if (_okButton.Text != OpenButtonText)
		{
			_okButton.Text = OpenButtonText;
		}

		if (_okButton.IconCssClass != "fas fa-fw fa-folder-open")
		{
			_okButton.IconCssClass = "fas fa-fw fa-folder-open";
		}

		StateHasChanged();

		// refresh the current folder contents
		await FileExplorer.RefreshTableAsync().ConfigureAwait(true);

		var userAction = await Modal.ShowAndWaitResultAsync().ConfigureAwait(true);

		if (userAction == "Cancel")
		{
			return string.Empty;
		}

		return GetOpenResult();
	}

	public async Task ShowSaveAsAsync(string initialFilename = "", string filenamePattern = "")
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

		if (!_filenameTextbox.IsVisible)
		{
			_filenameTextbox.IsVisible = true;
		}

		if (_okButton.Text != SaveButtonText)
		{
			_okButton.Text = SaveButtonText;
		}

		if (_okButton.IconCssClass != "fas fa-fw fa-save")
		{
			_okButton.IconCssClass = "fas fa-fw fa-save";
		}

		StateHasChanged();

		await Modal.ShowAsync().ConfigureAwait(true);

		// default folder?
		if (!string.IsNullOrWhiteSpace(initialFilename))
		{
			// default to current initial files location
			await FileExplorer.NavigateToAsync(initialFilename).ConfigureAwait(true);
		}
		else
		{
			// default to root
			await FileExplorer.NavigateToAsync("/").ConfigureAwait(true);
			//await FileExplorer.RefreshTableAsync().ConfigureAwait(true);
		}
	}

	public async Task<string> ShowSaveAsAndWaitResultAsync(string initialFilename = "", string filenamePattern = "")
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

		if (!_filenameTextbox.IsVisible)
		{
			_filenameTextbox.IsVisible = true;
		}

		if (_okButton.Text != SaveButtonText)
		{
			_okButton.Text = SaveButtonText;
		}

		if (_okButton.IconCssClass != "fas fa-fw fa-save")
		{
			_okButton.IconCssClass = "fas fa-fw fa-save";
		}

		StateHasChanged();

		// default folder?
		if (!string.IsNullOrWhiteSpace(initialFilename))
		{
			// default to current initial files location
			await FileExplorer.NavigateToAsync(initialFilename).ConfigureAwait(true);
		}
		else
		{
			// default to root
			await FileExplorer.NavigateToAsync("/").ConfigureAwait(true);
			//await FileExplorer.RefreshTableAsync().ConfigureAwait(true);
		}

		FileExplorerItem? existing = null;
		do
		{
			var userAction = await Modal.ShowAndWaitResultAsync().ConfigureAwait(true);
			if (userAction == "Cancel")
			{
				return string.Empty;
			}

			// check for over write?
			existing = Array.Find(FileExplorer.FileItems ?? Array.Empty<FileExplorerItem>(), x => x.EntryType == FileExplorerItemType.File && x.Name == _filenameTextbox.Value);
			if (existing != null)
			{
				var confirmation = await ModalConfirm.ShowAndWaitResultAsync().ConfigureAwait(true);
				if (confirmation == "Yes")
				{
					existing = null;
				}
			}

		} while (existing != null);

		return $"{FileExplorer.FolderPath.TrimEnd('/')}/{_filenameTextbox.Value}";
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
				var existing = Array.Find(FileExplorer.FileItems ?? Array.Empty<FileExplorerItem>(), x => x.EntryType == FileExplorerItemType.File && x.Name == _filenameTextbox.Value);
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
		await ModalHidden.InvokeAsync(result).ConfigureAwait(true);
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
			_okButton.IsEnabled = (!string.IsNullOrEmpty(FileExplorer.FolderPath) && selection.Length == 0) ||
								  (selection.Length == 1 && selection[0].EntryType == FileExplorerItemType.Directory && selection[0].Name != "..");
		}
	}

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
			Task.Run(async () => await Modal.OnButtonClick(new KeyedEventArgs<MouseEventArgs>(_okButton.Key)).ConfigureAwait(true)).ConfigureAwait(true);
		}
	}
}
