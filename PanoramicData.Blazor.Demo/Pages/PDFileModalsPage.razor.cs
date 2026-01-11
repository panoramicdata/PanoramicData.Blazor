namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDFileModalsPage
{
	private PDFileModal _fileModal = null!;
	private string _openResult = string.Empty;
	private string _saveAsResult = string.Empty;
	private readonly IDataProviderService<FileExplorerItem> _dataProvider = new TestFileSystemDataProvider();
	private bool _showOpen;

	// Virtual folders to prioritize at the top of the tree
	private readonly string[] _virtualFolders = ["/Library", "/Users"];

	private static string GetIconCssClass(FileExplorerItem item)
	{
		if (item.EntryType == FileExplorerItemType.Directory && item.Name != "..")
		{
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
		// Shift Library and Users folders to top
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

	private void OnModalHidden(string result)
	{
		// only called when modal NOT shown with a AndWaitResult method
		if (_showOpen)
		{
			_openResult = result;
		}
		else
		{
			_saveAsResult = result;
		}
	}

	private async Task ShowFileOpenModalAndWaitResult() => _openResult = await _fileModal.ShowOpenAndWaitResultAsync().ConfigureAwait(true);

	private async Task ShowFileOpenFilteredModalAndWaitResult() =>
		// show DOCX and XLSX files only
		_openResult = await _fileModal.ShowOpenAndWaitResultAsync(false, "*.docx;*.xlsx").ConfigureAwait(true);

	private async Task ShowFolderOpenModalAndWaitResult() => _openResult = await _fileModal.ShowOpenAndWaitResultAsync(true).ConfigureAwait(true);

	private async Task ShowFileSaveAsModalAndWaitResult() => _saveAsResult = await _fileModal.ShowSaveAsAndWaitResultAsync("NewFile.html").ConfigureAwait(true);

	private async Task ShowFileOpenModal()
	{
		_showOpen = true;
		await _fileModal.ShowOpenAsync().ConfigureAwait(true);
	}

	private async Task ShowFileSaveAsModal()
	{
		_showOpen = false;
		await _fileModal.ShowSaveAsAsync(_openResult).ConfigureAwait(true);
	}
}
